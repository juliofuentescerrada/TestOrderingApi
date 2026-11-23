using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Abstractions;
using Ordering.Application.Orders;
using Ordering.Domain;
using Ordering.Infrastructure.Persistence;
using Ordering.Tests.Support;

namespace Ordering.Tests;

/// <summary>Tests del requisito "Idempotencia del alta" (spec order-creation, ADR-0007 a ADR-0012).</summary>
public sealed class CreateOrderIdempotencyApiTests : IClassFixture<ApiFactory>
{
    private const string Subject = "client-a";

    private readonly ApiFactory _factory;

    public CreateOrderIdempotencyApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string Body(Guid customerId, int quantity = 2) =>
        $$"""{"customerId":"{{customerId}}","lines":[{"sku":"SKU-1","quantity":{{quantity}},"unitPrice":10.00}]}""";

    private static string NewKey() => Guid.NewGuid().ToString("N");

    [Fact]
    public async Task Without_header_each_request_creates_an_order()
    {
        var customerId = Guid.NewGuid();

        var first = await Post(Subject, key: null, Body(customerId));
        var second = await Post(Subject, key: null, Body(customerId));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(2, await CountOrders(customerId));
    }

    [Fact]
    public async Task First_request_with_key_creates_the_order_without_replayed_header()
    {
        var customerId = Guid.NewGuid();

        var response = await Post(Subject, NewKey(), Body(customerId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.False(response.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal(1, await CountOrders(customerId));
    }

    [Fact]
    public async Task Repeating_the_same_request_returns_the_same_order()
    {
        var customerId = Guid.NewGuid();
        var key = NewKey();

        var first = await Post(Subject, key, Body(customerId));
        var second = await Post(Subject, key, Body(customerId));

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.Equal((await ReadOrder(first)).Id, (await ReadOrder(second)).Id);
        Assert.Equal("true", second.Headers.GetValues("Idempotent-Replayed").Single());
        Assert.Equal(1, await CountOrders(customerId));
    }

    [Fact]
    public async Task Same_body_written_differently_is_a_replay()
    {
        // Otro orden de propiedades, GUID en mayúsculas y 10 en lugar de 10.00 (ADR-0010).
        var customerId = Guid.NewGuid();
        var key = NewKey();
        var rewritten =
            $$"""{ "lines": [ { "unitPrice": 10, "quantity": 2, "sku": "SKU-1" } ], "customerId": "{{customerId.ToString().ToUpperInvariant()}}" }""";

        var first = await Post(Subject, key, Body(customerId));
        var second = await Post(Subject, key, rewritten);

        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.True(second.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal((await ReadOrder(first)).Id, (await ReadOrder(second)).Id);
    }

    [Fact]
    public async Task Same_key_with_different_body_returns_422_and_creates_nothing()
    {
        var customerId = Guid.NewGuid();
        var key = NewKey();

        await Post(Subject, key, Body(customerId, quantity: 2));
        var response = await Post(Subject, key, Body(customerId, quantity: 3));

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(1, await CountOrders(customerId));
    }

    [Fact]
    public async Task Same_key_in_different_clients_creates_one_order_each()
    {
        // Con una clave global, el cliente B recibiría el pedido de A (ADR-0008).
        var customerId = Guid.NewGuid();
        var key = NewKey();

        var a = await Post("client-a", key, Body(customerId));
        var b = await Post("client-b", key, Body(customerId));

        Assert.Equal(HttpStatusCode.Created, b.StatusCode);
        Assert.False(b.Headers.Contains("Idempotent-Replayed"));
        Assert.NotEqual((await ReadOrder(a)).Id, (await ReadOrder(b)).Id);
        Assert.Equal(2, await CountOrders(customerId));
    }

    [Fact]
    public async Task Concurrent_requests_with_the_same_key_create_a_single_order()
    {
        var customerId = Guid.NewGuid();
        var key = NewKey();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Post(Subject, key, Body(customerId))));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var ids = await Task.WhenAll(responses.Select(ReadOrder));
        Assert.Single(ids.Select(o => o.Id).Distinct());
        Assert.Equal(1, await CountOrders(customerId));
    }

    [Fact]
    public async Task Losing_the_race_saves_nothing_and_reports_the_conflict()
    {
        // Fuerza de forma determinista la rama de la carrera (ADR-0011), que el test concurrente
        // puede no alcanzar porque SQLite serializa las escrituras.
        var key = NewKey();
        var existingOrderId = Guid.NewGuid();
        var record = new IdempotencyRecord(Subject, key, "hash", existingOrderId, DateTime.UtcNow);

        using (var winner = _factory.Services.CreateScope())
        {
            var repository = winner.ServiceProvider.GetRequiredService<IOrderRepository>();
            await repository.AddIdempotencyRecordAsync(record, CancellationToken.None);
            Assert.True(await repository.TrySaveChangesAsync(CancellationToken.None));
        }

        var customerId = Guid.NewGuid();
        using (var loser = _factory.Services.CreateScope())
        {
            var repository = loser.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = Order.Create(customerId, DateTime.UtcNow);
            order.AddLine("SKU-1", 1, 1m);
            await repository.AddAsync(order, CancellationToken.None);
            await repository.AddIdempotencyRecordAsync(record with { OrderId = order.Id }, CancellationToken.None);

            Assert.False(await repository.TrySaveChangesAsync(CancellationToken.None));

            // Tras el conflicto el ChangeTracker queda limpio: un SaveChanges posterior no inserta el pedido.
            await repository.SaveChangesAsync(CancellationToken.None);
        }

        Assert.Equal(0, await CountOrders(customerId));
    }

    [Fact]
    public async Task Rejected_request_does_not_consume_the_key()
    {
        var customerId = Guid.NewGuid();
        var key = NewKey();

        // Uno rechazado por validación de entrada y otro por el dominio (customerId vacío).
        var invalid = await Post(Subject, key, Body(customerId, quantity: 0));
        var domainInvalid = await Post(Subject, key, Body(Guid.Empty));
        var valid = await Post(Subject, key, Body(customerId));

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, domainInvalid.StatusCode);
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
        Assert.False(valid.Headers.Contains("Idempotent-Replayed"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("k1234567890123456789012345678901234567890123456789012345678901234")] // 65 caracteres
    public async Task Invalid_header_returns_400(string key)
    {
        var customerId = Guid.NewGuid();
        var token = TestTokens.CreateFor(Subject, "orders:write");

        // HttpClient/TestServer descartan las cabeceras vacías, así que se construye el HttpContext a mano
        // para que la cabecera llegue tal cual (Kestrel sí la entrega, comprobado con curl).
        var context = await _factory.Server.SendAsync(c =>
        {
            c.Request.Method = HttpMethods.Post;
            c.Request.Path = "/api/orders";
            c.Request.Headers.Authorization = $"Bearer {token}";
            c.Request.Headers["Idempotency-Key"] = key;
            c.Request.ContentType = "application/json";
            c.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(Body(customerId)));
        });

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        Assert.Equal(0, await CountOrders(customerId));
    }

    [Fact]
    public async Task Header_with_a_token_without_sub_returns_400()
    {
        var customerId = Guid.NewGuid();

        var response = await Post(subject: null, NewKey(), Body(customerId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountOrders(customerId));
    }

    private async Task<HttpResponseMessage> Post(string? subject, string? key, string json)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.CreateFor(subject, "orders:write"));

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        if (key is not null)
        {
            // Sin validación para poder enviar cabeceras vacías o solo con espacios.
            request.Headers.TryAddWithoutValidation("Idempotency-Key", key);
        }

        return await client.SendAsync(request);
    }

    private static async Task<OrderDto> ReadOrder(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<OrderDto>())!;

    private async Task<int> CountOrders(Guid customerId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Orders.CountAsync(o => o.CustomerId == customerId);
    }
}

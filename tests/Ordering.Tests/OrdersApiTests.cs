using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Orders;
using Ordering.Domain;
using Ordering.Infrastructure.Persistence;
using Ordering.Tests.Support;

namespace Ordering.Tests;

/// <summary>Tests de integración de referencia: así se prueba un endpoint protegido en este repo.</summary>
public sealed class OrdersApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public OrdersApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_without_token_returns_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_with_token_but_without_read_scope_returns_403()
    {
        var client = CreateClient(TestTokens.Create("something:else"));

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Get_unknown_order_returns_404()
    {
        var client = CreateClient(TestTokens.Create("orders:read"));

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_existing_order_returns_200_with_lines_and_total()
    {
        var id = SeedOrder();
        var client = CreateClient(TestTokens.Create("orders:read"));

        var response = await client.GetAsync($"/api/orders/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(id, order!.Id);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(25.5m, order.Total);
    }

    private HttpClient CreateClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private Guid SeedOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = Order.Create(Guid.NewGuid(), DateTime.UtcNow);
        order.AddLine("SKU-1", 2, 10m);
        order.AddLine("SKU-2", 1, 5.5m);

        db.Orders.Add(order);
        db.SaveChanges();

        return order.Id;
    }
}

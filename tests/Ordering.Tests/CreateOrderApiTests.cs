using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ordering.Application.Orders;
using Ordering.Tests.Support;

namespace Ordering.Tests;

/// <summary>Tests de integración de POST /api/orders (spec order-creation).</summary>
public sealed class CreateOrderApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public CreateOrderApiTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static object ValidBody() => new
    {
        customerId = Guid.NewGuid(),
        lines = new object[]
        {
            new { sku = "SKU-1", quantity = 2, unitPrice = 10.00m },
            new { sku = "SKU-2", quantity = 1, unitPrice = 5.50m }
        }
    };

    [Fact]
    public async Task Post_without_token_returns_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/orders", ValidBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_with_only_read_scope_returns_403()
    {
        var response = await CreateClient("orders:read").PostAsJsonAsync("/api/orders", ValidBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Post_valid_order_returns_201_with_location_and_order_is_readable()
    {
        var client = CreateClient("orders:write", "orders:read");

        var response = await client.PostAsJsonAsync("/api/orders", ValidBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(created);
        Assert.Equal("Pending", created!.Status);
        Assert.Equal(2, created.Lines.Count);
        Assert.Equal(25.5m, created.Total);
        Assert.Equal($"/api/orders/{created.Id}", response.Headers.Location!.AbsolutePath);

        var get = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(created.Id, (await get.Content.ReadFromJsonAsync<OrderDto>())!.Id);
    }

    [Fact]
    public async Task Post_without_lines_returns_400()
    {
        var body = new { customerId = Guid.NewGuid(), lines = Array.Empty<object>() };

        var errors = await PostExpecting400(body);

        Assert.Contains("Lines", errors.Keys);
    }

    [Fact]
    public async Task Post_with_zero_quantity_returns_400()
    {
        var body = new { customerId = Guid.NewGuid(), lines = new[] { new { sku = "SKU-1", quantity = 0, unitPrice = 1m } } };

        var errors = await PostExpecting400(body);

        Assert.Contains("Lines[0].Quantity", errors.Keys);
    }

    [Fact]
    public async Task Post_with_missing_unit_price_returns_400()
    {
        // Sin decimal? este caso pasaría como precio 0 (ADR-0003).
        var body = new { customerId = Guid.NewGuid(), lines = new[] { new { sku = "SKU-1", quantity = 1 } } };

        var errors = await PostExpecting400(body);

        Assert.Contains("Lines[0].UnitPrice", errors.Keys);
    }

    [Fact]
    public async Task Post_with_several_errors_reports_all_of_them()
    {
        var body = new { lines = new[] { new { sku = "SKU-1", quantity = 0, unitPrice = 1m } } };

        var errors = await PostExpecting400(body);

        Assert.Contains("CustomerId", errors.Keys);
        Assert.Contains("Lines[0].Quantity", errors.Keys);
    }

    [Fact]
    public async Task Post_with_repeated_sku_creates_both_lines()
    {
        // ADR-0006: SKUs repetidos permitidos.
        var body = new
        {
            customerId = Guid.NewGuid(),
            lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = 1m }, new { sku = "SKU-1", quantity = 2, unitPrice = 1m } }
        };

        var created = await PostExpecting201(body);

        Assert.Equal(2, created.Lines.Count);
    }

    [Fact]
    public async Task Post_with_zero_unit_price_is_accepted()
    {
        var body = new { customerId = Guid.NewGuid(), lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = 0m } } };

        var created = await PostExpecting201(body);

        Assert.Equal(0m, created.Total);
    }

    [Fact]
    public async Task Post_with_more_than_50_lines_returns_400()
    {
        var lines = Enumerable.Range(0, 51).Select(i => new { sku = $"SKU-{i}", quantity = 1, unitPrice = 1m }).ToArray();

        var errors = await PostExpecting400(new { customerId = Guid.NewGuid(), lines });

        Assert.Contains("Lines", errors.Keys);
    }

    [Fact]
    public async Task Post_without_lines_property_returns_400()
    {
        var errors = await PostExpecting400(new { customerId = Guid.NewGuid() });

        Assert.Contains("Lines", errors.Keys);
    }

    [Fact]
    public async Task Post_with_null_line_returns_400()
    {
        // Sin la validación de CreateOrderRequest, el mapeo fallaría con un 500.
        var errors = await PostExpecting400(new { customerId = Guid.NewGuid(), lines = new object?[] { null } });

        Assert.Contains("Lines", errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("SKU-0123456789012345678901234567890123456789012345678901234567890")] // 65 caracteres
    public async Task Post_with_invalid_sku_returns_400(string sku)
    {
        var body = new { customerId = Guid.NewGuid(), lines = new[] { new { sku, quantity = 1, unitPrice = 1m } } };

        var errors = await PostExpecting400(body);

        Assert.Contains("Lines[0].Sku", errors.Keys);
    }

    [Fact]
    public async Task Post_with_negative_unit_price_returns_400()
    {
        var body = new { customerId = Guid.NewGuid(), lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = -0.01m } } };

        var errors = await PostExpecting400(body);

        Assert.Contains("Lines[0].UnitPrice", errors.Keys);
    }

    [Fact]
    public async Task Post_with_empty_customer_id_is_rejected_by_domain_as_400_problem_details()
    {
        // Guid.Empty explícito pasa [Required]; lo rechaza el dominio y DomainExceptionHandler lo traduce (ADR-0001).
        var body = new { customerId = Guid.Empty, lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = 1m } } };

        var response = await CreateClient("orders:write").PostAsJsonAsync("/api/orders", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        Assert.Equal("CustomerId is required.", problem!.Detail);
    }

    private async Task<OrderDto> PostExpecting201(object body)
    {
        var response = await CreateClient("orders:write").PostAsJsonAsync("/api/orders", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private async Task<Dictionary<string, string[]>> PostExpecting400(object body)
    {
        var response = await CreateClient("orders:write").PostAsJsonAsync("/api/orders", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsBody>();
        return problem!.Errors!;
    }

    private HttpClient CreateClient(params string[] scopes)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TestTokens.Create(scopes));
        return client;
    }

    private sealed record ProblemDetailsBody(int Status, string? Detail, Dictionary<string, string[]>? Errors);
}

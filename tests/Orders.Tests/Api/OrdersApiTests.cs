using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Orders.Core.Contracts;
using Orders.Tests.Infrastructure;

namespace Orders.Tests.Api;

/// <summary>Testes HTTP da API (spec 02) com PostgreSQL real.</summary>
[Collection(PostgresCollection.Name)]
public sealed class OrdersApiTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ApiFactory? _factory;
    private HttpClient Client => _factory!.CreateClient();

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable) return;
        await postgres.ResetAsync();
        _factory = new ApiFactory(postgres);
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null) await _factory.DisposeAsync();
    }

    [DockerFact]
    public async Task Post_creates_pending_order_and_outbox_message()
    {
        var response = await Client.PostAsJsonAsync("/orders",
            new { cliente = "Ana", produto = "Teclado", valor = 199.9m, status = "Finalizado" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/orders/{id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal("Pendente", body.GetProperty("status").GetString()); // status do cliente é ignorado
        Assert.Equal(199.9m, body.GetProperty("valor").GetDecimal());

        await using var db = postgres.CreateContext();
        var message = await db.OutboxMessages.SingleAsync();
        Assert.Equal((MessagingConstants.EventTypes.OrderCreated, id.ToString()), (message.Type, message.CorrelationId));
    }

    [DockerFact]
    public async Task Post_with_invalid_fields_returns_validation_problem()
    {
        var response = await Client.PostAsJsonAsync("/orders", new { cliente = "", produto = new string('x', 101), valor = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.True(errors.TryGetProperty("cliente", out _));
        Assert.True(errors.TryGetProperty("produto", out _));
        Assert.True(errors.TryGetProperty("valor", out _));
    }

    [DockerFact]
    public async Task Get_by_id_returns_order_with_history()
    {
        var created = await CreateAsync("Ana");

        var body = await Client.GetFromJsonAsync<JsonElement>($"/orders/{created}");

        var history = body.GetProperty("historico").EnumerateArray().ToList();
        Assert.Equal("Pendente", Assert.Single(history).GetProperty("status").GetString());
    }

    [DockerFact]
    public async Task Get_unknown_order_returns_not_found_problem()
    {
        var response = await Client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [DockerFact]
    public async Task List_returns_newest_first()
    {
        await CreateAsync("Primeiro");
        await CreateAsync("Segundo");

        var body = await Client.GetFromJsonAsync<JsonElement>("/orders");

        Assert.Equal(["Segundo", "Primeiro"], body.EnumerateArray().Select(o => o.GetProperty("cliente").GetString()));
    }

    [DockerFact]
    public async Task Liveness_is_healthy()
    {
        var response = await Client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [DockerFact]
    public async Task Ask_without_api_key_returns_service_unavailable()
    {
        var status = await Client.GetFromJsonAsync<JsonElement>("/orders/ask/status");
        Assert.False(status.GetProperty("enabled").GetBoolean());

        var response = await Client.PostAsJsonAsync("/orders/ask", new { question = "Quantos pedidos estão pendentes?" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private async Task<Guid> CreateAsync(string cliente)
    {
        var response = await Client.PostAsJsonAsync("/orders", new { cliente, produto = "Teclado", valor = 10m });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }
}

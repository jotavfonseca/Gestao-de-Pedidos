using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Orders.Api.Controllers;
using Orders.Tests.Infrastructure;

namespace Orders.Tests.Api;

/// <summary>
/// API real em memória apontando para o PostgreSQL do Testcontainers.
/// Service Bus desligado (Messaging:Enabled=false) e IA sem chave.
/// </summary>
public sealed class ApiFactory(PostgresFixture postgres) : WebApplicationFactory<OrdersController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Postgres", postgres.ConnectionString);
        builder.UseSetting("Messaging:Enabled", "false");
        builder.UseSetting("Ai:ApiKey", "");
        builder.UseSetting("ANTHROPIC_API_KEY", "");
        builder.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", "");
    }
}

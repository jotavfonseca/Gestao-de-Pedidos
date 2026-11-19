using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Orders.Api.Controllers;
using Orders.Tests.Infrastructure;

namespace Orders.Tests.Api;

/// <summary>
/// The real API in memory, pointing to the Testcontainers PostgreSQL.
/// Service Bus is off (Messaging:Enabled=false) and the AI has no key.
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

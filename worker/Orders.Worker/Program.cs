using Orders.Core.Application;
using Orders.Core.Infrastructure.Messaging;
using Orders.Core.Infrastructure.Persistence;
using Orders.Core.Observability;
using Orders.Worker;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

var postgres = configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");

builder.Logging.Configure(options => options.ActivityTrackingOptions =
    ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddOptions<WorkerOptions>().BindConfiguration(WorkerOptions.SectionName);

// Domínio, aplicação e persistência (o worker não aplica migrations: isso é papel da API)
builder.Services.AddOrdersPersistence(postgres).AddOrdersApplication();

// Mensageria: consome order-created e publica OrderStatusChanged pelo outbox
builder.Services.AddServiceBusMessaging(configuration);
builder.Services.AddScoped<OrderCreatedHandler>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<Heartbeat>();

builder.Services.AddOrdersTelemetry(configuration, "orders-worker");

await builder.Build().RunAsync();

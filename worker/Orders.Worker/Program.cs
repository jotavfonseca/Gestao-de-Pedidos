using Orders.Core.Application;
using Orders.Core.Infrastructure.Messaging;
using Orders.Core.Infrastructure.Persistence;
using Orders.Core.Observability;
using Orders.Worker;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

var postgres = configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

builder.Logging.Configure(options => options.ActivityTrackingOptions =
    ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.AddOptions<WorkerOptions>().BindConfiguration(WorkerOptions.SectionName);

// Domain, application and persistence (the worker does not apply migrations: that is the API's job)
builder.Services.AddOrdersPersistence(postgres).AddOrdersApplication();

// Messaging: consumes order-created and publishes OrderStatusChanged through the outbox
builder.Services.AddServiceBusMessaging(configuration);
builder.Services.AddScoped<OrderCreatedHandler>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<Heartbeat>();

builder.Services.AddOrdersTelemetry(configuration, "orders-worker");

await builder.Build().RunAsync();

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Orders.Api.Ai;
using Orders.Api.Health;
using Orders.Api.Infrastructure;
using Orders.Api.Realtime;
using Orders.Core.Application;
using Orders.Core.Infrastructure.Messaging;
using Orders.Core.Infrastructure.Persistence;
using Orders.Core.Observability;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var postgres = configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");
var messaging = configuration.GetMessagingOptions();

builder.Logging.Configure(options => options.ActivityTrackingOptions =
    ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// Domínio, aplicação e persistência
builder.Services.AddOrdersPersistence(postgres).AddOrdersApplication();

// HTTP
builder.Services
    .AddControllers(options => options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Gestão de Pedidos API", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, "Orders.Api.xml");
    if (File.Exists(xml))
    {
        options.IncludeXmlComments(xml);
    }
});

// Tempo real
builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton<IOrderNotifier, SignalROrderNotifier>();

// Health checks
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API em execução."), tags: [HealthEndpoints.LiveTag])
    .AddNpgSql(postgres, name: "postgres", timeout: TimeSpan.FromSeconds(5));

// Mensageria (Service Bus + outbox + consumidor de status)
if (messaging.Enabled)
{
    builder.Services.AddServiceBusMessaging(configuration);
    builder.Services.AddHostedService<StatusChangedConsumer>();
    healthChecks.AddCheck<ServiceBusHealthCheck>("servicebus");
}

// Módulo IA (opcional: ativo só com ANTHROPIC_API_KEY)
builder.Services.AddOrderAnalytics(configuration);

// Observabilidade
builder.Services.AddOrdersTelemetry(configuration, "orders-api", tracing => tracing
    .AddAspNetCoreInstrumentation(options =>
        options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
    .AddHttpClientInstrumentation());

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<OrdersHub>(OrdersHub.Path);
app.MapOrdersHealthChecks();

await app.MigrateDatabaseAsync();
await app.RunAsync();

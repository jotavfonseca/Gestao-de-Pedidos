using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Core.Infrastructure.Outbox;

namespace Orders.Core.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registra o <see cref="ServiceBusClient"/> (singleton), os senders e o
    /// publicador do outbox. Lê ConnectionStrings:ServiceBus.
    /// </summary>
    public static IServiceCollection AddServiceBusMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ServiceBus");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ServiceBus não configurada. Defina SERVICEBUS_CONNECTION_STRING no .env " +
                "ou desative a mensageria com Messaging:Enabled=false.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<MessagingOptions>().BindConfiguration(MessagingOptions.SectionName);
        services.AddSingleton(_ => new ServiceBusClient(connectionString, new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                MaxRetries = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                MaxDelay = TimeSpan.FromSeconds(5),
                TryTimeout = TimeSpan.FromSeconds(15)
            }
        }));
        services.AddSingleton<ServiceBusSenders>();
        services.AddHostedService<OutboxPublisher>();

        return services;
    }

    /// <summary>Lê a seção Messaging; usado para decidir se a mensageria deve ser registrada.</summary>
    public static MessagingOptions GetMessagingOptions(this IConfiguration configuration) =>
        configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
}

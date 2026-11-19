using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Core.Infrastructure.Outbox;

namespace Orders.Core.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ServiceBusClient"/> (singleton), the senders and the
    /// outbox publisher. Reads ConnectionStrings:ServiceBus.
    /// </summary>
    public static IServiceCollection AddServiceBusMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ServiceBus");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ServiceBus is not configured. Set SERVICEBUS_CONNECTION_STRING in .env " +
                "or disable messaging with Messaging:Enabled=false.");
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

    /// <summary>Reads the Messaging section; used to decide whether messaging is registered.</summary>
    public static MessagingOptions GetMessagingOptions(this IConfiguration configuration) =>
        configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Core.Infrastructure.Outbox;

namespace Orders.Core.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersPersistence(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<DomainEventsToOutboxInterceptor>();

        services.AddDbContext<OrderContext>((provider, options) => options
            .UseNpgsql(connectionString)
            .AddInterceptors(provider.GetRequiredService<DomainEventsToOutboxInterceptor>()));

        return services;
    }
}

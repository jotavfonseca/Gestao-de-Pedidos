using Microsoft.Extensions.DependencyInjection;
using Orders.Core.Application.Orders;

namespace Orders.Core.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateOrderHandler>();
        services.AddScoped<OrderQueries>();
        services.AddScoped<ProcessOrderHandler>();
        services.AddOptions<ProcessingOptions>().BindConfiguration(ProcessingOptions.SectionName);
        return services;
    }
}

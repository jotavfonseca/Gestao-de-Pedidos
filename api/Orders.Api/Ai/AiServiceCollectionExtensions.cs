using Anthropic;
using Microsoft.Extensions.Options;

namespace Orders.Api.Ai;

internal static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Registra o módulo de IA. Sem chave (Ai:ApiKey ou ANTHROPIC_API_KEY),
    /// registra uma implementação desativada e o endpoint responde 503.
    /// </summary>
    public static IServiceCollection AddOrderAnalytics(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiOptions>()
            .BindConfiguration(AiOptions.SectionName)
            .PostConfigure(options => options.ApiKey ??= configuration["ANTHROPIC_API_KEY"]);

        var settings = configuration.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
        settings.ApiKey ??= configuration["ANTHROPIC_API_KEY"];

        if (!settings.Enabled)
        {
            services.AddSingleton<IOrderAnalyticsService, DisabledOrderAnalyticsService>();
            return services;
        }

        services.AddSingleton(TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone));
        services.AddSingleton(provider => new AnthropicClient
        {
            ApiKey = provider.GetRequiredService<IOptions<AiOptions>>().Value.ApiKey!
        });
        services.AddScoped<OrderAnalyticsTools>();
        services.AddScoped<IOrderAnalyticsService, ClaudeOrderAnalyticsService>();
        return services;
    }
}

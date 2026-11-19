using Microsoft.EntityFrameworkCore;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Api.Infrastructure;

internal static class DatabaseMigrator
{
    private const int MaxAttempts = 10;

    /// <summary>
    /// Aplica as migrations pendentes antes de aceitar requisições, tentando
    /// novamente enquanto o PostgreSQL ainda estiver subindo.
    /// </summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderContext>();
                await db.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Banco de dados atualizado (migrations aplicadas).");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is not OperationCanceledException)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                logger.LogWarning(
                    "Banco indisponível para migrations (tentativa {Attempt}/{MaxAttempts}): {Error}. Nova tentativa em {Delay}s.",
                    attempt, MaxAttempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}

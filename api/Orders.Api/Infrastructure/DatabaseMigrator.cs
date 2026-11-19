using Microsoft.EntityFrameworkCore;
using Orders.Core.Infrastructure.Persistence;

namespace Orders.Api.Infrastructure;

internal static class DatabaseMigrator
{
    private const int MaxAttempts = 10;

    /// <summary>
    /// Applies pending migrations before accepting requests, retrying while
    /// PostgreSQL is still starting up.
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
                logger.LogInformation("Database is up to date (migrations applied).");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts && ex is not OperationCanceledException)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(attempt * 2, 10));
                logger.LogWarning(
                    "Database unavailable for migrations (attempt {Attempt}/{MaxAttempts}): {Error}. Retrying in {Delay}s.",
                    attempt, MaxAttempts, ex.Message, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}

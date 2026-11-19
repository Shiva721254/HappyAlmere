using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace HappyCity.Infrastructure;

// No SQL, parameters, search terms, hosts, connection strings or customer identifiers.
public sealed class SafeQueryTimingInterceptor(ILogger<SafeQueryTimingInterceptor> logger) : DbCommandInterceptor
{
    private long lastWarning;
    public void Record(TimeSpan duration)
    {
        if (duration.TotalMilliseconds < 250) return;
        var now = Environment.TickCount64;
        var previous = Interlocked.Read(ref lastWarning);
        if (previous != 0 && now - previous < 60_000) return;
        if (Interlocked.CompareExchange(ref lastWarning, now, previous) == previous)
            logger.LogWarning(new EventId(2001, "SlowDatabaseRead"), "Database read exceeded local threshold: {ElapsedMilliseconds} ms", duration.TotalMilliseconds);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result, CancellationToken cancellationToken = default)
    {
        Record(eventData.Duration);
        return ValueTask.FromResult(result);
    }
}

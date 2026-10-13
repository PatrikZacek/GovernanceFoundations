using System.Data.Common;
using System.Text.Json;
using Governance.Abstractions.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Hosting;

internal static class PersistenceErrors
{
    public static async Task<T> TranslateAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException or JsonException or InvalidDataException)
        {
            // Preserve internal diagnostics. The runtime still exposes only its safe reason code.
            // Cancellation and injected crash exceptions intentionally propagate unchanged.
            throw new PersistenceException(exception);
        }
    }
}

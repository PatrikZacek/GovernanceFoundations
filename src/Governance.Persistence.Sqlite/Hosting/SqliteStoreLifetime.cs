namespace Governance.Persistence.Sqlite.Hosting;

/// <summary>Serializes store operations and prevents disposal from releasing an in-use database lease.</summary>
internal sealed class SqliteStoreLifetime(SqliteHostLease lease, RuntimeDatabaseInitializer initializer) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private bool disposed;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (initialized)
            {
                return;
            }

            lease.Acquire();
            try
            {
                await initializer.InitializeAsync(cancellationToken);
                initialized = true;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!initialized)
            {
                throw new InvalidOperationException("Governance store must be initialized by its owning host.");
            }

            return await PersistenceErrors.TranslateAsync(operation);
        }
        finally
        {
            gate.Release();
        }
    }

    public Task RunAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        return RunAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            initialized = false;
            lease.Dispose();
        }
        finally
        {
            // Kept alive so operations already waiting can observe the disposed state safely.
            gate.Release();
        }
    }
}

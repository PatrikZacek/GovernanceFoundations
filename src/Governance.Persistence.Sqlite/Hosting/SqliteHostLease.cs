namespace Governance.Persistence.Sqlite.Hosting;

/// <summary>Exclusive ownership of one canonical local database path for the host lifetime.</summary>
internal sealed class SqliteHostLease(string databasePath) : IDisposable
{
    private FileStream? stream;

    public void Acquire()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        try
        {
            stream = new FileStream(databasePath + ".hostlock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException exception)
        {
            throw new InvalidOperationException("GovernanceHostAlreadyRunning", exception);
        }
    }

    public void Dispose()
    {
        stream?.Dispose();
        stream = null;
    }
}

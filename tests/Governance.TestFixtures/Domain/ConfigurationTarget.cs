using Microsoft.Data.Sqlite;

namespace Governance.TestFixtures.Domain;

/// <summary>Independent durable target: versioned writes and receipts commit atomically.</summary>
public sealed class ConfigurationTarget(string path)
{
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Pooling = false, DefaultTimeout = 10
        }.ToString());
        connection.Open();
        return connection;
    }

    public async Task InitializeAsync()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Entries (Tenant TEXT, Environment TEXT, Key TEXT, Value TEXT, Version INTEGER,
                PRIMARY KEY (Tenant, Environment, Key));
            CREATE TABLE IF NOT EXISTS Receipts (ExecutionKey TEXT PRIMARY KEY, Tenant TEXT, Environment TEXT, Key TEXT);
            INSERT OR IGNORE INTO Entries VALUES ('tenant-a', 'development', 'feature.new-search', 'false', 1);
            INSERT OR IGNORE INTO Entries VALUES ('tenant-a', 'production', 'feature.new-search', 'false', 1);
            INSERT OR IGNORE INTO Entries VALUES ('tenant-a', 'production', 'auth.secret', 'false', 1);
            """;
        await command.ExecuteNonQueryAsync();
    }

    public async Task<ConfigurationEntry?> ReadAsync(string tenant, string environment, string key)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Value, Version FROM Entries WHERE Tenant=$tenant AND Environment=$env AND Key=$key";
        Bind(command, tenant, environment, key);
        using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new(reader.GetString(0), reader.GetInt64(1)) : null;
    }

    public async Task ForceExternalChangeAsync(string environment, string key, string value)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Entries SET Value=$value, Version=Version+1 WHERE Tenant=$tenant AND Environment=$env AND Key=$key";
        Bind(command, "tenant-a", environment, key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> ApplyAsync(string tenant, UpdateConfigurationValue payload, string executionKey)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM Receipts WHERE ExecutionKey=$execution";
        command.Parameters.AddWithValue("$execution", executionKey);
        if ((long)(await command.ExecuteScalarAsync())! == 1) return true;
        command.CommandText = "UPDATE Entries SET Value=$value, Version=Version+1 WHERE Tenant=$tenant AND Environment=$env AND Key=$key AND Version=$version";
        Bind(command, tenant, payload.Environment, payload.Key);
        command.Parameters.AddWithValue("$value", payload.NewValue);
        command.Parameters.AddWithValue("$version", payload.ExpectedVersion);
        if (await command.ExecuteNonQueryAsync() != 1) return false;
        command.CommandText = "INSERT INTO Receipts VALUES ($execution, $tenant, $env, $key)";
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
        return true;
    }

    public async Task<bool> HasReceiptAsync(string tenant, string executionKey)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Receipts WHERE Tenant=$tenant AND ExecutionKey=$execution";
        command.Parameters.AddWithValue("$tenant", tenant);
        command.Parameters.AddWithValue("$execution", executionKey);
        return (long)(await command.ExecuteScalarAsync())! == 1;
    }

    public async Task<long> ReceiptCountAsync()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Receipts";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static void Bind(SqliteCommand command, string tenant, string environment, string key)
    {
        command.Parameters.AddWithValue("$tenant", tenant);
        command.Parameters.AddWithValue("$env", environment);
        command.Parameters.AddWithValue("$key", key);
    }
}

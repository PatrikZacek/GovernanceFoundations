using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Storage;

public sealed class RuntimeDbFactory : IDbContextFactory<RuntimeDb>
{
    private readonly DbContextOptions<RuntimeDb> options;

    public RuntimeDbFactory(string databasePath)
    {
        var connection = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 10
        };
        options = new DbContextOptionsBuilder<RuntimeDb>().UseSqlite(connection.ToString()).Options;
    }

    public RuntimeDb CreateDbContext() => new(options);
}

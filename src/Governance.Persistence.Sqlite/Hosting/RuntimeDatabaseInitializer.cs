using Governance.Persistence.Sqlite.Storage;
using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Hosting;

internal sealed class RuntimeDatabaseInitializer(IDbContextFactory<RuntimeDb> factory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        await database.Database.MigrateAsync(cancellationToken);
    }
}

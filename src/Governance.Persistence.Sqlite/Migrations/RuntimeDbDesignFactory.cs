using Governance.Persistence.Sqlite.Storage;
using Microsoft.EntityFrameworkCore.Design;

namespace Governance.Persistence.Sqlite.Migrations;

public sealed class RuntimeDbDesignFactory : IDesignTimeDbContextFactory<RuntimeDb>
{
    public RuntimeDb CreateDbContext(string[] args)
    {
        var path = args.Length == 0 ? "governance-design.db" : args[0];
        return new RuntimeDbFactory(path).CreateDbContext();
    }
}

using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Governance.Persistence.Sqlite.Storage;

public static class SqliteGovernanceServices
{
    public static IServiceCollection AddGovernanceSqlite(this IServiceCollection services, string databasePath)
    {
        services.AddSingleton<IGovernanceStore>(s => new SqliteGovernanceStore(databasePath, s.GetRequiredService<TimeProvider>(), s.GetRequiredService<IGovernanceFaults>()));
        return services;
    }
}


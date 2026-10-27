using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Core;
using Governance.Core.Hosting;
using Governance.Core.Model;
using Governance.Persistence.Sqlite.Storage;
using Governance.Testing.Domain;
using Governance.TestFixtures.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Governance.TestFixtures;

/// <summary>Shared, executable test fixture; no application or linked sources are required.</summary>
public static class DemoHost
{
    public static void Configure(GovernanceBuilder builder, string version = "configuration-demo/1")
    {
        builder.ModelVersion(version);
        builder.Action<UpdateConfigurationValue>("configuration.update", 1)
            .RequireCapability("configuration.propose")
            .ValidateWith<ConfigurationValidator>()
            .FactsWith<ConfigurationAdapter>()
            .UsePolicy<ConfigurationPolicy>("configuration.environment", "1", mayRequireApproval: true)
            .Approval("configuration.approve", TimeSpan.FromMinutes(30))
            .ExecuteWith<ConfigurationAdapter>("1")
            .VerifyWith<ConfigurationAdapter>()
            .ReconcileWith<ConfigurationAdapter>();
    }

    public static IHost Create(string directory, TimeProvider? time = null, FaultInjector? faults = null,
        DemoBehavior? behavior = null, DemoAuthorization? authorization = null,
        string version = "configuration-demo/1", IGovernanceStore? store = null)
    {
        Directory.CreateDirectory(directory);
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        var services = builder.Services;
        services.AddSingleton(time ?? TimeProvider.System);
        services.AddSingleton<IGovernanceFaults>(faults ?? new FaultInjector());
        services.AddSingleton(behavior ?? new DemoBehavior());
        services.AddSingleton(authorization ?? new DemoAuthorization());
        services.AddSingleton<IGovernanceAuthorizationService>(s => s.GetRequiredService<DemoAuthorization>());
        services.AddScoped<FixturePrincipal>();
        services.AddScoped<ITrustedPrincipalAccessor>(s => s.GetRequiredService<FixturePrincipal>());
        services.AddSingleton(new ConfigurationTarget(Path.Combine(directory, "target.db")));
        services.AddSingleton<ConfigurationValidator>();
        services.AddSingleton<ConfigurationAdapter>();
        services.AddSingleton<ConfigurationPolicy>();
        services.AddGovernance(b => Configure(b, version), o => o.PolicyTimeout = TimeSpan.FromMilliseconds(100));
        if (store is null)
            services.AddGovernanceSqlite(Path.Combine(directory, "governance.db"));
        else
            services.AddSingleton(store);
        return builder.Build();
    }

    public static async Task StartAsync(IHost host)
    {
        await host.Services.GetRequiredService<ConfigurationTarget>().InitializeAsync();
        await host.StartAsync();
    }

    public static async Task StopAsync(IHost host)
    {
        try { await host.StopAsync(); }
        finally
        {
            if (host is IAsyncDisposable asyncHost) await asyncHost.DisposeAsync();
            else host.Dispose();
        }
    }

    public static async Task<T> As<T>(IHost host, string actor, Func<GovernanceContext, Task<T>> action,
        string tenant = "tenant-a", string? behalf = null)
    {
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<FixturePrincipal>().Current =
            new(tenant, actor, actor != "service", behalf);
        return await action(scope.ServiceProvider.GetRequiredService<GovernanceContext>());
    }
}

public sealed class FixturePrincipal : ITrustedPrincipalAccessor
{
    public TrustedPrincipal Current { get; set; } = new("tenant-a", "worker");
}

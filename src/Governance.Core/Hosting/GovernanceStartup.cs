using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Governance.Core.Hosting;

internal sealed class GovernanceStartup(IServiceProvider provider, IGovernanceStore store) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = provider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        _ = serviceProvider.GetRequiredService<ITrustedPrincipalAccessor>();
        _ = serviceProvider.GetRequiredService<IGovernanceAuthorizationService>();
        foreach (var d in serviceProvider.GetRequiredService<GovernanceModel>().Actions)
        {
            d.ValidateServices(serviceProvider);
        }

        await store.InitializeAsync(ct);
        await serviceProvider.GetRequiredService<GovernanceContext>().RecoverAsync(ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}


using Governance.Abstractions;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Core.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Governance.Core.Hosting;

public static class GovernanceServices
{
    public static IServiceCollection AddGovernance(this IServiceCollection services,
        Action<GovernanceBuilder> configure, Action<GovernanceOptions>? options = null)
    {
        var builder = new GovernanceBuilder();
        configure(builder);
        services.AddSingleton(builder.Build());
        var governanceOptions = new GovernanceOptions();
        options?.Invoke(governanceOptions);
        if (governanceOptions.PayloadLimitBytes < 1
            || governanceOptions.PolicyTimeout <= TimeSpan.Zero
            || governanceOptions.MaxRecoveryEvaluationAttempts is < 1 or > 10)
        {
            throw new InvalidOperationException("Invalid governance options.");
        }

        services.AddSingleton(governanceOptions);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IGovernanceFaults, NoGovernanceFaults>();
        services.AddScoped<GovernanceContext>();
        services.AddHostedService<GovernanceStartup>();
        return services;
    }
}


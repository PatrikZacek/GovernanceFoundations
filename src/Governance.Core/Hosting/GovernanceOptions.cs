namespace Governance.Core.Hosting;

public sealed class GovernanceOptions
{
    public int PayloadLimitBytes { get; set; } = 64 * 1024;
    public TimeSpan PolicyTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxRecoveryEvaluationAttempts { get; set; } = 3;
}


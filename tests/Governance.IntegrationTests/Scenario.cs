using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.ConfigurationDemo;
using Governance.ConfigurationDemo.Domain;
using Governance.Core;
using Governance.Testing.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Governance.IntegrationTests;

public sealed class Scenario : IAsyncDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "governance-acceptance-" + Guid.NewGuid());
    public ManualTimeProvider Time { get; } = new(new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero));
    public FaultInjector Faults { get; } = new(); public DemoBehavior Behavior { get; } = new(); public DemoAuthorization Auth { get; } = new();
    public IHost Host { get; private set; } = null!;
    public ConfigurationTarget Target => Host.Services.GetRequiredService<ConfigurationTarget>();
    public IGovernanceStore Store => Host.Services.GetRequiredService<IGovernanceStore>();
    public static async Task<Scenario> Create()
    {
        var s = new Scenario();
        await s.Start();
        return s;
    }
    private async Task Start(string version = "configuration-demo/1")
    {
        Host = DemoHost.Create(Directory, Time, Faults, Behavior, Auth, version);
        await DemoHost.StartAsync(Host);
    }
    public async Task Restart(string version = "configuration-demo/1")
    {
        await DemoHost.StopAsync(Host);
        await Start(version);
    }
    public Task<T> As<T>(string actor, Func<GovernanceContext, Task<T>> f, string tenant = "tenant-a", string? behalf = null) => DemoHost.As(Host, actor, f, tenant, behalf);
    public SubmissionOptions Options(string? key = null) => new(key ?? Guid.NewGuid().ToString(), Time.GetUtcNow().AddHours(1));
    public Task<GovernanceResult> Submit(string environment = "development", string key = "feature.new-search", string value = "true", long version = 1, SubmissionOptions? options = null) => As("alice", g => g.SubmitAsync(new UpdateConfigurationValue(environment, key, value, version), options ?? Options()));
    public Task<GovernanceResult> Execute(Guid id) => As("worker", g => g.ExecuteAsync(id));
    public Task<GovernanceResult> Get(Guid id) => As("alice", g => g.GetAsync(id));
    public async Task<GovernanceResult> Approve(Guid id, string actor = "bob")
    {
        var a = (await Get(id)).Action!;
        return await As(actor, g => g.ApproveAsync(a.ApprovalId!.Value, a.Revision, "reviewed"));
    }
    public async ValueTask DisposeAsync() => await DemoHost.StopAsync(Host);
}

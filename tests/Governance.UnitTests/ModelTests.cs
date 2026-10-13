using Governance.Abstractions;
using Governance.Abstractions.Action;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.ConfigurationDemo;
using Governance.ConfigurationDemo.Domain;
using Governance.Core.Model;
using Governance.Testing.Domain;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Governance.UnitTests;

public sealed class ModelTests
{
    [Fact] public void Missing_contracts_fail_configuration() => Assert.Throws<InvalidOperationException>(Invalid);
    private static GovernanceModel Invalid()
    {
        var b = new GovernanceBuilder().ModelVersion("1");
        b.Action<int>("x", 1);
        return b.Build();
    }
    [Fact]
    public void Duplicate_registration_fails()
    {
        var b = new GovernanceBuilder();
        DemoHost.Configure(b);
        DemoHost.Configure(b);
        Assert.Throws<InvalidOperationException>(() => b.Build());
    }
    [Fact]
    public void Manifest_stable_and_sensitive_to_version()
    {
        var a = new GovernanceBuilder();
        DemoHost.Configure(a);
        var b = new GovernanceBuilder();
        DemoHost.Configure(b);
        Assert.Equal(a.Build().ManifestHash, b.Build().ManifestHash);
        var c = new GovernanceBuilder();
        DemoHost.Configure(c, "2");
        Assert.NotEqual(a.Build().ManifestHash, c.Build().ManifestHash);
    }
    [Fact]
    public void Canonical_order_is_stable()
    {
        Assert.Equal("{\"a\":2,\"z\":1}", GovernanceJson.Canonical(new
        {
            z = 1,
            a = 2
        }, new
        {
            z = 1,
            a = 2
        }.GetType()));
    }
    [Fact]
    public async Task M1_memory_allow_deny_approval_no_submission_effect()
    {
        var time = new ManualTimeProvider(DateTimeOffset.UtcNow);
        var store = new MemoryGovernanceStore(time);
        var host = DemoHost.Create(Path.Combine(Path.GetTempPath(), "governance-m1-" + Guid.NewGuid()), time, store: store);
        await DemoHost.StartAsync(host);
        try
        {
            foreach (var (env, key, state) in new[] { ("development", "feature.new-search", ActionState.Ready), ("production", "feature.new-search", ActionState.AwaitingApproval), ("production", "auth.secret", ActionState.Denied) })
            {
                var r = await DemoHost.As(
host,
"alice",
g => g.SubmitAsync(new UpdateConfigurationValue(env, key, "true", 1), new(Guid.NewGuid().ToString(), time.GetUtcNow().AddHours(1)))
);
                Assert.Equal(state, r.Action!.State);
            }
            Assert.Equal(0, host.Services.GetRequiredService<DemoBehavior>().DispatchCount);
        }
        finally { await DemoHost.StopAsync(host); }
    }
}


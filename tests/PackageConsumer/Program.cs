using System.Collections.Immutable;
using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Core;
using Governance.Core.Hosting;
using Governance.Persistence.Sqlite.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var database = Path.Combine(AppContext.BaseDirectory, "consumer.db");
var adapter = new ConsumerAdapter();
Guid actionId;
await Run(async context =>
{
    var result = await context.SubmitAsync(new ConsumerAction("smoke"),
        new("consumer-smoke", DateTimeOffset.UtcNow.AddHours(1)));
    Check(result.Action?.State == ActionState.Ready, "proposal did not become ready");
    actionId = result.ActionId!.Value;
    var executed = await context.ExecuteAsync(actionId);
    Check(executed.Action?.State == ActionState.Succeeded, "execution/verification failed");
});
await Run(async context =>
{
    var repeated = await context.SubmitAsync(new ConsumerAction("smoke"),
        new("consumer-smoke", DateTimeOffset.UtcNow.AddHours(1)));
    Check(repeated.Action?.State == ActionState.Succeeded, "durable action was not recovered");
    var history = await context.GetHistoryAsync(repeated.ActionId!.Value);
    Check(history.Events.Any(e => e.Type == AuditEventType.RecordVerification), "audit did not survive restart");
    await context.ExecuteAsync(repeated.ActionId.Value);
    Check(adapter.Dispatches == 1, "completed action was executed twice");
});
Console.WriteLine("PACKAGE_SMOKE_OK: all three packages; SQLite migrations, execution, verification, restart, idempotency and audit.");

async Task Run(Func<GovernanceContext, Task> use)
{
    var builder = Host.CreateApplicationBuilder();
    builder.Logging.ClearProviders();
    builder.Services.AddSingleton<ITrustedPrincipalAccessor, ConsumerPrincipal>();
    builder.Services.AddSingleton<IGovernanceAuthorizationService, ConsumerAuthorization>();
    builder.Services.AddSingleton(adapter);
    builder.Services.AddGovernance(b =>
    {
        b.ModelVersion("consumer/1");
        b.Action<ConsumerAction>("consumer.write", 1)
            .RequireCapability("consumer.write")
            .ValidateWith<ConsumerAdapter>().FactsWith<ConsumerAdapter>()
            .ExecuteWith<ConsumerAdapter>("1").VerifyWith<ConsumerAdapter>();
    });
    builder.Services.AddGovernanceSqlite(database);
    var host = builder.Build();
    try
    {
        await host.StartAsync();
        using var scope = host.Services.CreateScope();
        await use(scope.ServiceProvider.GetRequiredService<GovernanceContext>());
        await host.StopAsync();
    }
    finally
    {
        if (host is IAsyncDisposable asyncHost) await asyncHost.DisposeAsync();
        else host.Dispose();
    }
}

static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

public sealed record ConsumerAction(string Value);
public sealed class ConsumerPrincipal : ITrustedPrincipalAccessor
{
    public TrustedPrincipal Current => new("consumer", "consumer-worker");
}
public sealed class ConsumerAuthorization : IGovernanceAuthorizationService
{
    public ValueTask<bool> AuthorizeAsync(AuthorizationRequest request, CancellationToken ct) => ValueTask.FromResult(true);
}
public sealed class ConsumerAdapter : IActionValidator<ConsumerAction>, IActionFactsProvider<ConsumerAction>,
    IActionExecutor<ConsumerAction>, IActionVerifier<ConsumerAction>
{
    public int Dispatches { get; private set; }
    public ValidationResult<ConsumerAction> Validate(ConsumerAction payload) => new(true, payload, "consumer/target", 1);
    public ValueTask<FactsSnapshot> ReadAsync(ConsumerAction payload, TrustedPrincipal principal, CancellationToken ct) =>
        ValueTask.FromResult(new FactsSnapshot("1", ImmutableDictionary<string, string>.Empty));
    public Task<ExecutionResult> ExecuteAsync(ExecutionContext<ConsumerAction> context, CancellationToken ct)
    {
        Dispatches++;
        return Task.FromResult(new ExecutionResult(Effect.Applied, RuntimeReasonCode.Ok));
    }
    public Task<VerificationResult> VerifyAsync(ExecutionContext<ConsumerAction> context, CancellationToken ct) =>
        Task.FromResult(new VerificationResult(Dispatches == 1 ? VerificationOutcome.Verified : VerificationOutcome.Mismatch,
            RuntimeReasonCode.Ok));
}

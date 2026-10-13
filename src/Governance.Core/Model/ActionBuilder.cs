using Governance.Abstractions.Abstractions;

namespace Governance.Core.Model;

public sealed class ActionBuilder<T>
{
    private readonly string id;
    private readonly int schema;
    private string capability = "";
    private Type? validator, facts, executor, verifier, reconciler;
    private string adapterVersion = "";
    private ApprovalSettings? approval;
    private readonly List<PolicyRegistration> policies = [];

    internal ActionBuilder(string id, int schema)
    {
        this.id = id;
        this.schema = schema;
    }

    public ActionBuilder<T> ValidateWith<TValidator>() where TValidator : IActionValidator<T>
    {
        validator = typeof(TValidator);
        return this;
    }

    public ActionBuilder<T> FactsWith<TFactsProvider>() where TFactsProvider : IActionFactsProvider<T>
    {
        facts = typeof(TFactsProvider);
        return this;
    }

    public ActionBuilder<T> RequireCapability(string value)
    {
        capability = value;
        return this;
    }

    public ActionBuilder<T> UsePolicy<TActionPolicy>(string id, string version, bool mayRequireApproval = false)
        where TActionPolicy : IActionPolicy<T>
    {
        policies.Add(new PolicyRegistration(id, version, typeof(TActionPolicy), mayRequireApproval));
        return this;
    }

    public ActionBuilder<T> Approval(string capability, TimeSpan lifetime)
    {
        approval = new ApprovalSettings(capability, lifetime);
        return this;
    }

    public ActionBuilder<T> ExecuteWith<TExecutor>(string version) where TExecutor : IActionExecutor<T>
    {
        executor = typeof(TExecutor);
        adapterVersion = version;
        return this;
    }

    public ActionBuilder<T> VerifyWith<TVerifier>() where TVerifier : IActionVerifier<T>
    {
        verifier = typeof(TVerifier);
        return this;
    }

    public ActionBuilder<T> ReconcileWith<TReconciler>() where TReconciler : IActionReconciler<T>
    {
        reconciler = typeof(TReconciler);
        return this;
    }

    internal IActionDefinition Build()
    {
        ValidateIdentity();
        ValidateAdapters();
        ValidatePolicies();
        ValidateApproval();
        return new ActionDefinition<T>(id, schema, capability, validator!, facts!, executor!, verifier!, reconciler,
            adapterVersion, approval, [..policies]);
    }

    private void ValidateIdentity()
    {
        if (string.IsNullOrWhiteSpace(id) || schema < 1 || string.IsNullOrWhiteSpace(capability))
        {
            throw ConfigurationError("An action requires an ID, a positive schema version and a capability.");
        }
    }

    private void ValidateAdapters()
    {
        if (validator is null || facts is null || executor is null || verifier is null ||
            string.IsNullOrWhiteSpace(adapterVersion))
        {
            throw ConfigurationError("Validator, facts provider, versioned executor and verifier are required.");
        }
    }

    private void ValidatePolicies()
    {
        if (policies.Any(policy => string.IsNullOrWhiteSpace(policy.Id) || string.IsNullOrWhiteSpace(policy.Version)))
        {
            throw ConfigurationError("Each policy requires an explicit ID and version.");
        }

        if (policies.Select(policy => policy.Id).Distinct().Count() != policies.Count)
        {
            throw ConfigurationError("Policy IDs must be unique within an action.");
        }
    }

    private void ValidateApproval()
    {
        if (policies.Any(policy => policy.MayRequireApproval) && approval is null)
        {
            throw ConfigurationError("An approval policy requires approval settings.");
        }

        if (approval is not null &&
            (approval.Lifetime <= TimeSpan.Zero || string.IsNullOrWhiteSpace(approval.Capability)))
        {
            throw ConfigurationError("Approval requires a capability and a positive lifetime.");
        }
    }

    private InvalidOperationException ConfigurationError(string message) => new($"Action '{id}': {message}");
}

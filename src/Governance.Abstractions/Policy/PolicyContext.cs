
using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Policy;

public sealed record PolicyContext<T>(T Payload, TrustedPrincipal Principal, FactsSnapshot Facts, DateTimeOffset Now);


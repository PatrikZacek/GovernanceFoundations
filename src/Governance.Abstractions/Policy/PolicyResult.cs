
using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Policy;

public sealed record PolicyResult(PolicyDecision Decision, ReasonCode ReasonCode, string SafeMessage);


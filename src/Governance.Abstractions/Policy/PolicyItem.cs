
using Governance.Abstractions.Contracts;

namespace Governance.Abstractions.Policy;

public sealed record PolicyItem(string PolicyId, string PolicyVersion, PolicyDecision Decision, ReasonCode ReasonCode, string SafeMessage);


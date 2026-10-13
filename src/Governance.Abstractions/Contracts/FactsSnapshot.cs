using System.Collections.Immutable;

namespace Governance.Abstractions.Contracts;

public sealed record FactsSnapshot(
    string Version,
    ImmutableDictionary<string, string> Values);


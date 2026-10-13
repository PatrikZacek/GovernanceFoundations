using System.Collections.Immutable;

namespace Governance.Core.Model;

public sealed record GovernanceModel(ImmutableArray<IActionDefinition> Actions, string ManifestHash)
{
    public IActionDefinition? Find(string id, int schema) => Actions.FirstOrDefault(d => d.Id == id && d.Schema == schema);
}


using System.Collections.Immutable;

namespace Governance.Core.Model;

public sealed class GovernanceBuilder
{
    private string version = "";
    private readonly List<Func<IActionDefinition>> actions = [];
    public GovernanceBuilder ModelVersion(string value)
    {
        version = value;
        return this;
    }

    public ActionBuilder<T> Action<T>(string id, int schema)
    {
        var b = new ActionBuilder<T>(id, schema);
        actions.Add(b.Build);
        return b;
    }

    public GovernanceModel Build()
    {
        var definitions = actions.Select(a => a()).ToImmutableArray();
        if (string.IsNullOrWhiteSpace(version) || definitions.Length == 0 || definitions.GroupBy(d => (d.Id, d.Schema)).Any(g => g.Count() > 1)
|| definitions.GroupBy(d => d.PayloadType).Any(g => g.Count() > 1))
        {
            throw new InvalidOperationException("Invalid or duplicate governance model registration.");
        }

        return new(definitions, GovernanceJson.Hash(version + "\n" + string.Join("\n", definitions.OrderBy(d => d.Id, StringComparer.Ordinal).ThenBy(d => d.Schema).Select(d => d.Manifest))));
    }
}


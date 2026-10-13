namespace Governance.Core.Model;

public sealed record NormalizedPayload(bool Valid, string Json, string ResourceId, long ExpectedVersion);


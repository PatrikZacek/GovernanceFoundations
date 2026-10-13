using Governance.Abstractions.Contracts;
using Governance.Persistence.Sqlite.Storage;

namespace Governance.Persistence.Sqlite.Mapping;

internal static class AuditRowMapper
{
    public static AuditRow Create(AuditEvent entry) => new()
    {
        Id = entry.Id,
        TenantId = entry.TenantId,
        ActionId = entry.ActionId,
        Sequence = entry.Sequence,
        Type = entry.Type,
        At = entry.At,
        ActorId = entry.ActorId,
        CorrelationId = entry.CorrelationId,
        Code = entry.Code,
        SafeMetadataJson = entry.SafeMetadataJson
    };

    public static AuditEvent Read(AuditRow row) => new(
        row.Id, row.TenantId, row.ActionId, row.Sequence, row.Type,
        row.At, row.ActorId, row.CorrelationId, row.Code)
    {
        SafeMetadataJson = row.SafeMetadataJson
    };
}

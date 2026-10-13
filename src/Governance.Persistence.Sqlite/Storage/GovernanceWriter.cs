using Governance.Abstractions.Abstractions;
using Governance.Abstractions.Contracts;
using Governance.Abstractions.Enums;
using Governance.Core;
using Governance.Persistence.Sqlite.Mapping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Governance.Persistence.Sqlite.Storage;

/// <summary>Owns transactions spanning lifecycle state, typed history and public audit.</summary>
internal sealed class GovernanceWriter(IDbContextFactory<RuntimeDb> factory, TimeProvider time, IGovernanceFaults faults)
{
    public async Task<StoreResult> InsertAsync(GovernedAction action, string correlationId, CancellationToken cancellationToken)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            database.Actions.Add(ActionRowMapper.Create(action));
            database.Audit.Add(AuditRowMapper.Create(GovernanceTransitions.Event(
                action, AuditEventType.ProposalSubmitted, action.Proposer.PrincipalId, correlationId, time.GetUtcNow())));
            faults.Hit(GovernanceFaultPoint.BeforeSubmissionCommit);
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new StoreResult(StoreResultCode.Ok, action);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            // The failed transaction/context have already been disposed before this lookup.
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            var existing = await database.Actions.AsNoTracking().SingleOrDefaultAsync(
                row => row.TenantId == action.TenantId && row.ProposerId == action.Proposer.PrincipalId
                    && row.ActionTypeId == action.ActionTypeId && row.IdempotencyKey == action.IdempotencyKey,
                cancellationToken);
            if (existing is null)
            {
                throw;
            }

            var original = await GovernanceReader.ReadAsync(database, existing, cancellationToken);
            var code = original.ProposalFingerprint == action.ProposalFingerprint
                ? StoreResultCode.Ok : StoreResultCode.IdempotencyConflict;
            return new StoreResult(code, original);
        }
    }

    public async Task<StoreResult> ApplyAsync(StoreCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await using var database = await factory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            var row = await database.Actions.SingleOrDefaultAsync(
                action => action.TenantId == command.TenantId && action.ActionId == command.ActionId, cancellationToken);
            if (row is null)
            {
                return new StoreResult(StoreResultCode.NotFoundOrUnauthorized, null);
            }

            var original = await GovernanceReader.ReadAsync(database, row, cancellationToken);
            var result = GovernanceTransitions.Apply(original, command, time.GetUtcNow());
            if (!result.IsSuccess || result.Action!.Revision == original.Revision)
            {
                return result;
            }

            var next = result.Action!;
            ActionRowMapper.UpdateLifecycle(row, next);
            GovernanceHistoryWriter.Append(database, next, command);
            database.Audit.Add(AuditRowMapper.Create(GovernanceTransitions.Event(
                next, AuditEventTypes.FromCommand(command.Kind), command.ActorId, command.CorrelationId, time.GetUtcNow())));
            await database.SaveChangesAsync(cancellationToken);
            faults.Hit(command.Kind == CommandKind.Claim
                ? GovernanceFaultPoint.BeforeClaimCommit : GovernanceFaultPoint.BeforeTransitionCommit);
            await transaction.CommitAsync(cancellationToken);
            if (command.Kind == CommandKind.Claim)
            {
                faults.Hit(GovernanceFaultPoint.AfterClaimCommit);
            }

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            var current = await new GovernanceReader(factory).FindAsync(command.TenantId, command.ActionId, cancellationToken);
            return new StoreResult(StoreResultCode.ConcurrencyConflict, current);
        }
    }

    public async Task RecordIngressAsync(IngressEvent entry, CancellationToken cancellationToken)
    {
        await using var database = await factory.CreateDbContextAsync(cancellationToken);
        database.IngressAudit.Add(new IngressAuditRow
        {
            Id = entry.Id,
            TenantId = entry.TenantId,
            At = entry.At,
            CorrelationId = entry.CorrelationId,
            Code = entry.Code
        });
        await database.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        const int primaryKeyConstraint = 1555;
        const int uniqueConstraint = 2067;
        return exception.InnerException is SqliteException sqlite
            && sqlite.SqliteExtendedErrorCode is primaryKeyConstraint or uniqueConstraint;
    }
}

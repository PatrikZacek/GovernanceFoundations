# GovernanceFoundations

Governed actions for .NET 10: validate proposals, evaluate policies, obtain approval,
execute once, verify the effect, and recover interrupted work with an audit trail.

| Package | Purpose | Internal dependency |
| --- | --- | --- |
| Governance.Abstractions | Public contracts, policies, states and results | None |
| Governance.Core | Lifecycle, model builder and hosting | Governance.Abstractions |
| Governance.Persistence.Sqlite | Durable EF Core SQLite store and migrations | Governance.Core |

The proposed initial version is **0.1.0**. Packages are prepared for review; availability
on NuGet.org is not implied. Testing utilities and test fixtures are not packaged.
The repository currently has no license grant. No license expression is claimed in
the packages; the owner must resolve licensing before public distribution.

## Build and verify

Install the SDK pinned in `global.json`, then run from the repository root:

```powershell
dotnet restore GovernanceFoundations.slnx
dotnet build GovernanceFoundations.slnx -c Release --no-restore
dotnet test GovernanceFoundations.slnx -c Release --no-build
dotnet pack GovernanceFoundations.slnx -c Release --no-build -o artifacts/packages
pwsh ./eng/Verify-Packages.ps1 -Version 0.1.0
pwsh ./eng/Smoke-Test.ps1 -Version 0.1.0
```

CI runs the same checks on Linux and Windows. The consumer smoke test copies a
standalone consumer outside the repository, restores all three packages from the
local feed using an empty package cache, and exercises hosting and SQLite recovery.
No `ProjectReference` or linked source files are used by that consumer.

## Using the libraries

Install `Governance.Persistence.Sqlite` for the SQLite-backed runtime; its internal
dependencies bring in Core and Abstractions. Use `AddGovernance` from
`Governance.Core.Hosting` to register an explicit action model and
`AddGovernanceSqlite` from `Governance.Persistence.Sqlite.Storage` to configure
storage. Supply your own trusted principal accessor, authorization service,
validator, facts provider, policies, executor, verifier and optional reconciler.
Register concrete adapter types in dependency injection. Start the host before
resolving a scoped `GovernanceContext` and submitting actions.

SQLite storage owns an exclusive local database lease for the host lifetime.
Run one host per database path. Executors must use an idempotent target protocol
and reconciliation; an unknown outcome is not permission to repeat a write.

See [the standalone consumer](tests/PackageConsumer/Program.cs) for a minimal
working registration and [release instructions](docs/RELEASING.md) for versioning,
artifact review, approval and NuGet.org setup.

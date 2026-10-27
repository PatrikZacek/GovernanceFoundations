# Releasing GovernanceFoundations

## Current state

The initial proposed version is `0.1.0`. No release, tag, public package, license
grant, NuGet credential or GitHub environment is created by this change.
The repository owner must confirm authorship, package ID availability/ownership,
and legal distribution rights before publishing. The repository has no license;
choose and commit an authorized license and matching package metadata if a public
license grant is intended. Do not invent a license or infer one from dependencies.

## Local review

Use the exact SDK in `global.json`. `Directory.Packages.props` centralizes external
versions; committed `packages.lock.json` files fix the transitive restore graph.
Change central versions intentionally, run restore, review lock changes and audit
warnings, and commit the updated locks. Test utilities, the executable fixture and
the standalone consumer are not packable. The consumer is intentionally outside
the solution, because it must be restored only after the packages exist.

```powershell
$env:CI = 'true'
dotnet restore GovernanceFoundations.slnx --locked-mode
dotnet build GovernanceFoundations.slnx -c Release --no-restore
dotnet test GovernanceFoundations.slnx -c Release --no-build
dotnet pack GovernanceFoundations.slnx -c Release --no-build -o artifacts/packages
pwsh ./eng/Verify-Packages.ps1 -Version 0.1.0
pwsh ./eng/Smoke-Test.ps1 -Version 0.1.0
pwsh ./eng/Verify-Reproducibility.ps1 -Version 0.1.0
```

Commit source changes before the final build so SourceLink and repository metadata
identify the reviewed commit. Use a clean output feed for each version. Expected:
three `.nupkg` and three matching `.snupkg` files, each at the same version. Internal
dependencies use ordinary `ProjectReference`; pack turns them into same-version
minimum NuGet dependencies. EF design tooling and SourceLink remain private.
The verifier rejects extra/missing packages, wrong versions or repository commits,
incorrect internal dependencies, missing README/assembly/PDB, missing SourceLink,
or test binaries/tooling dependencies. The smoke test maps `Governance.*` exclusively
to the local feed, uses a fresh global package cache and no project references.

Deterministic compilation, portable PDBs, SourceLink and CI path normalization are
enabled. SDK 10.0.403 supports deterministic NuGet archive metadata; a fixed
`DeterministicTimestamp` also removes file modification times from the archive
inputs. CI rebuilds and compares SHA-256 of all six complete archives.
Reproduction requires the same SDK, source commit, restore graph and build options;
the check is per operating system, not a promise of identical native assets across
different operating systems. SourceLink URLs become available once the commit is
pushed to GitHub.

## Preparing a version

1. Merge reviewed changes and green Linux/Windows CI into `main`.
2. Obtain the user's explicit approval to create the actual release/tag. This task
   does not grant that approval.
3. Create a stable `vMAJOR.MINOR.PATCH` tag on the reviewed commit in `main`, then
   push that tag. The release workflow derives the version from the tag, verifies
   it belongs to `main`, restores in locked mode, builds, runs every test, packs,
   validates, smoke tests and checks reproducibility. It uploads six archives and
   `SHA256SUMS`. A tag push only prepares artifacts and never publishes.
4. Inspect the workflow results, package metadata, README, license decision,
   dependency graph, SourceLink commit and all six checksums. There is no automatic
   GitHub Release creation.

## Explicit NuGet.org publishing setup

Publishing uses a scoped NuGet API key; trusted publishing is not enabled by this
change. A repository administrator must perform all of the following deliberately:

1. Create the `nuget-production` GitHub environment. Configure required reviewers
   and **prevent self review**. Restrict deployment to `main`; disable administrator
   bypass where supported. The publishing job checks protection via the GitHub API
   and fails closed if it cannot verify reviewers and the self-review restriction.
2. Store a NuGet.org API key as **environment secret** `NUGET_API_KEY`, limited to
   the three package IDs and the shortest suitable expiry. Do not use a repository
   secret or commit a key. No credential is available to build/PR jobs.
3. Set repository variable `NUGET_PUBLISH_ENABLED` to exactly `true` only after the
   environment and legal/package ownership review are complete. Keep it unset or
   false during preparation and turn it off after publication if desired.
4. With the user's release approval, manually run **Prepare and approve NuGet
   release** from `main`, supply the existing tag and opt in with `publish=true`.
   It rebuilds and uploads the exact artifacts that will be published. Review that
   run's artifacts before the independent environment reviewer approves the job.
5. Publication revalidates package contents, commit, SourceLink and checksums after
   downloading this run's artifacts. It pushes Abstractions, Core and SQLite in
   dependency order; the CLI also sends each matching symbol package. A missing
   key or invalid artifact stops publication. No `--skip-duplicate` hides errors.

NuGet publication is not transactional. If a push fails, inspect all package and
symbol versions on NuGet.org before retrying; published versions are immutable.
Do not blindly re-run a partly published release. Confirm symbol indexing and
install all three packages from NuGet.org after successful publication.

For a future OIDC migration, use NuGet.org's [trusted publishing documentation](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
and bind the policy to this owner, repository, workflow and environment. Retain
the manual opt-in and protected environment; add `id-token: write` only to the
publishing job and remove the long-lived API key after verifying the migration.

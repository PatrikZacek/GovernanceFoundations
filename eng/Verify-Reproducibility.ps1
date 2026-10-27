param([string] $Version = '0.1.0')
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$original = Join-Path $root 'artifacts/packages'
$repeat = Join-Path $root ('artifacts/repro-' + [guid]::NewGuid())
# Recompile rather than reusing the first build, with the same SDK, commit and lock files.
dotnet build (Join-Path $root 'GovernanceFoundations.slnx') -c Release --no-restore -t:Rebuild "-p:Version=$Version"
if ($LASTEXITCODE) { throw 'Reproducibility rebuild failed.' }
dotnet pack (Join-Path $root 'GovernanceFoundations.slnx') -c Release --no-build "-p:Version=$Version" -o $repeat
if ($LASTEXITCODE) { throw 'Reproducibility pack failed.' }
foreach ($file in Get-ChildItem $original -File | Where-Object Extension -in '.nupkg','.snupkg') {
    if ((Get-FileHash $file.FullName).Hash -ne (Get-FileHash (Join-Path $repeat $file.Name)).Hash) {
        throw "Package is not byte-for-byte reproducible: $($file.Name)"
    }
}
Write-Host 'All six packages are byte-for-byte identical after a Release rebuild.'

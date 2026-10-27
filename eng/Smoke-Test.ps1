param(
    [string] $Version = '0.1.0',
    [string] $Feed = (Join-Path $PSScriptRoot '../artifacts/packages'),
    [string] $WorkDirectory = (Join-Path ([IO.Path]::GetTempPath()) ('governance-consumer-' + [guid]::NewGuid()))
)
$ErrorActionPreference = 'Stop'
$feedPath = (Resolve-Path $Feed).Path
New-Item -ItemType Directory -Path $WorkDirectory -ErrorAction Stop | Out-Null
$consumerPath = (Resolve-Path $WorkDirectory).Path
# An explicit standalone SDK and NuGet configuration prevent parent repository imports.
Copy-Item (Join-Path $PSScriptRoot '../tests/PackageConsumer/*') $consumerPath -Exclude bin,obj
Copy-Item (Join-Path $PSScriptRoot '../global.json') $consumerPath
$escapedFeed = [System.Security.SecurityElement]::Escape($feedPath)
@"
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$escapedFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="local"><package pattern="Governance.*" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content (Join-Path $consumerPath 'NuGet.Config')
'<Project />' | Set-Content (Join-Path $consumerPath 'Directory.Build.props')
'<Project />' | Set-Content (Join-Path $consumerPath 'Directory.Build.targets')
'<Project />' | Set-Content (Join-Path $consumerPath 'Directory.Packages.props')
$cache = Join-Path $consumerPath 'packages'
Push-Location $consumerPath
try {
    dotnet restore PackageConsumer.csproj --configfile NuGet.Config --packages $cache --no-http-cache "-p:GovernancePackageVersion=$Version"
    if ($LASTEXITCODE) { throw 'Consumer restore failed.' }
    dotnet run --project PackageConsumer.csproj -c Release --no-restore "-p:GovernancePackageVersion=$Version"
    if ($LASTEXITCODE) { throw 'Consumer execution failed.' }
    $assets = Get-Content obj/project.assets.json -Raw | ConvertFrom-Json -AsHashtable
    if (@($assets.libraries.Values | Where-Object type -eq 'project').Count) { throw 'Consumer contains a project reference.' }
    foreach ($id in @('Governance.Abstractions', 'Governance.Core', 'Governance.Persistence.Sqlite')) {
        if (!$assets.libraries.ContainsKey("$id/$Version")) { throw "Missing consumer package $id/$Version" }
        $metadata = Get-Content (Join-Path $cache "$($id.ToLowerInvariant())/$Version/.nupkg.metadata") -Raw | ConvertFrom-Json
        if ([IO.Path]::GetFullPath($metadata.source) -ne $feedPath) { throw "$id did not come from the local feed." }
    }
    Write-Host "Isolated consumer passed in $consumerPath"
} finally { Pop-Location }

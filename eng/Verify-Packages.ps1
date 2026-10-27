param(
    [string] $Version = '0.1.0',
    [string] $Feed = (Join-Path $PSScriptRoot '../artifacts/packages'),
    [string] $Commit = (git -C (Join-Path $PSScriptRoot '..') rev-parse HEAD)
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
public static class PackagePdb
{
    public static string SourceLink(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        var reader = provider.GetMetadataReader();
        foreach (var handle in reader.CustomDebugInformation)
        {
            var info = reader.GetCustomDebugInformation(handle);
            if (reader.GetGuid(info.Kind) == new Guid("cc110556-a091-4d38-9fec-25ab9a351a6a"))
                return Encoding.UTF8.GetString(reader.GetBlobBytes(info.Value));
        }
        throw new InvalidDataException("Portable PDB has no SourceLink information.");
    }
}
'@
$dependencies = [ordered]@{
    'Governance.Abstractions' = @()
    'Governance.Core' = @('Governance.Abstractions')
    'Governance.Persistence.Sqlite' = @('Governance.Core')
}
$expected = @($dependencies.Keys | ForEach-Object { "$_.${Version}.nupkg"; "$_.${Version}.snupkg" })
$actual = @(Get-ChildItem $Feed -File | Where-Object Extension -in '.nupkg','.snupkg' | ForEach-Object Name)
if (@(Compare-Object $expected $actual).Count) { throw "Expected exactly six package artifacts: $expected. Found: $actual" }
foreach ($id in $dependencies.Keys) {
    foreach ($extension in 'nupkg','snupkg') {
        $file = Join-Path $Feed "$id.$Version.$extension"
        $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path $file))
        try {
            $nuspec = @($zip.Entries | Where-Object FullName -like '*.nuspec')
            if ($nuspec.Count -ne 1) { throw "Invalid nuspec count in $file" }
            $reader = [IO.StreamReader]::new($nuspec[0].Open())
            try { [xml] $spec = $reader.ReadToEnd() } finally { $reader.Dispose() }
            $metadata = $spec.package.metadata
            if ($metadata.id -cne $id -or $metadata.version -cne $Version -or
                [string]::IsNullOrWhiteSpace($metadata.description) -or
                $metadata.repository.url -ne 'https://github.com/PatrikZacek/GovernanceFoundations' -or
                $metadata.repository.commit -ne $Commit) { throw "Invalid package metadata: $file" }
            if (@($zip.Entries | Where-Object FullName -match '(Testing|TestFixtures|UnitTests|IntegrationTests|ConfigurationDemo)\.(dll|pdb)$').Count) {
                throw "Test/demo binary leaked into $file"
            }
            if ($extension -eq 'nupkg') {
                if ($metadata.authors -ne 'Patrik Zacek' -or $metadata.readme -ne 'README.md' -or
                    !$zip.GetEntry('README.md') -or !$zip.GetEntry("lib/net10.0/$id.dll") -or
                    @($zip.Entries | Where-Object FullName -like '*.dll').Count -ne 1) {
                    throw "Missing README or target assembly: $file"
                }
                $deps = @($metadata.SelectNodes(".//*[local-name()='dependency']"))
                $internal = @($deps | Where-Object id -like 'Governance.*' | ForEach-Object id)
                if ((@($dependencies[$id] | Sort-Object) -join ',') -cne (@($internal | Sort-Object) -join ',')) {
                    throw "Invalid internal dependency graph: $id"
                }
                foreach ($dependency in $deps) {
                    if ($dependency.id -like 'Governance.*' -and $dependency.version -ne $Version -and $dependency.version -ne "[$Version, )") {
                        throw "Incorrect internal dependency version in $file"
                    }
                    if ($dependency.id -match 'SourceLink|Tasks.Git|EntityFrameworkCore.Design|xunit|Test.Sdk') { throw "Private dependency leaked: $file" }
                }
            } else {
                $pdb = $zip.GetEntry("lib/net10.0/$id.pdb")
                if (!$pdb) { throw "Missing portable PDB: $file" }
                $stream = [IO.MemoryStream]::new()
                $entryStream = $pdb.Open()
                try { $entryStream.CopyTo($stream) } finally { $entryStream.Dispose() }
                try { $sourceLink = [PackagePdb]::SourceLink($stream.ToArray()) | ConvertFrom-Json -AsHashtable }
                finally { $stream.Dispose() }
                $urls = @($sourceLink.documents.Values)
                if (!$urls.Count) { throw "Empty SourceLink map: $file" }
                foreach ($url in $urls) {
                    if (!$url.StartsWith("https://raw.githubusercontent.com/PatrikZacek/GovernanceFoundations/$Commit/")) {
                        throw "SourceLink does not point to the expected commit: $file"
                    }
                }
            }
        } finally { $zip.Dispose() }
    }
}
Write-Host "Verified all six artifacts, metadata, dependency graph, portable PDBs and SourceLink for $Version at $Commit."

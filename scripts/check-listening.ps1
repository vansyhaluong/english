# Tests the existing application build offline; optional EF metadata-only mode.
param([switch]$ReadOnlySchema, [switch]$SubtitleImport)
$ErrorActionPreference = 'Stop'
if ($ReadOnlySchema -and $SubtitleImport) { throw 'Select only one focused check mode.' }
$repoPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$appPath = Join-Path $repoPath 'bin/Debug/net10.0'
$appAssembly = Join-Path $appPath 'English.dll'
$depsPath = Join-Path $appPath 'English.deps.json'
if (!(Test-Path -LiteralPath $appAssembly) -or !(Test-Path -LiteralPath $depsPath)) {
    throw 'A successful Debug/net10.0 application build is required before these checks.'
}
$builtAt = (Get-Item -LiteralPath $appAssembly).LastWriteTimeUtc
$sourceFiles = @(Get-Item (Join-Path $repoPath 'EnglishHub.csproj'), (Join-Path $repoPath 'Program.cs'), (Join-Path $repoPath 'Data/ApplicationDbContext.cs'))
foreach ($directory in @('Controllers', 'Services', 'Interfaces', 'Models', 'Authorization', 'Views', 'Resources')) {
    $sourceFiles += Get-ChildItem -LiteralPath (Join-Path $repoPath $directory) -Recurse -File |
        Where-Object { $_.Extension -in @('.cs', '.cshtml', '.resx') }
}
if ($sourceFiles | Where-Object { $_.LastWriteTimeUtc -gt $builtAt }) {
    throw 'The application build is older than its source files. Build the current application before running these checks.'
}
$deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json
$target = $deps.targets.PSObject.Properties | Select-Object -First 1
foreach ($library in $target.Value.PSObject.Properties) {
    foreach ($runtime in $library.Value.runtime.PSObject.Properties) {
        $runtimeFile = Join-Path $appPath ([System.IO.Path]::GetFileName($runtime.Name))
        if (!(Test-Path -LiteralPath $runtimeFile)) { throw "Missing existing build dependency: $runtimeFile" }
    }
}
$references = foreach ($assembly in Get-ChildItem -LiteralPath $appPath -Filter '*.dll' -File) {
    $nameXml = [System.Security.SecurityElement]::Escape($assembly.BaseName)
    $pathXml = [System.Security.SecurityElement]::Escape($assembly.FullName)
    "<Reference Include=`"$nameXml`"><HintPath>$pathXml</HintPath><Private>true</Private></Reference>"
}
$probePath = Join-Path ([System.IO.Path]::GetTempPath()) ('english-listening-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probePath | Out-Null
$projectPath = Join-Path $probePath 'Checks.csproj'
$repoXml = [System.Security.SecurityElement]::Escape($repoPath)
$projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    $($references -join "`n")
    <Compile Include="$repoXml/scripts/check-listening.csx" Link="Program.cs" />
  </ItemGroup>
</Project>
"@
[System.IO.File]::WriteAllText($projectPath, $projectXml)
$probeArgs = @($repoPath)
if ($ReadOnlySchema) { $probeArgs += '--read-only-schema' }
if ($SubtitleImport) { $probeArgs += '--subtitle-import' }
# This package-free probe has no ProjectReference: it cannot restore or rebuild the application.
# Keep the existing empty local restore override so no network package source is consulted.
dotnet restore $projectPath --property:RestoreSources=$probePath --property:NuGetAudit=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet run --no-restore --project $projectPath -- @probeArgs
exit $LASTEXITCODE

param([switch]$Database, [string]$BaseUrl)
$ErrorActionPreference = 'Stop'
$repoPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$probePath = Join-Path ([System.IO.Path]::GetTempPath()) ('english-grammar-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probePath | Out-Null
$projectPath = Join-Path $probePath 'Checks.csproj'
$repoXml = [System.Security.SecurityElement]::Escape($repoPath)
$outputXml = [System.Security.SecurityElement]::Escape((Join-Path $probePath 'app'))
$projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$repoXml/EnglishHub.csproj" AdditionalProperties="OutputPath=$outputXml/;UseAppHost=false" />
    <Compile Include="$repoXml/scripts/check-grammar.csx" Link="Program.cs" />
  </ItemGroup>
</Project>
"@
[System.IO.File]::WriteAllText($projectPath, $projectXml)
$probeArgs = @($repoPath)
if ($Database) { $probeArgs += '--database' }
if ($BaseUrl) { $probeArgs += @('--base-url', $BaseUrl) }
dotnet run --project $projectPath -- @probeArgs
exit $LASTEXITCODE

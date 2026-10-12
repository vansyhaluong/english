param([Parameter(Mandatory = $true)][string]$BaseUrl)
$ErrorActionPreference = 'Stop'
if (-not $env:ConnectionStrings__DefaultConnection) { throw 'Set the temporary test DB environment override first.' }
$repoPath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$probePath = Join-Path ([System.IO.Path]::GetTempPath()) ('english-p2-check-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $probePath | Out-Null
$repoXml = [System.Security.SecurityElement]::Escape($repoPath)
$outputXml = [System.Security.SecurityElement]::Escape((Join-Path $probePath 'app'))
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$repoXml/EnglishHub.csproj" AdditionalProperties="OutputPath=$outputXml/;UseAppHost=false" />
    <Compile Include="$repoXml/scripts/check-p2.csx" Link="Program.cs" />
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $probePath 'Checks.csproj'
[System.IO.File]::WriteAllText($projectPath, $project)
dotnet run --project $projectPath -- $repoPath $BaseUrl
exit $LASTEXITCODE

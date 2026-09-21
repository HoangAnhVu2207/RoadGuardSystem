[CmdletBinding()]
param(
    [ValidateSet("Unit", "Sql", "Api", "All")]
    [string]$Category = "All",
    [switch]$Coverage,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$root = Split-Path -Parent $root
Set-Location $root

$projects = switch ($Category) {
    "Unit" { @("tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj") }
    "Sql" { @("tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj") }
    "Api" { @("tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj") }
    default {
        @(
            "tests/RoadGuardSystem.UnitTests/RoadGuardSystem.UnitTests.csproj",
            "tests/RoadGuardSystem.IntegrationTests/RoadGuardSystem.IntegrationTests.csproj",
            "tests/RoadGuardSystem.ApiTests/RoadGuardSystem.ApiTests.csproj"
        )
    }
}

if (-not $NoBuild) {
    dotnet build RoadGuardSystem.slnx -nologo -v q -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

foreach ($project in $projects) {
    $arguments = @("test", $project, "--no-build", "--nologo", "-v", "q", "--filter", "Category=$Category")
    if ($Category -eq "All") {
        $arguments = @("test", $project, "--no-build", "--nologo", "-v", "q")
    }
    if ($Coverage) {
        $arguments += '--collect:XPlat Code Coverage'
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

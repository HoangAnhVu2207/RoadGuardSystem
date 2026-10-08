param(
    [string]$ManifestPath = (Join-Path $PSScriptRoot 'sql-lanes.json'),
    [string]$AssemblyPath = (Join-Path $PSScriptRoot '../RoadGuardSystem.IntegrationTests/bin/Debug/net8.0/RoadGuardSystem.IntegrationTests.dll')
)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$expected = @($manifest.lanes | ForEach-Object { $_.classes })
if ($expected.Count -eq 0 -or @($expected | Sort-Object -Unique).Count -ne $expected.Count) {
    throw 'SQL lane class manifest is empty or contains duplicate classes.'
}

$assemblyFile = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assemblyDir = Split-Path -Parent $assemblyFile
$resolver = [ResolveEventHandler] {
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name
    $dependency = Join-Path $assemblyDir "$name.dll"
    if (Test-Path -LiteralPath $dependency) {
        return [Reflection.Assembly]::LoadFrom($dependency)
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    $assembly = [Reflection.Assembly]::LoadFrom($assemblyFile)
    $discovered = @($assembly.GetTypes() | Where-Object {
        if (-not $_.IsClass) { return $false }
        foreach ($method in $_.GetMethods()) {
            foreach ($attribute in $method.GetCustomAttributesData()) {
                $type = $attribute.AttributeType
                while ($null -ne $type) {
                    if ($type.FullName -eq 'Xunit.FactAttribute') { return $true }
                    $type = $type.BaseType
                }
            }
        }
        return $false
    } | ForEach-Object { $_.FullName })
}
finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}

$delta = @(Compare-Object $expected $discovered)
if ($delta.Count -ne 0) {
    $missing = @($delta | Where-Object SideIndicator -eq '=>').Count
    $stale = @($delta | Where-Object SideIndicator -eq '<=').Count
    throw "SQL lane manifest differs from compiled xUnit discovery: $missing unassigned class(es), $stale stale class(es)."
}
Write-Host "SQL lane discovery gate passed: $($discovered.Count) compiled xUnit classes assigned exactly once."

param(
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$AssemblyPath
)

$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$classes = @($manifest.lanes | ForEach-Object { $_.classes })
$methods = @($manifest.lanes | ForEach-Object { $_.methods } | Where-Object { $null -ne $_ })
$splitClasses = @($methods | ForEach-Object { $_.Substring(0, $_.LastIndexOf('.')) } | Sort-Object -Unique -CaseSensitive)
if ($manifest.localRealAcceptanceClass) { $classes += $manifest.localRealAcceptanceClass }
$expected = @($classes) + @($methods)
if ($expected.Count -eq 0 -or @($expected | Sort-Object -Unique -CaseSensitive).Count -ne $expected.Count -or
    @($classes | Where-Object { $_ -cin $splitClasses }).Count -ne 0) {
    throw 'Lane discovery manifest has no assignments, duplicate assignments, or overlapping class and method assignments.'
}

$assemblyFile = (Resolve-Path -LiteralPath $AssemblyPath).Path
$assemblyDir = Split-Path -Parent $assemblyFile
$frameworkDirectories = @(dotnet --list-runtimes | ForEach-Object {
    if ($_ -match '^Microsoft\.AspNetCore\.App (8\.\d+\.\d+) \[(.+)\]$') {
        [pscustomobject]@{ Version = [version]$Matches[1]; Path = Join-Path $Matches[2] $Matches[1] }
    }
} | Sort-Object Version -Descending | Select-Object -ExpandProperty Path -First 1)
$resolver = [ResolveEventHandler] {
    param($sender, $eventArgs)
    $name = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name
    foreach ($directory in @($assemblyDir) + $frameworkDirectories) {
        $dependency = Join-Path $directory "$name.dll"
        if (Test-Path -LiteralPath $dependency) {
            return [Reflection.Assembly]::LoadFrom($dependency)
        }
    }
    return $null
}

function Test-XunitMethod([Reflection.MethodInfo]$Method) {
    foreach ($attribute in $Method.GetCustomAttributesData()) {
        $type = $attribute.AttributeType
        while ($null -ne $type) {
            if ($type.FullName -eq 'Xunit.FactAttribute') { return $true }
            $type = $type.BaseType
        }
    }
    return $false
}

[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    $assembly = [Reflection.Assembly]::LoadFrom($assemblyFile)
    $discovered = @(foreach ($class in $assembly.GetTypes()) {
        if (-not $class.IsClass -or $class.IsAbstract) { continue }
        $testMethods = @($class.GetMethods() | Where-Object { Test-XunitMethod $_ })
        if ($testMethods.Count -eq 0) { continue }
        if ($class.FullName -cin $splitClasses) {
            foreach ($method in $testMethods) { "$($class.FullName).$($method.Name)" }
        } else {
            $class.FullName
        }
    })
} finally {
    [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver)
}

$delta = @(Compare-Object $expected $discovered -CaseSensitive)
if ($delta.Count -ne 0) {
    throw "Lane assignments differ from compiled xUnit discovery: $($delta | Out-String)"
}
Write-Host "Lane discovery passed for $([IO.Path]::GetFileName($AssemblyPath)): $($classes.Count) classes and $($methods.Count) separately assigned methods."

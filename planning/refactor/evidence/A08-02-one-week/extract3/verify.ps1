param([Parameter(Mandatory=$true)][string]$Zip,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
if(Test-Path $Destination){Remove-Item -Recurse -Force $Destination}
Expand-Archive -LiteralPath $Zip -DestinationPath $Destination
$m=Get-Content -Raw (Join-Path $Destination 'MANIFEST.json') | ConvertFrom-Json
$actual=@{}
Get-ChildItem $Destination -Recurse -File | ForEach-Object {
  $path=$_.FullName.Substring($Destination.Length+1).Replace('\','/')
  $actual[$path]=[ordered]@{size=$_.Length;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}
}
$missing=@($m.entries.path | Where-Object { -not $actual.ContainsKey($_) })
$mismatch=@($m.entries | Where-Object { $actual.ContainsKey($_.path) -and ($actual[$_.path].size -ne [int64]$_.size -or $actual[$_.path].sha256 -ne $_.sha256) } | Select-Object -ExpandProperty path)
$unlisted=@($actual.Keys | Where-Object { $_ -ne 'MANIFEST.json' -and -not ($m.entries.path -contains $_) })
$duplicates=@($m.entries.path | Group-Object | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name)
[ordered]@{missing=$missing;mismatch=$mismatch;unlisted=$unlisted;duplicates=$duplicates}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $Destination 'verification.json')
if($missing.Count -or $mismatch.Count -or $unlisted.Count -or $duplicates.Count){throw 'verification failed'}

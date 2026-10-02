param([Parameter(Mandatory=$true)][string]$Zip,[Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
if(Test-Path $Destination){Remove-Item -Recurse -Force $Destination}
New-Item -ItemType Directory -Force $Destination | Out-Null
Expand-Archive -LiteralPath $Zip -DestinationPath $Destination
$manifest=Get-Content -Raw (Join-Path $Destination 'MANIFEST.json') | ConvertFrom-Json
$actual=@{}
Get-ChildItem $Destination -Recurse -File | ForEach-Object { $rel=$_.FullName.Substring($Destination.Length+1).Replace('\','/'); $actual[$rel]=[ordered]@{size=$_.Length;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()} }
$missing=@($manifest.entries.path | Where-Object { -not $actual.ContainsKey($_) })
$mismatch=@($manifest.entries | Where-Object { $actual.ContainsKey($_.path) -and ($actual[$_.path].size -ne [int64]$_.size -or $actual[$_.path].sha256 -ne $_.sha256) } | Select-Object -ExpandProperty path)
$unlisted=@($actual.Keys | Where-Object { $_ -ne 'MANIFEST.json' -and -not ($manifest.entries.path -contains $_) })
$duplicates=@($manifest.entries.path | Group-Object | Where-Object Count -gt 1 | Select-Object -ExpandProperty Name)
[ordered]@{missing=$missing;mismatch=$mismatch;unlisted=$unlisted;duplicates=$duplicates;payloadCount=$manifest.entries.Count;extractedCount=$actual.Count}|ConvertTo-Json -Depth 6 | Tee-Object (Join-Path $Destination 'verification.json')
if($missing.Count -or $mismatch.Count -or $unlisted.Count -or $duplicates.Count){throw 'Archive verification failed.'}

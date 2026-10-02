$ErrorActionPreference='Stop'
$root=(Split-Path -Parent $MyInvocation.MyCommand.Path); $payload=Join-Path $root 'payload'; $zip=Join-Path $root 'RF-10-integrated-remediation-handoff.zip'; $stage=Join-Path $root '.stage';
if(Test-Path $stage){Remove-Item -Recurse -Force $stage}; New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item -Recurse -Force (Join-Path $payload '*') $stage
$entries=@(Get-ChildItem $stage -Recurse -File | ForEach-Object {[ordered]@{path=$_.FullName.Substring($stage.Length+1).Replace('\','/');size=$_.Length;sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}} | Sort-Object path)
[ordered]@{format='RF-10-integrated-remediation/v1';generatedAtUtc=[DateTime]::UtcNow.ToString('o');manifestSelfExcluded=$true;entries=$entries}|ConvertTo-Json -Depth 8|Set-Content -Encoding utf8 (Join-Path $stage 'MANIFEST.json')
if(Test-Path $zip){Remove-Item -Force $zip}; Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -CompressionLevel Optimal
Get-FileHash $zip -Algorithm SHA256 | ForEach-Object { "$($_.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($zip))" } | Set-Content -Encoding ascii "$zip.sha256"
$extract=Join-Path $root 'extract-verification'; & (Join-Path $root 'verify-extract.ps1') -Zip $zip -Destination $extract | Set-Content -Encoding utf8 (Join-Path $root 'archive-verification.json')
Remove-Item -Recurse -Force $stage

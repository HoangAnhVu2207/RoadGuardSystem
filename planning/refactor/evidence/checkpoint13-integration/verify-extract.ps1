# RF-10-checkpoint-13-three-box-handoff.zip Extract Verification Script
# Usage: .\verify-extract.ps1 -ExtractPath <path>
# Returns: Exit code 0 if valid, 1 if integrity check fails

param(
    [Parameter(Mandatory=$true)]
    [string]$ExtractPath
)

Write-Host "=== RF-10 Checkpoint 13 Extract Verification ===" -ForegroundColor Cyan
Write-Host "Extract path: $ExtractPath"
Write-Host ""

# Check if manifest exists
$manifestPath = Join-Path $ExtractPath "manifest.json"
if (-not (Test-Path $manifestPath)) {
    Write-Host "[FAIL] manifest.json not found" -ForegroundColor Red
    exit 1
}

# Load manifest
$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
Write-Host "[INFO] Package: $($manifest.package)" -ForegroundColor Yellow
Write-Host "[INFO] Version: $($manifest.version)" -ForegroundColor Yellow
Write-Host "[INFO] Created: $($manifest.created)" -ForegroundColor Yellow
Write-Host "[INFO] Branch: $($manifest.branch)" -ForegroundColor Yellow
Write-Host "[INFO] Commit: $($manifest.commit)" -ForegroundColor Yellow
Write-Host "[INFO] Expected artifacts: $($manifest.total_artifacts)" -ForegroundColor Yellow
Write-Host ""

# Verify each file
$missing = 0
$mismatch = 0
$verified = 0

foreach ($file in $manifest.files) {
    $filePath = Join-Path $ExtractPath $file.path

    if (-not (Test-Path $filePath)) {
        Write-Host "[MISSING] $($file.path)" -ForegroundColor Red
        $missing++
        continue
    }

    # Calculate SHA-256
    $hash = (Get-FileHash -Path $filePath -Algorithm SHA256).Hash.ToLower()

    if ($hash -ne $file.sha256) {
        Write-Host "[MISMATCH] $($file.path)" -ForegroundColor Red
        Write-Host "  Expected: $($file.sha256)" -ForegroundColor Gray
        Write-Host "  Actual:   $hash" -ForegroundColor Gray
        $mismatch++
    } else {
        $verified++
    }
}

# Check for unlisted files
$extractedFiles = Get-ChildItem -Path $ExtractPath -File | Where-Object { $_.Name -ne "manifest.json" -and $_.Name -ne "verify-extract.ps1" }
$manifestPaths = $manifest.files | ForEach-Object { $_.path }
$unlisted = $extractedFiles | Where-Object { $manifestPaths -notcontains $_.Name }

if ($unlisted.Count -gt 0) {
    Write-Host ""
    Write-Host "[WARN] Unlisted files found:" -ForegroundColor Yellow
    $unlisted | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
}

# Summary
Write-Host ""
Write-Host "=== Verification Summary ===" -ForegroundColor Cyan
Write-Host "Verified: $verified" -ForegroundColor Green
Write-Host "Missing:  $missing" -ForegroundColor $(if ($missing -gt 0) { "Red" } else { "Green" })
Write-Host "Mismatch: $mismatch" -ForegroundColor $(if ($mismatch -gt 0) { "Red" } else { "Green" })
Write-Host "Unlisted: $($unlisted.Count)" -ForegroundColor $(if ($unlisted.Count -gt 0) { "Yellow" } else { "Green" })
Write-Host ""

if ($missing -eq 0 -and $mismatch -eq 0) {
    Write-Host "[PASS] Extract integrity verified" -ForegroundColor Green
    exit 0
} else {
    Write-Host "[FAIL] Extract integrity check failed" -ForegroundColor Red
    exit 1
}

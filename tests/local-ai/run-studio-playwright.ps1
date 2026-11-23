param([switch]$Headless)
$ErrorActionPreference='Stop'
$env:PLAYWRIGHT_BROWSERS_PATH='D:/RoadGuard-AI-LocalTest/playwright-browsers'
$env:TEMP='D:/RoadGuard-AI-LocalTest/tmp'
$env:TMP=$env:TEMP
$env:PYTHONIOENCODING='utf-8'
$testArgs=@((Join-Path $PSScriptRoot 'test-studio-playwright.py'))
if($Headless){$testArgs+='--headless'}
& 'D:/RoadGuard-AI-LocalTest/venv/Scripts/python.exe' @testArgs
if($LASTEXITCODE -ne 0){throw 'Playwright acceptance failed; inspect its report.json and failure screenshot on D:.'}

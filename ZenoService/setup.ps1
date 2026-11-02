param([string]$Python = 'python')
$ErrorActionPreference = 'Stop'
$serviceRoot = $PSScriptRoot
$repoRoot = Split-Path $serviceRoot -Parent
$venvPython = Join-Path $serviceRoot '.venv/Scripts/python.exe'
if (-not (Test-Path -LiteralPath $venvPython)) {
    & $Python -m venv (Join-Path $serviceRoot '.venv')
    if ($LASTEXITCODE -ne 0) { throw 'Could not create the Python environment. Install Python 3.12 or later.' }
}
& $venvPython -m pip install -r (Join-Path $serviceRoot 'requirements.txt') --disable-pip-version-check
if ($LASTEXITCODE -ne 0) { throw 'Could not install wake recognition dependencies.' }
& $venvPython (Join-Path $serviceRoot 'install_whisper.py')
if ($LASTEXITCODE -ne 0) { throw 'Whisper download failed. Check your connection and run setup.ps1 again.' }
$localRoot = Join-Path $serviceRoot '.local'
New-Item -ItemType Directory -Path $localRoot -Force | Out-Null
$settingsPath = Join-Path $localRoot 'settings.json'
if (Test-Path -LiteralPath $settingsPath) {
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
} else {
    $bytes = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes); $rng.Dispose()
    $settings = [ordered]@{
        client_token = ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
        gemini_api_key = ''
        model = 'gemini-3.1-flash-lite'
    }
    $settings | ConvertTo-Json | Set-Content -LiteralPath $settingsPath -Encoding UTF8
}
$clientPath = Join-Path $repoRoot 'My project/zeno.local.json'
if (-not (Test-Path -LiteralPath $clientPath)) {
    [ordered]@{ endpoint='http://127.0.0.1:8765'; client_token=$settings.client_token; microphone=''; microphone_threshold=0.012 } |
        ConvertTo-Json | Set-Content -LiteralPath $clientPath -Encoding UTF8
}
Write-Host 'Zeno installed with local Whisper. Run configure.ps1 to enter your Gemini key privately, then start.ps1.'

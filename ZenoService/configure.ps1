$ErrorActionPreference = 'Stop'
$settingsPath = Join-Path $PSScriptRoot '.local/settings.json'
if (-not (Test-Path -LiteralPath $settingsPath)) { throw 'Run setup.ps1 first.' }
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$secret = Read-Host 'Paste your Gemini API key (hidden, saved only in the ignored local settings)' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
    $keyValue = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer).Trim()
    if (-not $keyValue) { throw 'Key was empty; settings were not changed.' }
    $settings.gemini_api_key = $keyValue
    $settings | ConvertTo-Json | Set-Content -LiteralPath $settingsPath -Encoding UTF8
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    $keyValue = $null
}
Write-Host 'Key saved locally. Restart start.ps1, then turn on Zeno inside Unity.'

param([string]$Voice = 'Microsoft Zira Desktop')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$voiceRoot = Join-Path $PSScriptRoot '../My project/Assets/Ferrari/Resources/FerrariNarration'
$voiceCatalog = Get-Content -Raw -LiteralPath (Join-Path $voiceRoot 'catalog.json') | ConvertFrom-Json
$narrator = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    $narrator.SelectVoice($Voice)
    $narrator.Rate = -1
    foreach ($entry in $voiceCatalog.entries) {
        foreach ($kind in @('explanation', 'hint')) {
            $audioPath = [System.IO.Path]::GetFullPath((Join-Path $voiceRoot ($entry.id + '_' + $kind + '.wav')))
            $narrator.SetOutputToWaveFile($audioPath)
            $narrator.Speak([string]$entry.$kind)
            $narrator.SetOutputToNull()
        }
    }
    Write-Output ('Generated {0} offline clips with {1}.' -f ($voiceCatalog.entries.Count * 2), $Voice)
} finally { $narrator.Dispose() }

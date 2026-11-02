param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Speech
$text = [Console]::In.ReadToEnd()
$voice = New-Object System.Speech.Synthesis.SpeechSynthesizer
try {
    # Enumerating voices can fail in a background PowerShell process on Windows.
    # Select an installed male voice directly, using the classic SAPI voice first.
    foreach ($name in @('Microsoft David Desktop', 'Microsoft David', 'Microsoft Mark', 'Microsoft Ravi')) {
        try { $voice.SelectVoice($name); break }
        catch [System.ArgumentException] { continue }
    }
    $voice.Rate = -1
    $voice.SetOutputToWaveFile($OutputPath)
    $voice.Speak($text)
} finally { $voice.Dispose() }

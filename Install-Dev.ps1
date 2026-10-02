param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Output = Join-Path $Root "bin\$Configuration\net462"
$Target = Join-Path $env:APPDATA "Playnite\Extensions\GameAudioSwitcher_6a113baf-8802-4dc5-9a5b-8cfc3519db83"

if (-not (Test-Path (Join-Path $Output "GameAudioSwitcher.dll"))) {
    throw "Build the plugin first. Expected: $Output\GameAudioSwitcher.dll"
}

if (Test-Path $Target) {
    Remove-Item $Target -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $Target | Out-Null
Copy-Item (Join-Path $Output "*") $Target -Recurse -Force

Write-Host "Installed development build to:"
Write-Host $Target
Write-Host "Restart Playnite to load the plugin."

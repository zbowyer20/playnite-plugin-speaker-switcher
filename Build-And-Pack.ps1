param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$ToolboxPath = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "GameAudioSwitcher.csproj"
$Output = Join-Path $Root "bin\$Configuration\net462"
$Dist = Join-Path $Root "dist"

Write-Host "Restoring packages..."
dotnet restore $Project

Write-Host "Building $Configuration..."
dotnet build $Project -c $Configuration --no-restore

if (-not (Test-Path $Output)) {
    throw "Expected build output not found: $Output"
}

New-Item -ItemType Directory -Force -Path $Dist | Out-Null

if ([string]::IsNullOrWhiteSpace($ToolboxPath)) {
    $Candidates = @(
        (Join-Path $env:LOCALAPPDATA "Playnite\Toolbox.exe"),
        (Join-Path $env:ProgramFiles "Playnite\Toolbox.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Playnite\Toolbox.exe")
    ) | Where-Object { $_ -and (Test-Path $_) }

    if ($Candidates.Count -gt 0) {
        $ToolboxPath = $Candidates[0]
    }
}

if (-not [string]::IsNullOrWhiteSpace($ToolboxPath) -and (Test-Path $ToolboxPath)) {
    Write-Host "Packing .pext with Playnite Toolbox..."
    & $ToolboxPath pack $Output $Dist
    Write-Host "Package written to $Dist"
} else {
    Write-Warning "Playnite Toolbox.exe was not found. Build succeeded, but no .pext was produced."
    Write-Host "You can pack manually with: Toolbox.exe pack `"$Output`" `"$Dist`""
}

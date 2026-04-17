# Publishes PadLink.DesktopApp (WPF) for Windows x64.
# Run from anywhere:  pwsh -File windows\scripts\Publish-DesktopApp.ps1
#
# Default: framework-dependent (smaller; requires .NET 8 Desktop Runtime on the PC).
# -SelfContained: single-file self-contained exe (no separate runtime install).

param(
    [switch]$SelfContained
)

$ErrorActionPreference = 'Stop'

$dotnetExe = $null
$cmd = Get-Command dotnet -ErrorAction SilentlyContinue
if ($cmd) {
    $dotnetExe = $cmd.Source
}
if (-not $dotnetExe) {
    $candidate = Join-Path $env:ProgramFiles 'dotnet\dotnet.exe'
    if (Test-Path $candidate) {
        $dotnetExe = $candidate
    }
}
if (-not $dotnetExe) {
    throw 'dotnet not found. Install .NET 8 SDK and ensure dotnet is on PATH, or install to Program Files\dotnet.'
}

$windowsRoot = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $windowsRoot 'desktop-app\PadLink.DesktopApp.csproj'

if ($SelfContained) {
    $out = Join-Path $windowsRoot 'desktop-app\publish\PadLink-Windows-x64-SelfContained'
    & $dotnetExe publish $proj -c Release -r win-x64 --self-contained true -o $out `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:EnableCompressionInSingleFile=true
}
else {
    $out = Join-Path $windowsRoot 'desktop-app\publish\PadLink-Windows-x64'
    & $dotnetExe publish $proj -c Release -r win-x64 --self-contained false -o $out
}

Write-Host ""
Write-Host "Published to: $out" -ForegroundColor Green
Write-Host "Run:          $(Join-Path $out 'PadLink.DesktopApp.exe')" -ForegroundColor Green

param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePng
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root "Assets"
$targets = @(
    (Join-Path $assets "AppIcon.ico"),
    (Join-Path $root "NovaBrowser.Installer\Assets\AppIcon.ico"),
    (Join-Path $root "NovaBrowser.Uninstaller\Assets\AppIcon.ico")
)

if (-not (Test-Path $SourcePng)) {
    throw "Source PNG not found: $SourcePng"
}

Add-Type -AssemblyName System.Drawing

Copy-Item $SourcePng (Join-Path $assets "AppIcon.png") -Force

$bitmap = New-Object System.Drawing.Bitmap $SourcePng
$scaled = New-Object System.Drawing.Bitmap 256, 256
$graphics = [System.Drawing.Graphics]::FromImage($scaled)
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.DrawImage($bitmap, 0, 0, 256, 256)
$graphics.Dispose()

$hIcon = $scaled.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($hIcon)
$tempIco = Join-Path $assets "AppIcon.ico"

$stream = [System.IO.File]::Create($tempIco)
$icon.Save($stream)
$stream.Close()

$icon.Dispose()
$scaled.Dispose()
$bitmap.Dispose()

foreach ($target in $targets) {
    New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
    Copy-Item $tempIco $target -Force
}

Write-Host "App icon synced to browser, installer, and uninstaller."

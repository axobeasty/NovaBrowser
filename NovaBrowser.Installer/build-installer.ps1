param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$rid = switch ($Platform) {
    "ARM64" { "win-arm64" }
    "x64" { "win-x64" }
    "x86" { "win-x86" }
    default { throw "Unsupported platform: $Platform" }
}
$platformFolder = if ($Platform -eq "ARM64") { "arm64" } else { $Platform.ToLowerInvariant() }

$bundleStaging = Join-Path $env:TEMP "NovaBrowser-Bundle-$Platform"
$uninstallStaging = Join-Path $env:TEMP "NovaBrowser-Uninstall-$Platform"
$bundleZip = Join-Path $PSScriptRoot "Assets\SetupBundle.zip"
$distDir = Join-Path $root "dist\installer-$platformFolder"
$setupExe = Join-Path $distDir "NovaBrowser.Setup.exe"

if (Test-Path $bundleStaging) { Remove-Item $bundleStaging -Recurse -Force }
if (Test-Path $uninstallStaging) { Remove-Item $uninstallStaging -Recurse -Force }
New-Item -ItemType Directory -Path $bundleStaging -Force | Out-Null

Write-Host "==> Publishing NovaBrowser ($Platform)..."
dotnet publish (Join-Path $root "NovaBrowser.csproj") -c Release -p:Platform=$Platform -r $rid -o $bundleStaging
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Publishing NovaBrowser.Uninstall ($Platform)..."
dotnet publish (Join-Path $root "NovaBrowser.Uninstaller\NovaBrowser.Uninstaller.csproj") -c Release -p:Platform=$Platform -r $rid -o $uninstallStaging
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Merging uninstaller into bundle..."
Copy-Item -Path (Join-Path $uninstallStaging "*") -Destination $bundleStaging -Recurse -Force

$requiredBundleFiles = @(
    "NovaBrowser.exe",
    "NovaBrowser.Uninstall.exe",
    "NovaBrowser.Setup.Common.dll"
)
foreach ($name in $requiredBundleFiles) {
    if (-not (Test-Path (Join-Path $bundleStaging $name))) {
        throw "Bundle is missing required file: $name"
    }
}

Write-Host "==> Creating embedded SetupBundle.zip..."
if (Test-Path $bundleZip) { Remove-Item $bundleZip -Force }
New-Item -ItemType Directory -Path (Split-Path $bundleZip) -Force | Out-Null
Copy-Item (Join-Path $root "Assets\AppIcon.ico") (Join-Path $PSScriptRoot "Assets\AppIcon.ico") -Force
Copy-Item (Join-Path $root "Assets\AppIcon.ico") (Join-Path $root "NovaBrowser.Uninstaller\Assets\AppIcon.ico") -Force
Copy-Item (Join-Path $root "Assets\AppIcon.png") (Join-Path $PSScriptRoot "Assets\AppIcon.png") -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $bundleStaging "*") -DestinationPath $bundleZip -Force

Write-Host "==> Publishing single-file NovaBrowser.Setup ($Platform)..."
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
New-Item -ItemType Directory -Path $distDir -Force | Out-Null

dotnet publish (Join-Path $PSScriptRoot "NovaBrowser.Installer.csproj") -c Release -p:Platform=$Platform -r $rid -o $distDir -p:DebugType=none -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem $distDir -Exclude "NovaBrowser.Setup.exe" | Remove-Item -Recurse -Force
if (-not (Test-Path $setupExe)) {
    throw "NovaBrowser.Setup.exe was not produced."
}

$releaseZip = Join-Path $root "dist\NovaBrowser.Setup-$rid.zip"
if (Test-Path $releaseZip) { Remove-Item $releaseZip -Force }
Compress-Archive -Path $setupExe -DestinationPath $releaseZip -Force

Write-Host ""
Write-Host "Done."
Write-Host "Installer: $setupExe"
Write-Host "Bundle:    $bundleZip"
Write-Host "Zip:       $releaseZip"

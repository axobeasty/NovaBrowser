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
$payloadDir = Join-Path $PSScriptRoot "payload"
$distDir = Join-Path $root "dist\installer-$platformFolder"

Write-Host "==> Publishing NovaBrowser ($Platform)..."
dotnet publish (Join-Path $root "NovaBrowser.csproj") `
    -c Release `
    -p:Platform=$Platform `
    -r $rid `
    -o $payloadDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "==> Publishing NovaBrowser.Setup ($Platform)..."
dotnet publish (Join-Path $PSScriptRoot "NovaBrowser.Installer.csproj") `
    -c Release `
    -p:Platform=$Platform `
    -r $rid `
    -o $distDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$distPayloadDir = Join-Path $distDir "payload"
if (Test-Path $distPayloadDir) {
    Remove-Item $distPayloadDir -Recurse -Force
}
Copy-Item -Path $payloadDir -Destination $distPayloadDir -Recurse -Force

$zipPath = Join-Path $root "dist\NovaBrowser.Setup-$rid.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $distDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Done."
Write-Host "Installer: $distDir\NovaBrowser.Setup.exe"
Write-Host "Payload:   $distPayloadDir"
Write-Host "Zip:       $zipPath"

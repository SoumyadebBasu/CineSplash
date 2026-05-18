# pack.ps1 - Build, copy assets, pack and rename .pext

$playnite = "E:\Playnite"
$msbuild  = "C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"
$buildOut = "$PSScriptRoot\bin\Release"
$packed   = "$PSScriptRoot\packed"

# Read name and version from extension.yaml
$yamlContent = Get-Content "$PSScriptRoot\extension.yaml" -Raw

$name = "CineSplash"
$version = "1.0.0"

if ($yamlContent -match '(?m)^Name:\s*(.+)$') {
    $name = $Matches[1].Trim() -replace ' ', ''
}

if ($yamlContent -match '(?m)^Version:\s*(.+)$') {
    $version = $Matches[1].Trim()
}

# Build
Write-Host "Building $name $version..." -ForegroundColor Cyan
& $msbuild "$PSScriptRoot\CineSplash.csproj" /p:Configuration=Release /v:minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed - aborting." -ForegroundColor Red
    exit 1
}

# Copy assets into build output
Copy-Item "$PSScriptRoot\extension.yaml" $buildOut -Force
Copy-Item "$PSScriptRoot\plugin.yaml"    $buildOut -Force
Copy-Item "$PSScriptRoot\icon.png"       $buildOut -Force
Copy-Item "$PSScriptRoot\Localization"   $buildOut -Recurse -Force

# Pack
New-Item -ItemType Directory -Force -Path $packed | Out-Null
& "$playnite\Toolbox.exe" pack $buildOut $packed

# Rename the generated .pext to Name_Version.pext
$generated = Get-ChildItem "$packed\*.pext" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($generated) {
    $target = Join-Path $packed ($name + "_" + $version + ".pext")
    if (Test-Path $target) { Remove-Item $target -Force }
    Rename-Item $generated.FullName $target
    Write-Host "Done! Packed to: $target" -ForegroundColor Green
} else {
    Write-Host "Pack failed - no .pext file found in $packed" -ForegroundColor Red
}
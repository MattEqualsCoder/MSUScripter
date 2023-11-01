$parentFolder = Split-Path -parent $PSScriptRoot

# Get publish folder
$folder = "$parentFolder/MSUScripter/bin/Release/net7.0/linux-x64/publish"
$winFolder = "$parentFolder/MSUScripter/bin/Release/net7.0-windows/win-x86/publish"
if (-not (Test-Path $folder))
{
    $folder = "$parentFolder/MSUScripter/bin/Release/net7.0/publish/linux-x64"
    $winFolder = "$parentFolder/MSUScripter/bin/Release/net7.0-windows/publish/win-x86"
}

# Get version number from MSUScripter
$version = "0.0.0"
if (Test-Path "$winFolder/MSUScripter.exe") {
    $version = (Get-Item "$winFolder/MSUScripter.exe").VersionInfo.ProductVersion
}
else {
    $version = (Get-Item "$folder/MSUScripter.dll").VersionInfo.ProductVersion
}
Write-Host "Version: $version"

# Create package
$fullVersion = "MSUScripterLinux_$version"
$outputFolder = "$PSScriptRoot/Output"
New-Item -Path "$outputFolder" -Force -ItemType Directory -ErrorAction SilentlyContinue
$outputFile = "$outputFolder/$fullVersion.tar.gz"
Write-Host "Creating $outputFile"
if (Test-Path $outputFile) {
    Remove-Item $outputFile -Force
}
if (-not (Test-Path $outputFile)) {
    Set-Location $folder
    tar -cvzf $outputFile *
}
Set-Location $PSScriptRoot
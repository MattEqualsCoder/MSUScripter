# To run, make sure 7zip and Inno Setup are in your path environment variables

$pathFromRoot = "MSUScripter\bin\Release\net9.0"
$exe = "MSUScripter.exe"
$timeDifference = 2

$scriptFolder = $PSScriptRoot
$outputFolder = "$PSScriptRoot\Output"
$parentFolder = Split-Path -parent $PSScriptRoot
$dll = $exe -replace ".exe", ".dll"

$releaseFolder = "$parentFolder\$pathFromRoot"
$win64Folder = "$releaseFolder\win-x64"
$win32Folder = "$releaseFolder\win-x86"
$linuxFolder = "$releaseFolder\linux-x64"
$win64Exe = "$win64Folder\$exe"
$win32Exe = "$win32Folder\$exe"
$linuxExe = "$linuxFolder\$dll"

Write-Host "Win64 Exe Path: $win64Exe"
Write-Host "Win32 Exe Path: $win32Exe"
Write-Host "Linux Exe Path: $linuxExe"

if (Test-Path "$win64Folder") {
    Remove-Item "$win64Folder" -Recurse -Force
}

if (Test-Path "$win32Folder") {
    Remove-Item "$win32Folder" -Recurse -Force
}

if (Test-Path "$linuxFolder") {
    Remove-Item "$linuxFolder" -Recurse -Force
}

Write-Host "Deleted previous builds"

dotnet publish --os win --arch x64 -c Release --framework net9.0 --self-contained false ../MSUScripter/MSUScripter.csproj
dotnet publish --os win --arch x86 -c Release --framework net9.0 --self-contained false  ../MSUScripter/MSUScripter.csproj
dotnet publish --os linux --arch x64 -c Release --framework net9.0 --self-contained false ../MSUScripter/MSUScripter.csproj

$hasError = $false

if (-not(Test-Path -Path $win64Exe -PathType Leaf)) {
    Write-Error "Win64 Executable not found at $win64Exe"
    $hasError = $true
}

if (-not(Test-Path -Path $win32Exe -PathType Leaf)) {
    Write-Error "Win32 Executable not found at $win32Exe"
    $hasError = $true
}

if (-not(Test-Path -Path $linuxExe -PathType Leaf)) {
    Write-Error "linux Executable not found at $linuxExe"
    $hasError = $true
}

if ($hasError) {
    exit
}

Write-Host ""

$win64Version = (Get-Item $win64Exe).VersionInfo.ProductVersion
$win32Version = (Get-Item $win32Exe).VersionInfo.ProductVersion
$linuxVersion = (Get-Item $linuxExe).VersionInfo.ProductVersion

Write-Host "Win64 Version: $win64Version"
Write-Host "Win32 Version: $win32Version"
Write-Host "Linux Version: $linuxVersion"

if ($win64Version -ne $win32Version -or $win32Version -ne $linuxVersion) {
    Write-Error "File version mismatch"
    exit
}

Write-Host ""

$win64ModifiedDate = (Get-Item $win64Exe).LastWriteTime
$win32ModifiedDate = (Get-Item $win32Exe).LastWriteTime
$linuxModifiedDate = (Get-Item $linuxExe).LastWriteTime

Write-Host "Win64 Modified Date: $win64ModifiedDate"
Write-Host "Win32 Modified Date: $win32ModifiedDate"
Write-Host "Linux Modified Date: $linuxModifiedDate"

$dateDiff1 = [Math]::Abs(($win64ModifiedDate - $win32ModifiedDate).TotalMinutes);
$dateDiff2 = [Math]::Abs(($win32ModifiedDate - $linuxModifiedDate).TotalMinutes);

if ($dateDiff1 -gt $timeDifference -or $dateDiff2 -gt $timeDifference)
{
    $errorMessage = "Builds are more than $timeDifference minutes apart"
    Write-Error $errorMessage
    exit
}

$ErrorActionPreference = "Stop"

iscc "$scriptFolder\MSUScripter.iss"

$zipFileName = $exe -replace ".exe", "_$linuxVersion-linux.zip"

if (Test-Path "$outputFolder\$zipFileName") {
    Remove-Item "$outputFolder\$zipFileName" -Recurse -Force
}

& "7z.exe" a -tzip "$outputFolder\$zipFileName" "$linuxFolder\publish\*" -aoa
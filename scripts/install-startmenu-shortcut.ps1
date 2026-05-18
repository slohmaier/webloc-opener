# Creates a Start Menu shortcut in the user's slohmaier folder
# pointing at the locally published dev build of webloc-opener.
$ErrorActionPreference = 'Stop'

$exe = Join-Path $PSScriptRoot '..\WeblocOpener\bin\Release\net8.0-windows\win-x64\publish\webloc-opener.exe'
$exe = [System.IO.Path]::GetFullPath($exe)

if (-not (Test-Path $exe)) {
    throw "exe missing: $exe -- run dotnet publish first."
}

$folder = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\slohmaier'
if (-not (Test-Path $folder)) {
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
}

$lnk = Join-Path $folder 'webloc-opener (Dev).lnk'

$ws = New-Object -ComObject WScript.Shell
$s = $ws.CreateShortcut($lnk)
$s.TargetPath = $exe
$s.WorkingDirectory = Split-Path $exe -Parent
$s.IconLocation = "$exe,0"
$s.Description = 'webloc-opener (Dev) - opens .webloc shortcut files'
$s.Save()

Write-Output "exe: $exe"
Write-Output "lnk: $lnk"
Write-Output "ok"

# Brendigo USB Key: copies the TEST portable program to a removable USB flash drive.
# It does NOT register the USB token, unlock Windows, or alter Windows sign-in.
# Author: Brendigo | https://brendigo.com
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$DriveLetter
)
$ErrorActionPreference = "Stop"

Write-Host "Brendigo USB Key - USB portable setup (TEST ONLY)" -ForegroundColor Cyan
Write-Host "Ova skripta samo kopira prijenosni program; registracija kljuca radi se u aplikaciji."
Write-Host "Windows lozinka / Hello ostaju obvezni za otkljucavanje sustava."

if ([string]::IsNullOrWhiteSpace($DriveLetter)) {
    $DriveLetter = Read-Host "Upisi slovo USB pogona, npr. E:"
}
$inputDrive = $DriveLetter.Trim()
if ($inputDrive -notmatch '^[a-zA-Z](:\\?)?$') {
    throw "Dopusteno je samo slovo pogona (npr. E:), bez putanja."
}

$root = $inputDrive.Substring(0, 1).ToUpperInvariant() + ":\"
$drive = [System.IO.DriveInfo]::new($root)
if (-not $drive.IsReady -or $drive.DriveType -ne [System.IO.DriveType]::Removable) {
    throw "Odabrani pogon nije spreman izmjenjivi USB stick. Nista nije kopirano."
}

$sourceExe = Join-Path -Path $PSScriptRoot -ChildPath "Brendigo-USB-Key-Portable.exe"
if (-not (Test-Path -LiteralPath $sourceExe -PathType Leaf)) {
    throw "Nedostaje Portable.exe u USB Setup Kitu. Preuzmi kompletan paket s GitHuba."
}
$target = Join-Path -Path $root -ChildPath "Brendigo-USB-Key-Portable"

if (Test-Path -LiteralPath $target) {
    $existing = Get-Item -LiteralPath $target -Force
    if (($existing.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Odredišna mapa nije dopuštena jer je simbolička poveznica."
    }
}

$required = (Get-Item -LiteralPath $sourceExe).Length + 1048576
if ($drive.AvailableFreeSpace -lt $required) {
    throw "Nema dovoljno slobodnog prostora na USB sticku."
}

Write-Host "Odabrani USB: $root"
Write-Host "Odrediste: $target"
$confirm = Read-Host "Za potvrdu kopiranja upisi DA"
if ($confirm -cne "DA") {
    Write-Host "Prekinuto. Nista nije kopirano."
    exit 0
}

New-Item -Path $target -ItemType Directory -Force | Out-Null
$targetExe = Join-Path -Path $target -ChildPath "Brendigo-USB-Key-Portable.exe"
Copy-Item -LiteralPath $sourceExe -Destination $targetExe -Force

foreach ($file in @("SETUP-USB-HR.md", "SETUP-USB-EN.md", "TROUBLESHOOTING-HR.md", "START-HERE.txt")) {
    $sourceDoc = Join-Path -Path $PSScriptRoot -ChildPath $file
    if (Test-Path -LiteralPath $sourceDoc -PathType Leaf) {
        Copy-Item -LiteralPath $sourceDoc -Destination (Join-Path -Path $target -ChildPath $file) -Force
    }
}

$sourceHash = (Get-FileHash -LiteralPath $sourceExe -Algorithm SHA256).Hash
$targetHash = (Get-FileHash -LiteralPath $targetExe -Algorithm SHA256).Hash
if ($sourceHash -cne $targetHash) {
    Remove-Item -LiteralPath $targetExe -Force -ErrorAction SilentlyContinue
    throw "Provjera SHA-256 nije uspjela. Kopirana EXE datoteka uklonjena je."
}
Write-Host "Uspjeh: program za TESTIRANJE kopiran je na USB." -ForegroundColor Green
Write-Host "Portable.exe se pokrece rucno nakon Windows prijave."
Write-Host "USB token registriraj zasebno unutar Brendigo USB Key aplikacije."
Write-Host "Obicni USB token moze se kopirati. USB jos ne otkljucava Windows prijavu."

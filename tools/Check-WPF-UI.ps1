# Lightweight static checks for WPF event handlers, local assets and documentation.
# This is NOT a complete dead-code or penetration audit.
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root "src/Brendigo.USBKey"
$views = Get-ChildItem -Path $source -Recurse -Filter "*.xaml" -File
$checks = 0

foreach ($view in $views) {
    $markup = Get-Content -LiteralPath $view.FullName -Raw -Encoding utf8
    try { [xml]$null = $markup }
    catch { throw "Invalid XAML XML: $($view.FullName): $($_.Exception.Message)" }
    $codeBehind = "$($view.FullName).cs"

    $matches = [regex]::Matches(
        $markup,
        '(?:Click|SelectionChanged|RequestNavigate)="([A-Za-z_][A-Za-z0-9_]*)"'
    )
    if ($matches.Count -gt 0 -and -not (Test-Path -LiteralPath $codeBehind)) {
        throw "Missing code-behind for $($view.Name)"
    }
    $sourceCode = if (Test-Path -LiteralPath $codeBehind) {
        Get-Content -LiteralPath $codeBehind -Raw -Encoding utf8
    } else { "" }

    foreach ($match in $matches) {
        $handler = $match.Groups[1].Value
        if ($sourceCode -notmatch ('\b' + [regex]::Escape($handler) + '\s*\(')) {
            throw "Missing WPF event handler $handler in $codeBehind"
        }
        Write-Host "PASS WPF event $($view.Name) -> $handler"
        $checks++
    }

    $names = [regex]::Matches($markup, 'x:Name="([^"]+)"') |
        ForEach-Object { $_.Groups[1].Value }
    if (($names | Select-Object -Unique).Count -ne @($names).Count) {
        throw "Duplicate x:Name in $($view.Name)"
    }
}

$readme = Get-Content -LiteralPath (Join-Path $root "README.md") -Raw -Encoding utf8
$images = [regex]::Matches($readme, 'assets/screenshots/([a-z0-9-]+\.png)') |
    ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
foreach ($name in $images) {
    $filepath = Join-Path $root "assets/screenshots/$name"
    if (-not (Test-Path -LiteralPath $filepath -PathType Leaf)) {
        throw "Missing documented screenshot: $name"
    }
    $checks++
}
if (@($images).Count -ne 12) {
    throw "README must link all 12 WPF snapshots, found $(@($images).Count)."
}

foreach ($file in @(
    "assets/brendigo-usb-key.ico",
    "assets/brendigo-usb-key.svg",
    "LICENSE",
    "docs/SETUP-USB-HR.md",
    "docs/SETUP-USB-EN.md",
    "tools/Install-To-USB.ps1"
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $file) -PathType Leaf)) {
        throw "Missing required local asset or document: $file"
    }
    $checks++
}

$project = Get-Content -LiteralPath (Join-Path $source "Brendigo.USBKey.csproj") -Raw
if ($project -notmatch '<Authors>Brendigo</Authors>') {
    throw "Brendigo authorship metadata is missing."
}
Write-Host "PASS: $checks static WPF wiring, screenshot and local-file checks."

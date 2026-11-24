param(
    [string]$Version = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root

try {
    $running = Get-Process ScreenDraw -ErrorAction SilentlyContinue
    if ($running) {
        throw "ScreenDraw is running (PID $(($running.Id) -join ', ')). Quit it from the tray first: the publish output is locked while it runs."
    }

    if (-not $Version) {
        $Version = (dotnet msbuild src/ScreenDraw.App/ScreenDraw.App.csproj -getProperty:Version).Trim()
    }

    Remove-Item 'artifacts\win-x64' -Recurse -Force -ErrorAction SilentlyContinue
    dotnet publish src/ScreenDraw.App -p:PublishProfile=win-x64 -v minimal

    $zip = "artifacts\screen-draw-$Version-win-x64.zip"
    Remove-Item $zip -Force -ErrorAction SilentlyContinue
    Compress-Archive -Path 'artifacts\win-x64\*' -DestinationPath $zip

    $hashes = Get-FileHash -Algorithm SHA256 $zip, 'artifacts\win-x64\ScreenDraw.exe'
    $lines = $hashes | ForEach-Object { "$($_.Hash.ToLower())  $(Split-Path $_.Path -Leaf)" }
    $lines | Set-Content 'artifacts\SHA256SUMS.txt'

    $lines
    "release: $zip"
}
finally {
    Pop-Location
}

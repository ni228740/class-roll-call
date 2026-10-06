$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

New-Item -ItemType Directory -Force -Path "name" | Out-Null
New-Item -ItemType Directory -Force -Path "build" | Out-Null

function Ensure-AppIcon($Path) {
    if (Test-Path $Path) { return }
    Add-Type -AssemblyName System.Drawing
    $bmp = New-Object System.Drawing.Bitmap(32, 32)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $p = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 176, 200))
    $c = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(226, 92, 142))
    for ($i = 0; $i -lt 5; $i++) {
        $a = $i * 2 * [Math]::PI / 5 - [Math]::PI / 2
        $g.FillEllipse($p, [int](16 + 7 * [Math]::Cos($a)) - 5, [int](16 + 7 * [Math]::Sin($a)) - 5, 10, 10)
    }
    $g.FillEllipse($c, 11, 11, 10, 10); $g.Dispose()
    $h = $bmp.GetHicon(); $ic = [System.Drawing.Icon]::FromHandle($h)
    $s = [System.IO.File]::Create($Path); $ic.Save($s); $s.Close(); $s.Dispose(); $bmp.Dispose()
}
$iconFile = Join-Path $root "build\app.ico"
Ensure-AppIcon -Path $iconFile

$compiler = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (-not (Test-Path $compiler)) {
    throw "Cannot find .NET Framework compiler: $compiler"
}

$asciiOutput = "ClassRollCall.exe"
$chineseBaseName = -join ([char[]](0x73ED, 0x7EA7, 0x70B9, 0x540D, 0x5668))
$chineseOutput = $chineseBaseName + ".exe"

& $compiler `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /optimize+ `
    /codepage:65001 `
    "/out:$asciiOutput" `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    "/win32icon:$iconFile" `
    "src\Program.cs"

if ($LASTEXITCODE -ne 0) {
    throw "C# build failed."
}

Copy-Item -Path $asciiOutput -Destination $chineseOutput -Force
Remove-Item -Path $asciiOutput -Force
Write-Host ("Build finished: " + $chineseOutput)

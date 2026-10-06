$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Find-Python {
    $commands = @("python", "py")
    foreach ($cmd in $commands) {
        $found = Get-Command $cmd -ErrorAction SilentlyContinue
        if ($found) {
            return $cmd
        }
    }

    $registryPaths = @(
        "HKCU:\Software\Python\PythonCore\3.13\InstallPath",
        "HKCU:\Software\Python\PythonCore\3.12\InstallPath",
        "HKCU:\Software\Python\PythonCore\3.11\InstallPath",
        "HKLM:\Software\Python\PythonCore\3.13\InstallPath",
        "HKLM:\Software\Python\PythonCore\3.12\InstallPath",
        "HKLM:\Software\Python\PythonCore\3.11\InstallPath"
    )
    foreach ($path in $registryPaths) {
        if (Test-Path $path) {
            $value = (Get-ItemProperty -LiteralPath $path).ExecutablePath
            if ($value) {
                return $value
            }
        }
    }

    throw "Cannot find Python. Add Python to PATH, then run this script again."
}

$python = Find-Python
Write-Host "Using Python: $python"

try {
    & $python -m PyInstaller --version | Out-Host
} catch {
    Write-Host "PyInstaller is not installed."
    Write-Host "Install it with: $python -m pip install pyinstaller"
    throw "PyInstaller missing."
}

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

$entry = Join-Path $root "python_app\class_roll_call.py"
$chineseBaseName = -join ([char[]](0x73ED, 0x7EA7, 0x70B9, 0x540D, 0x5668, 0x005F, 0x0050, 0x0079, 0x0074, 0x0068, 0x006F, 0x006E, 0x7248))
& $python -m PyInstaller `
    --noconfirm `
    --clean `
    --windowed `
    --onefile `
    --name $chineseBaseName `
    --distpath $root `
    --workpath (Join-Path $root "build\pyinstaller") `
    --specpath (Join-Path $root "build") `
    --icon $iconFile `
    "$entry"

New-Item -ItemType Directory -Force -Path (Join-Path $root "name") | Out-Null
Write-Host ("Build finished: " + $chineseBaseName + ".exe")

# Generates app.ico (multi-size, PNG-compressed frames) with the Memo mark:
# rounded blue square + white "M". Run from the project root:
#   powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1
param(
    [string]$OutPath = (Join-Path $PSScriptRoot '..\app.ico')
)

Add-Type -AssemblyName System.Drawing

$sizes = 16, 24, 32, 48, 64, 128, 256
$accent = [System.Drawing.Color]::FromArgb(79, 124, 244)
$frames = New-Object System.Collections.ArrayList

foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::Transparent)

    $r = [int][Math]::Round($s * 0.25)
    if ($r -lt 1) { $r = 1 }
    $d = $r * 2
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $d, $d, 180, 90)
    $path.AddArc($s - $d, 0, $d, $d, 270, 90)
    $path.AddArc($s - $d, $s - $d, $d, $d, 0, 90)
    $path.AddArc(0, $s - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $brush = New-Object System.Drawing.SolidBrush($accent)
    $g.FillPath($brush, $path)

    $font = New-Object System.Drawing.Font('Segoe UI', [float]($s * 0.6), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $sf = New-Object System.Drawing.StringFormat
    $sf.Alignment = [System.Drawing.StringAlignment]::Center
    $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $rect = [System.Drawing.RectangleF]::new([float]0, [float](-$s * 0.03), [float]$s, [float]$s)
    $g.DrawString('M', $font, $white, $rect, $sf)
    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    [void]$frames.Add(@{ Size = $s; Data = $ms.ToArray() })
    $ms.Dispose()
    $bmp.Dispose()
    $path.Dispose(); $brush.Dispose(); $font.Dispose(); $sf.Dispose(); $white.Dispose()
}

# ICO container: 6-byte header + 16-byte directory entries + frame data
$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$frames.Count)
$offset = 6 + 16 * $frames.Count
foreach ($f in $frames) {
    $dim = if ($f.Size -ge 256) { 0 } else { $f.Size }
    $bw.Write([byte]$dim)
    $bw.Write([byte]$dim)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$f.Data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $f.Data.Length
}
foreach ($f in $frames) { $bw.Write($f.Data) }
$bw.Flush()
[System.IO.File]::WriteAllBytes($OutPath, $out.ToArray())
$bw.Dispose(); $out.Dispose()

Write-Output ("wrote " + (Resolve-Path $OutPath) + " (" + (Get-Item $OutPath).Length + " bytes, " + $frames.Count + " sizes)")

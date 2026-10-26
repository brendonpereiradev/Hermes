# Gera Assets/app.ico e Assets/app.png a partir dos emblemas de Themes/Brand.xaml.
# Roda no Windows PowerShell (STA), que já traz o WPF. Use depois de mudar o emblema:
#   powershell -NoProfile -File scripts\New-HermesIcons.ps1

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

$repoRoot = Split-Path -Parent $PSScriptRoot
$appDir = Join-Path $repoRoot "src\PrinterInstall.App"
$brandXaml = Join-Path $appDir "Themes\Brand.xaml"
$assetsDir = Join-Path $appDir "Assets"

$stream = [System.IO.File]::OpenRead($brandXaml)
try {
    $brand = [System.Windows.Markup.XamlReader]::Load($stream)
}
finally {
    $stream.Dispose()
}

function Get-EmblemPng([int]$size) {
    # Cada tamanho usa o desenho feito para ele: menos detalhe quanto menor o ícone.
    $key = if ($size -le 16) { "HermesEmblemTinyImage" } elseif ($size -le 32) { "HermesEmblemSmallImage" } else { "HermesEmblemImage" }
    $image = $brand[$key]

    $visual = New-Object System.Windows.Media.DrawingVisual
    $context = $visual.RenderOpen()
    $context.DrawImage($image, (New-Object System.Windows.Rect 0, 0, $size, $size))
    $context.Close()

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)

    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $memory = New-Object System.IO.MemoryStream
    $encoder.Save($memory)
    return ,$memory.ToArray()
}

[System.IO.File]::WriteAllBytes((Join-Path $assetsDir "app.png"), (Get-EmblemPng 256))

# ICO com quadros PNG (aceito desde o Windows Vista).
$sizes = 16, 20, 24, 32, 48, 64, 128, 256
$frames = $sizes | ForEach-Object { ,(Get-EmblemPng $_) }

$output = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter $output
$writer.Write([UInt16]0)
$writer.Write([UInt16]1)
$writer.Write([UInt16]$sizes.Count)

$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dimension = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]32)
    $writer.Write([UInt32]$frames[$i].Length)
    $writer.Write([UInt32]$offset)
    $offset += $frames[$i].Length
}
foreach ($frame in $frames) {
    $writer.Write($frame)
}
$writer.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $assetsDir "app.ico"), $output.ToArray())

Write-Host "Ícones gerados em $assetsDir"

param(
    [Parameter(Mandatory = $true)][string]$InputPath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

Add-Type -AssemblyName System.Drawing

$source = [System.Drawing.Bitmap]::FromFile($InputPath)
$width = $source.Width
$height = $source.Height
$background = [System.Drawing.Color]::FromArgb(255, 181, 176, 170) # #B5B0AA
$visited = New-Object 'bool[]' ($width * $height)
$queue = [System.Collections.Generic.Queue[int]]::new()

function Try-Enqueue([int]$x, [int]$y) {
    if ($x -lt 0 -or $y -lt 0 -or $x -ge $width -or $y -ge $height) { return }
    $index = $y * $width + $x
    if ($visited[$index]) { return }
    $pixel = $source.GetPixel($x, $y)
    # Generated backdrop is a warm grey field near RGB 175/169/163. Limit to it,
    # then flood from the canvas edges so similarly coloured character materials remain intact.
    if ($pixel.R -lt 135 -or $pixel.R -gt 205 -or
        $pixel.G -lt 130 -or $pixel.G -gt 200 -or
        $pixel.B -lt 125 -or $pixel.B -gt 195) { return }
    if (([Math]::Abs($pixel.R - $pixel.G) -gt 18) -or
        ([Math]::Abs($pixel.G - $pixel.B) -gt 18)) { return }
    $visited[$index] = $true
    $queue.Enqueue($index)
}

for ($x = 0; $x -lt $width; $x++) { Try-Enqueue $x 0; Try-Enqueue $x ($height - 1) }
for ($y = 0; $y -lt $height; $y++) { Try-Enqueue 0 $y; Try-Enqueue ($width - 1) $y }

while ($queue.Count -gt 0) {
    $index = $queue.Dequeue()
    $x = $index % $width
    $y = [Math]::Floor($index / $width)
    Try-Enqueue ($x - 1) $y
    Try-Enqueue ($x + 1) $y
    Try-Enqueue $x ($y - 1)
    Try-Enqueue $x ($y + 1)
}

$final = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $index = $y * $width + $x
        if ($visited[$index]) { $final.SetPixel($x, $y, $background) }
        else { $final.SetPixel($x, $y, $source.GetPixel($x, $y)) }
    }
}

$folder = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $folder)) { New-Item -ItemType Directory -Path $folder -Force | Out-Null }
$final.Save($OutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$final.Dispose()
$source.Dispose()

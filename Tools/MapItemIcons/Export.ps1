param([string]$ManifestPath = (Join-Path $PSScriptRoot 'Manifest.json'))

Add-Type -AssemblyName System.Drawing
$workspace = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$assetFolder = Join-Path $workspace 'Assets/4. SlimeAsset'
$manifest = [System.IO.File]::ReadAllText($ManifestPath) | ConvertFrom-Json
$template = [System.IO.File]::ReadAllText((Join-Path $assetFolder 'UI_Icon_Settings.png.meta'))

foreach ($item in $manifest.Icons) {
    if (Test-Path -LiteralPath (Join-Path $assetFolder $item.FileName)) {
        throw ('Asset already exists: ' + $item.FileName)
    }
}

foreach ($item in $manifest.Icons) {
    $source = [System.Drawing.Bitmap]::new($item.Source)
    try {
        if ($source.Width -ne $source.Height) {
            throw ('A square source is required; refusing aspect distortion: ' + $item.FileName)
        }
        $targetPath = Join-Path $assetFolder $item.FileName
        if ($source.Width -eq 1254) {
            Copy-Item -LiteralPath $item.Source -Destination $targetPath
        }
        else {
            $output = [System.Drawing.Bitmap]::new(1254,1254,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
            $graphics = [System.Drawing.Graphics]::FromImage($output)
            $attributes = [System.Drawing.Imaging.ImageAttributes]::new()
            try {
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $attributes.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
                $graphics.DrawImage($source,[System.Drawing.Rectangle]::new(0,0,1254,1254),0,0,$source.Width,$source.Height,[System.Drawing.GraphicsUnit]::Pixel,$attributes)
                $output.Save($targetPath,[System.Drawing.Imaging.ImageFormat]::Png)
            }
            finally { $attributes.Dispose(); $graphics.Dispose(); $output.Dispose() }
        }
    }
    finally { $source.Dispose() }

    $metadata = [regex]::Replace($template,'(?m)^guid: [0-9a-f]+',('guid: '+[guid]::NewGuid().ToString('N')))
    $metadata = [regex]::Replace($metadata,'(?m)^(\s*)spriteID: [0-9a-f]+',('$1spriteID: '+[guid]::NewGuid().ToString('N')))
    $metadata = [regex]::Replace($metadata,'(?m)^  userData:.*',('  userData: SLIME POP '+$item.Category+' icon - '+$item.Name+'; 1254x1254; original aspect preserved'))
    if (-not (Test-Path -LiteralPath ($targetPath+'.meta'))) {
        [System.IO.File]::WriteAllText(($targetPath+'.meta'),$metadata,[System.Text.UTF8Encoding]::new($false))
    }
    Write-Output $item.FileName
}

foreach ($category in @('Map','Item')) {
    $group = @($manifest.Icons | Where-Object { $_.Category -eq $category })
    $columns = if ($category -eq 'Map') { 4 } else { 3 }
    $rows = [int][Math]::Ceiling($group.Count/$columns)
    $sheet = [System.Drawing.Bitmap]::new($columns*256,$rows*290,[System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($sheet)
    $font = [System.Drawing.Font]::new('Malgun Gothic',16,[System.Drawing.FontStyle]::Bold)
    $brush = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(50,53,52))
    $format = [System.Drawing.StringFormat]::new()
    try {
        $graphics.Clear([System.Drawing.Color]::FromArgb(243,242,237))
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $format.Alignment = [System.Drawing.StringAlignment]::Center
        $format.LineAlignment = [System.Drawing.StringAlignment]::Center
        for ($i=0; $i -lt $group.Count; $i++) {
            $x = ($i%$columns)*256
            $y = [int][Math]::Floor($i/$columns)*290
            $sprite = [System.Drawing.Image]::FromFile((Join-Path $assetFolder $group[$i].FileName))
            try { $graphics.DrawImage($sprite,[System.Drawing.Rectangle]::new($x,$y,256,256)) }
            finally { $sprite.Dispose() }
            $graphics.DrawString($group[$i].Label,$font,$brush,[System.Drawing.RectangleF]::new($x,$y+252,256,38),$format)
        }
        $sheet.Save((Join-Path $PSScriptRoot ($category+'_Preview.png')),[System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $format.Dispose(); $brush.Dispose(); $font.Dispose(); $graphics.Dispose(); $sheet.Dispose() }
}

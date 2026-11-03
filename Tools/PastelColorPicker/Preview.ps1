# Preview illustration only; does not change Unity assets, scenes or prefabs.
Add-Type -AssemblyName System.Drawing
$previewRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$previewImages = @{}
$previewNames = @{
    paper = 'UI_Popup_Background_Paper.png'
    paint = 'UI_Palette_Paint_White.png'
    close = 'UI_Icon_Close.png'
    check = 'UI_Icon_Check_Red.png'
    apply = 'UI_Button_Normal.png'
    body = 'Slime_Modular/Slime_White_Faceless.png'
    face = 'Slime_Modular/Slime_Face_Normal.png'
}
foreach ($previewKey in $previewNames.Keys) {
    $previewImages[$previewKey] = [System.Drawing.Image]::FromFile((Join-Path $previewRoot ('Assets/4. SlimeAsset/' + $previewNames[$previewKey])))
}
$previewBitmap = [System.Drawing.Bitmap]::new(900,700)
$previewGraphics = [System.Drawing.Graphics]::FromImage($previewBitmap)
$previewGraphics.Clear([System.Drawing.Color]::FromArgb(232,228,219))
$previewGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$previewGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$previewFormat = [System.Drawing.StringFormat]::new()
$previewFormat.Alignment = [System.Drawing.StringAlignment]::Center
$previewFormat.LineAlignment = [System.Drawing.StringAlignment]::Center
$previewInk = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(56,51,46))
$previewHintInk = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(122,112,99))
$previewTitleFont = [System.Drawing.Font]::new('Malgun Gothic',36,[System.Drawing.FontStyle]::Bold,[System.Drawing.GraphicsUnit]::Pixel)
$previewLabelFont = [System.Drawing.Font]::new('Malgun Gothic',24,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
$previewHintFont = [System.Drawing.Font]::new('Malgun Gothic',22,[System.Drawing.FontStyle]::Regular,[System.Drawing.GraphicsUnit]::Pixel)
function Draw-PreviewTint([System.Drawing.Image] $sourceImage, [System.Drawing.Rectangle] $targetRect, [string] $tintHex) {
    $tintColor = [System.Drawing.ColorTranslator]::FromHtml('#' + $tintHex)
    $tintMatrix = [System.Drawing.Imaging.ColorMatrix]::new()
    $tintMatrix.Matrix00 = $tintColor.R / 255.0
    $tintMatrix.Matrix11 = $tintColor.G / 255.0
    $tintMatrix.Matrix22 = $tintColor.B / 255.0
    $tintAttributes = [System.Drawing.Imaging.ImageAttributes]::new()
    $tintAttributes.SetColorMatrix($tintMatrix)
    $previewGraphics.DrawImage($sourceImage,$targetRect,0,0,$sourceImage.Width,$sourceImage.Height,[System.Drawing.GraphicsUnit]::Pixel,$tintAttributes)
    $tintAttributes.Dispose()
}
$previewGraphics.DrawImage($previewImages.paper,[System.Drawing.Rectangle]::new(30,50,840,600))
$previewGraphics.DrawString('슬라임 색상',$previewTitleFont,$previewInk,[System.Drawing.RectangleF]::new(140,95,620,56),$previewFormat)
$previewGraphics.DrawString('물감을 눌러 색상을 골라주세요',$previewHintFont,$previewHintInk,[System.Drawing.RectangleF]::new(150,149,600,35),$previewFormat)
$previewGraphics.DrawImage($previewImages.close,[System.Drawing.Rectangle]::new(776,100,48,48))
Draw-PreviewTint $previewImages.body ([System.Drawing.Rectangle]::new(253,156,144,144)) 'F3B6D0'
$previewGraphics.DrawImage($previewImages.face,[System.Drawing.Rectangle]::new(253,156,144,144))
$previewGraphics.DrawString('분홍색',$previewLabelFont,$previewInk,[System.Drawing.RectangleF]::new(380,216,230,44),$previewFormat)
$previewSwatches = @(
    @{ name = '하얀색'; hex = 'FFFFFF' },
    @{ name = '검정색'; hex = '3C3C48' },
    @{ name = '빨간색'; hex = 'F28D8D' },
    @{ name = '파란색'; hex = '93BDF1' },
    @{ name = '분홍색'; hex = 'F3B6D0' },
    @{ name = '노란색'; hex = 'F6DE91' },
    @{ name = '초록색'; hex = '8CC5A1' },
    @{ name = '연두색'; hex = 'C9DF94' },
    @{ name = '갈색'; hex = 'B5917D' },
    @{ name = '남색'; hex = '697A9D' }
)
for ($previewIndex = 0; $previewIndex -lt $previewSwatches.Count; $previewIndex++) {
    $previewCenterX = 450 + (($previewIndex % 5) - 2) * 140
    $previewCenterY = 350 + [int][Math]::Floor($previewIndex / 5) * 130
    Draw-PreviewTint $previewImages.paint ([System.Drawing.Rectangle]::new($previewCenterX-54,$previewCenterY-54,108,108)) $previewSwatches[$previewIndex].hex
    $previewGraphics.DrawString($previewSwatches[$previewIndex].name,$previewLabelFont,$previewInk,[System.Drawing.RectangleF]::new($previewCenterX-61,$previewCenterY+48,122,36),$previewFormat)
    if ($previewIndex -eq 4) {
        $previewGraphics.DrawImage($previewImages.check,[System.Drawing.Rectangle]::new($previewCenterX+14,$previewCenterY-56,42,42))
    }
}
$previewGraphics.DrawImage($previewImages.apply,[System.Drawing.Rectangle]::new(350,564,200,72))
$previewGraphics.DrawString('적용',$previewLabelFont,$previewInk,[System.Drawing.RectangleF]::new(370,580,160,40),$previewFormat)
$previewBitmap.Save((Join-Path $PSScriptRoot 'Preview.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$previewGraphics.Dispose()
$previewBitmap.Dispose()
$previewFormat.Dispose()
$previewInk.Dispose()
$previewHintInk.Dispose()
$previewTitleFont.Dispose()
$previewLabelFont.Dispose()
$previewHintFont.Dispose()
foreach ($previewImage in $previewImages.Values) { $previewImage.Dispose() }

$ErrorActionPreference='Stop'
$stage=$PSScriptRoot
$project='C:\Users\user\Desktop\Programing\Project-B-SilmePOP'
$relative='Assets/4. SlimeAsset/Atlases/Map_MenuPapers.png'
$target=Join-Path $project $relative
$meta=[IO.File]::ReadAllText($target+'.meta')
$themes=@('Basic','Spring','Summer','Autumn','Winter','Sunny','Night','Cloudy')
$blocks=[regex]::Matches($meta,'(?s)    - serializedVersion: 2\r?\n      name: (Map_\w+_MenuPaper).*?(?=    - serializedVersion: 2|    outline: \[\])')
if($blocks.Count -ne 8){throw 'Expected eight existing sprite definitions'}
for($i=0;$i -lt 8;$i++){
  $block=$blocks[$i].Value
  if($blocks[$i].Groups[1].Value -ne ('Map_'+$themes[$i]+'_MenuPaper')){throw 'Unexpected sprite order'}
  $x=($i%4)*768;$y=if($i -lt 4){1024}else{0}
  if($block -notmatch ('(?m)^        x: '+$x+'\s*$') -or $block -notmatch ('(?m)^        y: '+$y+'\s*$') -or $block -notmatch '(?m)^        width: 768\s*$' -or $block -notmatch '(?m)^        height: 1024\s*$'){throw 'Existing sprite layout differs'}
}
Add-Type -AssemblyName System.Drawing
$atlas=[Drawing.Bitmap]::new(3072,2048,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g=[Drawing.Graphics]::FromImage($atlas)
$g.Clear([Drawing.Color]::Transparent)
$g.CompositingMode=[Drawing.Drawing2D.CompositingMode]::SourceCopy
$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$sources=@()
for($i=0;$i -lt 8;$i++){
  $path='Assets/4. SlimeAsset/Map/Menu/'+$themes[$i]+'_Menu.png'
  $source=Join-Path $project $path
  $bitmap=[Drawing.Bitmap]::new($source)
  if($bitmap.Width -ne 2400 -or $bitmap.Height -ne 3200){throw 'Original menu dimensions changed'}
  $x=($i%4)*768;$y=[int][Math]::Floor($i/4)*1024
  $g.DrawImage($bitmap,[Drawing.Rectangle]::new($x,$y,768,1024),0,0,2400,3200,[Drawing.GraphicsUnit]::Pixel)
  $bitmap.Dispose()
  $sources+=[pscustomobject]@{Theme=$themes[$i];Path=$path;Hash=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash;Name=('Map_'+$themes[$i]+'_MenuPaper');FileID=(21300001+$i);X=$x;Y=(2048-$y-1024);Width=768;Height=1024}
}
$g.Dispose()
foreach($i in 0..7){
  $x=($i%4)*768;$y=[int][Math]::Floor($i/4)*1024
  if($atlas.GetPixel($x,$y).A -ne 0 -or $atlas.GetPixel($x+384,$y+512).A -lt 245){throw 'Atlas cell alpha validation failed'}
}
$output=Join-Path $stage 'Map_MenuPapers.png'
$atlas.Save($output,[Drawing.Imaging.ImageFormat]::Png);$atlas.Dispose()
$manifest=[pscustomobject]@{Path=$relative;Before=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash;After=(Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash;MetaHash=(Get-FileHash -LiteralPath ($target+'.meta') -Algorithm SHA256).Hash;Sources=$sources}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding UTF8
[IO.File]::WriteAllText((Join-Path $stage 'Validation.txt'),'PASS: 3072 x 2048 RGBA atlas; eight 768 x 1024 cells; transparent cell padding and opaque centers; existing SpriteMultiple names, rectangles, GUID and IDs preserved; order Basic Spring Summer Autumn Winter Sunny Night Cloudy.',[Text.UTF8Encoding]::new($false))
Write-Output 'Packed 8 notebook menu sprites: 3072 x 2048 atlas; 768 x 1024 per sprite; existing metadata validated.'

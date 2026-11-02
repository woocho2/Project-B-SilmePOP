param([Parameter(Mandatory = $true)][string]$SourceImage)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$assetPath = Join-Path $projectRoot 'Assets/4. SlimeAsset/SlimePop_TitleLetters.png'
if (Test-Path -LiteralPath $assetPath) { throw 'Atlas already exists; preserve the existing asset and its GUID.' }
Copy-Item -LiteralPath $SourceImage -Destination $assetPath
Add-Type -AssemblyName System.Drawing
$atlasBitmap = [Drawing.Bitmap]::new($assetPath)
try {
    $glyphs = @('S', 'L', 'I', 'M', 'E', 'P', 'O', 'Exclamation')
    $spriteEntries = [Collections.Generic.List[string]]::new()
    $fileIdEntries = [Collections.Generic.List[string]]::new()
    $summary = [Collections.Generic.List[object]]::new()
    for ($glyphIndex = 0; $glyphIndex -lt $glyphs.Length; $glyphIndex++) {
        $column = $glyphIndex % 4
        $row = [Math]::Floor($glyphIndex / 4)
        $left = [int][Math]::Floor($column * $atlasBitmap.Width / 4)
        $right = [int][Math]::Floor(($column + 1) * $atlasBitmap.Width / 4)
        $top = [int][Math]::Floor($row * $atlasBitmap.Height / 2)
        $bottom = [int][Math]::Floor(($row + 1) * $atlasBitmap.Height / 2)
        $minX = $right; $maxX = $left - 1; $minY = $bottom; $maxY = $top - 1
        for ($pixelY = $top; $pixelY -lt $bottom; $pixelY++) {
            for ($pixelX = $left; $pixelX -lt $right; $pixelX++) {
                if ($atlasBitmap.GetPixel($pixelX, $pixelY).A -lt 8) { continue }
                $minX = [Math]::Min($minX, $pixelX); $maxX = [Math]::Max($maxX, $pixelX)
                $minY = [Math]::Min($minY, $pixelY); $maxY = [Math]::Max($maxY, $pixelY)
            }
        }
        if ($maxX -lt $minX) { throw "Missing glyph: $($glyphs[$glyphIndex])" }
        # Trim only sprite rectangles; retain the generated PNG and its original alpha unchanged.
        $spriteX = [Math]::Max($left, $minX - 3)
        $spriteTop = [Math]::Max($top, $minY - 3)
        $spriteRight = [Math]::Min($right, $maxX + 4)
        $spriteBottom = [Math]::Min($bottom, $maxY + 4)
        $spriteY = $atlasBitmap.Height - $spriteBottom
        $spriteWidth = $spriteRight - $spriteX
        $spriteHeight = $spriteBottom - $spriteTop
        $spriteName = 'Title_' + $glyphs[$glyphIndex]
        $fileId = 21300001 + $glyphIndex
        $spriteId = [Guid]::NewGuid().ToString('N')
        $spriteEntries.Add(@"
    - serializedVersion: 2
      name: $spriteName
      rect:
        serializedVersion: 2
        x: $spriteX
        y: $spriteY
        width: $spriteWidth
        height: $spriteHeight
      alignment: 0
      pivot: {x: 0.5, y: 0.5}
      border: {x: 0, y: 0, z: 0, w: 0}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: $spriteId
      internalID: $fileId
      vertices: []
      indices:
      edges: []
      weights: []
"@)
        $fileIdEntries.Add("      ${spriteName}: $fileId")
        $summary.Add([ordered]@{ name = $spriteName; fileId = $fileId; x = $spriteX; y = $spriteY; width = $spriteWidth; height = $spriteHeight })
    }
    $atlasGuid = [Guid]::NewGuid().ToString('N')
    $metadata = @"
fileFormatVersion: 2
guid: $atlasGuid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
  isReadable: 0
  streamingMipmaps: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  compressionQuality: 100
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  textureType: 8
  textureShape: 1
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    overridden: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
$($spriteEntries -join "`n")
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: $([Guid]::NewGuid().ToString('N'))
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable:
$($fileIdEntries -join "`n")
  userData: Eight title glyphs, S L I M / E P O !, transparent 4x2 atlas.
  assetBundleName:
  assetBundleVariant:
"@
    $utf8 = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText($assetPath + '.meta', $metadata + "`n", $utf8)
    [IO.File]::WriteAllText((Join-Path $PSScriptRoot 'Atlas.json'), ($summary | ConvertTo-Json -Depth 3), $utf8)
    [PSCustomObject]@{ Path = $assetPath; Width = $atlasBitmap.Width; Height = $atlasBitmap.Height; Glyphs = $summary.Count; CornerAlpha = $atlasBitmap.GetPixel(0, 0).A }
} finally { $atlasBitmap.Dispose() }

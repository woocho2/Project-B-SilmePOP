CURRENT FACE ALIGNMENT: Slime_Face_Normal.png shares the body's 1024x1024 canvas. The existing expression is 369x123px at (327,502), centered horizontally. Place body and face at the same position and size to match Slime_White_Normal.png. Outer alpha in the face layer is intentional alignment space. Only face size/placement changed; body artwork and Unity files remain untouched. No metadata edits or validation.

CURRENT BODY: built-in image_gen redraw from the BLUE ANGEL SLIME at the lower right of Assets/4. SlimeAsset/BackGround.png. ExportAngelReferenceBody.cs trims outer padding and saves 1024x1024. ComposeFace overlays the existing expression for the UI variant. Cartoon_Body.svg and WriteBody are obsolete historical attempts and are NOT sources of the current PNGs. BeforeAngelReference/ preserves the rejected round version. No metadata edits or output validation.

Latest correction: Slime_White_Faceless.png and Slime_White_Normal.png now use newly drawn round, plump 1024x1024 silhouettes. The contour reaches the top, bottom, left and right canvas edges; there are no vertical padding bands. SVG and ModularCartoonBody.cs contain the editable geometry. BeforeRoundBody1024/ preserves the previous padded bodies. Other modular parts remain unchanged. No .meta edits or output validation performed.

# Modular slime sprites — current cartoon assets

Latest user override: ALL seven PNGs now have exactly 1024x1024 square canvases. SquareCanvas1024.cs uniformly fits and centers each existing image without stretching its artwork. Transparent space needed to retain each part's natural proportions is kept. BeforeSquare1024/ contains the previous images. The natural-aspect production dimensions described below are historical source dimensions, not the current exported canvas dimensions. No .meta edits or validation were performed.

Assets: Assets/4. SlimeAsset/Slime_Modular/. Seven existing PNG filenames were retained and replaced with clean cartoon artwork.

| File | Use | Canvas |
| --- | --- | --- |
| Slime_White_Faceless.png | Fully opaque neutral-white body, crisp charcoal outline, simple gray lower cel shade | 1536x1024 (3:2) |
| Slime_White_Normal.png | EXACT same body with the new expression composited for standalone UI | Same as body |
| Slime_Face_Normal.png | Two black eyes and U smile only | Tight3:1 |
| Slime_Costume_Crown.png | Gold crown with red jewel only | Tight3:2 |
| Slime_Costume_Angel.png | Two wings and golden halo only | Tight2:1 |
| Slime_Costume_Horns.png | Two red horns only | Tight3:1 |
| Slime_Costume_Joker.png | Red/charcoal jester hat only | Tight3:1 |

## Placement

The user requested removal of large outer alpha margins. Each image is now framed to the part itself, with its natural aspect ratio. This deliberately replaces the old shared square canvas with embedded positioning offsets. Do not assume matching-size RectTransforms for body, face and costume will automatically align.

The user authors scene and prefab layout. Set each part's size and placement in the Editor, preserving its image aspect ratio. Use Slime_White_Normal as a single faced UI image if a separate expression layer is unnecessary. Gameplay can continue to use the faceless body plus the selected costume.

Transparent gaps BETWEEN eyes/mouth, BETWEEN horns, and between angel wings/halo are intentional assembly spaces, not outer canvas padding. Body interiors and filled accessory parts are opaque. Tint the neutral body; leave colored costumes and expression layers white.

No Unity .meta files were created, edited or inspected. No Unity execution, compilation, tests, output checks, Sprite slicing or layout validation was run. Existing game scripts, scene layout and prefabs were not changed.

## Production sources

- CartoonPlan.json: six built-in image_gen prompts and reference inputs.
- CartoonManifest.json: seven final output paths, production methods and sources.
- Cartoon_Body.svg: editable vector artwork for the simple solid grayscale body.
- ModularCartoonBody.cs: body PNG export and faced-body assembly; this is an offline asset utility, not runtime UI generation.
- BeforeCartoon/: original seven PNGs.
- README_PreCartoon.md and earlier previews: historical shared-canvas/crayon workflow; superseded by this cartoon pass.

# Modular slime overlay layout

Current asset directory: `Assets/4. SlimeAsset/Slime_Modular/`.

The body files `Slime_White_Faceless.png` and `Slime_White_Normal.png` are unchanged. They retain their original 1024x1024 canvases.

The following files now use 2048x2048 transparent canvases:

- `Slime_Face_Normal.png`
- `Slime_Costume_Crown.png`
- `Slime_Costume_Angel.png`
- `Slime_Costume_Horns.png`
- `Slime_Costume_Joker.png`

## Common coordinates

All five overlays reference the unchanged 1024px body at **(512,512)** within the 2048px canvas. The shared pivot is **(1024,1024)**. Coordinates in this document use a top-left origin.

The face layer is copied without scaling from its already aligned original canvas to (512,512). Its expression retains the same size and position as the one in Slime_White_Normal. Intentional transparency surrounding the face and costumes provides assembly space.

Crown: 760px artwork width. Its lower band follows the existing body's forehead contour, with a 14px overlap.

Joker: 1100px artwork width, same centered placement and 16px center overlap. A smooth quadratic contact arch is fitted using the continuous center portion of the source opening, avoiding the incorrect jump to curled-tip/bell boundaries. The bells can overhang the body within the larger canvas.

Angel: halo and each wing are separated from their original combined sprite and repositioned independently. Halo width 560px, lower edge 98px below the center forehead (moved down 168px from the initial layout). Wing width 330px each, roots overlap the body shoulders; wing placement unchanged.

Horns: each existing horn is placed independently at 28%/72% of the body width, scaled to 220px wide, tilted outward 28 degrees and seated against the forehead with 110px root overlap (moved down 72px from the initial layout). Size, angle and horizontal position unchanged.

## Unity placement, authored by the user

- SpriteRenderer: use the same **Pixels Per Unit**, a **center pivot (0.5,0.5)**, matching local position and matching local scale for body and overlays. The 2048px sprite occupies twice the body's width and height in world space, while its artwork aligns to the smaller body at the center.
- UI Image: center both layers on the same anchor/pivot. Set the overlay RectTransform to **twice the body's width and height**. For example, a 100x100 body requires a 200x200 face or costume layer. Do not give the 1024px body and 2048px overlays identical UI dimensions.
- Keep face/costume Image or SpriteRenderer color white. Only tint the neutral body.

No Unity metadata, scene, prefab or runtime script was changed. No Unity execution, tests or output verification was performed, per user instructions.

## Reproduction and history

- `OverlayCanvas2048.cs`: offline deterministic layout exporter. Uses the actual faceless-body contour for positioning; never writes a body file.
- `BeforeOverlayCanvas2048/`: the original five PNGs used as export inputs. Always use these originals when rerunning the exporter, not the expanded outputs.
- `OverlayCanvas2048.json`: layout/export record.
- `README_BeforeOverlayCanvas2048.md`: historical notes superseded by this layout.
- Earlier SVGs, generated references and 1024px conversion utilities are historical sources, not the current overlay-layout export path.

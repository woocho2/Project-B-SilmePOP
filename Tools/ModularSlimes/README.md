# Modular slime sprites

Assets are in `Assets/4. SlimeAsset/Slime_Modular/`. Existing source atlases were retained; the user owns removal of old colored art and scene assembly.

| File | Use |
| --- | --- |
| Slime_White_Faceless.png | Common tintable gameplay body; original white faceless atlas cell exported unchanged |
| Slime_Face_Normal.png | Two black eyes and smile only, for UI overlays |
| Slime_White_Normal.png | The same common body with the separate expression composited for standalone UI Images |
| Slime_Costume_Crown.png | Crown only, King overlay |
| Slime_Costume_Angel.png | Both wings and halo only, Angel overlay |
| Slime_Costume_Horns.png | Two horns only, Devil overlay |
| Slime_Joker_Faceless.png | Original red/black Joker with hat and no expression, complete gameplay sprite replacement |

All layers use a 512 x 512 canvas, centered pivot, 512 PPU, single Sprite, alpha transparency, bilinear/clamp, no mipmaps and no texture compression. Deliberate transparent space lets every layer share one origin and scale. Do not independently trim or auto-slice accessories. Give UI body and layers matching RectTransforms; SpriteRenderers need matching transforms, PPU and pivots. Tint only the body; leave costumes white. Use Slime_White_Normal for standalone UI Images without another face on top.

SlimeSkinAssets.asset binds Body, Face, Crown, Angel, Horns, Joker and BodyUI to these standalone sprites. Entry-scene DoodleSkinBindings already registers it. SlimeController uses its existing Color/Face/Costume children: gameplay faces are hidden, costumes follow BoardSlime.Type, and Joker swaps the complete body and clears costumes. A missing Costume renderer is created before visual visibility is captured so empty cells also hide it. Basic/Merged clear role decorations. Existing player/palette tinting still colors the common body; role overlays replace the old forced role body colors. The existing merged tint is retained. Joker keeps the source red/black colors and white number text.

Change retains UI faces and defaults them to Face when no expression has been selected. UI layers share the body's RectTransform. Gameplay decorations are owned by board type rather than cosmetic selections. UIManager_Menu and UIManager_Game use the new face/costume bindings for selection actions. Standalone UI Images referencing a still-existing slime preview atlas are switched to the faced UI sprite. For scene Images directly assigned to old/deleted sprites, assign Slime_White_Normal manually. No prefab layout or scene files were edited.

Mode: built-in image_gen extraction using atlas-cell references under References/, followed by common-canvas export with System.Drawing. Export regions: face x199/y283/w116/h46, crown x144/y82/w225/h132, angel x35/y125/w446/h220, horns x140/y165/w236/h76. The UI body and expression are composited from the finished layers. Preview.png shows six assembled appearances using the white source body; runtime tinting is not shown. No Unity, compile or runtime verification was run, per the user's workflow.

## Generation prompts

### Face

Use case: background-extraction. This is an existing hand-drawn game sprite. Extract the requested layer faithfully, preserving its exact original style, colors and proportions, with real transparent alpha. No new ornament, no surrounding paper or scene. Do not add text, labels, numbers, shadow patches, white scraps or detached flecks. Output ONLY the face: the two small black oval eyes and the tiny simple U-shaped black smile from this white slime. Remove the ENTIRE cream-white body, texture and black outer silhouette. Keep the face's original spacing: the smile centered between two widely spaced eyes. No cheeks, eyebrows, white fills or new facial features. Transparent everywhere except the three black face marks. One reusable expression layer, not a complete character. Keep all three marks together in one image.

### Crown

Use case: background-extraction. This is an existing hand-drawn game sprite. Extract the requested layer faithfully, preserving its exact original style, colors and proportions, with real transparent alpha. No new ornament, no surrounding paper or scene. Do not add text, labels, numbers, shadow patches, white scraps or detached flecks. Output ONLY the existing GOLD CROWN with its red oval center jewel, three round-tipped points and curved gold base band. Remove ALL of the yellow slime body, its outline and the entire face. The crown is complete, including the curved lower band where it sat on the head. Keep its front-facing hand-drawn crayon gold colors, dark pencil contour and simple highlight. Transparent everywhere outside the crown, with no slime material left underneath.

### Angel

Use case: background-extraction. This is an existing hand-drawn game sprite. Extract the requested layer faithfully, preserving its exact original style, colors and proportions, with real transparent alpha. No new ornament, no surrounding paper or scene. Do not add text, labels, numbers, shadow patches, white scraps or detached flecks. Output ONLY the two little cream-white ANGEL WINGS and the single golden oval HALO above them. Remove ALL blue slime body, its black perimeter and the face. Keep the exact placement of the two wings relative to the halo, with an entirely transparent empty middle where the body was. Preserve each wing's white fill, thin sky-blue pencil outline, three rounded feathers, mirror symmetry and front-facing golden halo with transparent center. Complete the inner feather edges smoothly so each wing is a whole separate silhouette. All three elements together form ONE costume layer: halo centered above, left and right wings below at the sides. Keep layout proportions and leave center empty; do not compact the three pieces into a cluster. No body ghost, head silhouette, stand or linking stroke.

### Demon

Use case: background-extraction. This is an existing hand-drawn game sprite. Extract the requested layer faithfully, preserving its exact original style, colors and proportions, with real transparent alpha. No new ornament, no surrounding paper or scene. Do not add text, labels, numbers, shadow patches, white scraps or detached flecks. Output ONLY the PAIR OF SMALL RED DEMON HORNS from this slime. Remove ALL of the red slime body, its outer outline and the whole face. Preserve the left horn and right horn as two separate curved tapered red shapes with small pale highlights and thin dark hand-drawn edges. Preserve their original symmetrical spacing with a broad transparent gap between them; do not move them together. Complete the short curved base of each horn, no slime remains below. This is ONE reusable horns-only costume layer, not a whole character. Transparent everywhere except the two horns.

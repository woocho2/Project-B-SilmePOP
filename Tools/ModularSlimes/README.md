# Modular slime sprites

Assets: `Assets/4. SlimeAsset/Slime_Modular/`. Scene and prefab assembly belongs to the user; these scripts only update existing renderers.

| File | Use |
| --- | --- |
| Slime_White_Faceless.png | Common tintable gameplay body |
| Slime_Face_Normal.png | Separate eyes and smile for UI |
| Slime_White_Normal.png | Body with face for standalone UI Images |
| Slime_Costume_Crown.png | King crown overlay |
| Slime_Costume_Angel.png | Angel wings and halo overlay |
| Slime_Costume_Horns.png | Devil horns overlay |
| Slime_Costume_Joker.png | Joker hat only, shared-body overlay |
| Slime_Joker_Faceless.png | Legacy complete red/black Joker; no longer used by the gameplay skin path |

Body, face, wings and horns have 512-square canvases and 512 PPU. The fitted crown and Joker hat preserve their generated 1254-square originals and use 1254 PPU. Every layer therefore has the same one-unit canvas, centered pivot, transparency, bilinear filtering, no mipmaps and no compression. Do not independently trim accessories. Give body and costumes matching transforms; tint only the body, leaving costumes white.

DoodleLabSkin selects Body for every slime and the matching costume by BoardSlime.Type. Joker uses the Joker binding as a hat, following the same palette/selected body color and contrasting number text as ordinary slimes. Merged keeps its existing blue tint. Basic/Merged clear costumes. SlimeController only uses existing Color/Face/Costume renderers; it does not create objects or change transforms. Gameplay faces stay hidden for the number. Change handles UI expressions and costumes on existing layers.

Manual binding: assign Slime_Costume_Joker.png to the `Joker` entry in your DoodleSkinAssets asset. No DoodleSkinAssets .asset currently exists in this checkout, so no serialized binding was changed or recreated. Existing scenes retain their serialized references and layout.

Alignment_Fitted.png shows the current costumes composited on the unchanged body. Crown and Joker contact curves were revised to hug the common head. Alignment_Prompts.md records the edit prompts, sources and final asset paths. Existing asset GUIDs were preserved. Earlier Preview.png shows the legacy full-body Joker; Alignment_Current.png shows the pre-fit accessories. Both are retained as historical previews. No Unity, compilation or runtime checks were run, per the user's workflow.

## Crown contact correction

The first fit retained gaps at the crown ends. Slime_Costume_Crown.png was replaced again using a head-derived registration guide so the lower band overlaps the body across its full contact width. Crown_Contact_Corrected.png shows this corrected result on a dark background. Crown_Contact_Prompt.md records the final prompt/source and sampled contact overlap; it supersedes the crown source in Alignment_Prompts.md. The asset GUID and prefab layout remain unchanged.

## Joker contact correction

Slime_Costume_Joker.png was replaced using a head-derived contact guide, then its central V was made shallower so the body's head cannot protrude through it. The lower sides overlap the common body. Joker_Contact_Corrected.png shows the finished overlay on a dark background; Joker_Contact_Prompt.md records the prompts and final source and supersedes the Joker source in Alignment_Prompts.md. Existing GUID and manual prefab layout were retained.

## Original Joker accessory generation (superseded by the fitted version)

Mode: built-in image_gen edit/extraction. References: Slime_Joker_Faceless.png (edit target) and Slime_White_Faceless.png (positioning only). Source: C:/Users/user/.codex/generated_images/01a19996-8135-7271-9545-228715db1958/exec-ab27510b-7b7d-4499-a968-8f03fd57f74c.png. Copied byte-for-byte to Slime_Costume_Joker.png; no crop, recoloring, resizing or artwork post-processing.

Prompt:

Edit target: first image, existing faceless red-and-black Joker slime. Second image: common white slime body, positioning reference ONLY; do NOT output the body. Create ONLY its two-point red-and-charcoal jester HAT accessory, removing the ENTIRE slime body. Preserve the first image's crayon texture, simple dark pencil contour, left charcoal horn with coral-red round bell, right coral-red horn with charcoal round bell. Complete a shallow curved lower hat edge where it sat on the head, so the cap is a standalone reusable costume. Real transparent alpha everywhere else. Layout is essential: square canvas identical proportions to references; keep the hat in the UPPER part, top at 20% canvas height, left at 10%, right at 91%, lower curved edge at 42% height in center, at most 45% height at sides. Bottom 55% of canvas stays COMPLETELY TRANSPARENT, reserved for a separate common slime body and number. No slime, no face, no torso, no text, no numbers, no additional ornaments, no floor shadow. Hat must retain its original proportions, not enlarged to fill the canvas. Output a single transparent accessory layer, not an assembled slime.

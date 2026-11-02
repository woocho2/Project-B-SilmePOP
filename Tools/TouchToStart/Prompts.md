# Touch To Start image generation

Mode: built-in `image_gen`, transparent background enabled.

Reference image: `Assets/4. SlimeAsset/GameStart_Crayon.png` (style reference; unchanged).

Final asset: `Assets/4. SlimeAsset/TouchToStart_Crayon.png`.

## Original button prompt (superseded by the text-only edit below)

Use case: text-localization. Asset type: production Unity title-screen start button sprite. Input reference: the existing mint crayon GAME START button; preserve its playful wax-crayon / colored-pencil texture, chunky charcoal handwritten lettering, pale mint-green rounded capsule with a dark green sketchy outline, creamy highlights, and small warm-yellow play triangle at the left. Create a new matching button graphic with the EXACT text "Touch To Start" in Title Case: capital T in Touch, capital T in To, capital S in Start; the other letters lowercase. One clean horizontal line, clear word spaces, highly readable at 600 px UI width. The lettering should sit comfortably inside the capsule, optically centered to the right of the play triangle; no text clipping. Slight hand-drawn irregularity, thick readable strokes, no glossy 3D bevels. A wide horizontal layout, around 4:1 aspect ratio with modest transparent exterior margins. True transparent alpha outside the mint capsule; no rectangular background, no checkerboard, no extra slogan, no logo, no extra words or decorative sprites. Deliver a single isolated complete start-button sprite, not an atlas or a screenshot. Match the attached reference's soft mint, charcoal and yellow palette exactly.

## Final text-only edit

Mode: built-in `image_gen`, transparent background enabled. Edit target: the original mint-button version of `TouchToStart_Crayon.png`. The generated output replaced the PNG at the same asset path; its import GUID is retained.

Edit target: the attached Touch To Start Unity button asset. Remove the entire mint-green capsule background, every green outline, the yellow play triangle, yellow accent marks, and ALL decorative elements. Keep ONLY the exact text "Touch To Start" with the same capitalization, chunky rounded handwritten letter shapes, subtle black crayon grain, and clearly separated word spacing. The surviving lettering must be black / near-black charcoal only, with no green or yellow or other colored pixels, no colored backing visible through the letters. All empty areas and letter counters must be true transparent alpha. Tiny crayon gaps can be transparent; do not put white or colored fill behind the text. Re-center this single horizontal line of text on a wide transparent canvas and reduce the exterior margins so it is suitable as a standalone title-screen prompt. No button outline, no rectangle, no capsule, no icon, no drop shadow, no background, no checkerboard. Text must remain complete and unclipped. Output a production RGBA text-only sprite.

# Title glyph artwork

Generated with the built-in `image_gen` tool. The reference is the existing `Assets/4. SlimeAsset/SlimePop_Title_Crayon.png`. That file was inspected first and is unchanged.

The first pass corrected the wordmark to `SLIME POP!!!`, including a capital I with top and bottom bars. After the user selected manual placement of eight unique glyphs, the final pass reorganized the corrected style reference into a sprite sheet.

Final saved deliverable: `Assets/4. SlimeAsset/SlimePop_TitleLetters.png`. The generated PNG and transparent alpha are preserved without pixel edits. Sprite rectangles are trimmed and recorded in the accompanying `.meta` and `Atlas.json`.

## Final generation prompt

Production Unity sprite atlas, using the attached wordmark as strict style reference. Reorganize the lettering into EXACTLY EIGHT isolated sprites on ONE atlas, arranged in an exact regular 4-column by 2-row grid of equal square cells. Row 1 left to right: uppercase S, uppercase L, uppercase I, uppercase M. Row 2 left to right: uppercase E, uppercase P, uppercase O, exclamation mark !. No other glyphs. Each glyph centered inside its own cell with at least 12 percent completely transparent margin around all sides and equal apparent cap height. The capital I MUST have top and bottom horizontal bars, absolutely no dot. The O is uppercase at full cap height. S,L,I,M,E are mint green; P,O are coral red; ! is golden yellow. Match the thick rounded inflated crayon strokes, heavy charcoal pencil outline, dry waxy scribble hatching, creamy white glossy highlights of the reference. Exact 2:1 canvas aspect ratio suitable for eight square cells, ideally 2048x1024 or 1536x768. All gutters and background must be truly transparent alpha, NOT black, NOT a checkerboard. No grid lines, no cell boundaries, no labels, no shadows connecting neighboring glyphs, no decorative elements, no extra text. Every sprite must fit fully within its own equal-size cell. This is a sprite sheet, not a readable wordmark.

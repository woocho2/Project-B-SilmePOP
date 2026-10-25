# Map atlases grouped by purpose

Location: `Assets/Resources/DoodleLab/`.

| Atlas | Sprite cell | Sheet size | Grid | Gutter | Border (left, bottom, right, top) |
| --- | --- | --- | --- | --- | --- |
| Map_Bars.png | 512 x 128 | 1024 x 512 | 2 x 4 | 4 px | 176, 48, 96, 48 |
| Map_Mascots.png | 512 x 512 | 2048 x 1024 | 4 x 2 | at least 16 px | 0 |
| Map_MenuPapers.png | 768 x 1024 | 3072 x 2048 | 4 x 2 | at least 16 px | 0 |
| Map_Tables.png | 512 x 512 | 2048 x 1024 | 4 x 2 | 8 px | 144, 144, 144, 144 |
| Map_Panels.png | 512 x 512 | 2048 x 1024 | 4 x 2 | 8 px | 144, 144, 144, 144 |

Every sheet contains exactly 8 sprites. Reading order, left to right then top to bottom: **Basic, Spring, Summer, Autumn, Winter, Sunny, Night, Cloudy**.

Common import settings: Sprite Multiple, 100 PPU, centered pivot (0.5, 0.5), Full Rect, bilinear filter, clamp wrap, no mipmaps, no NPOT resizing, uncompressed RGBA, max texture size 4096. Sprite rectangles are the full equal-size cells. Mascots are uniformly fitted and bottom-aligned to y=496 within their cells. Paper art keeps its aspect and transparent torn holes. Existing artwork was mechanically cropped/resized/packed; no new art was generated.

Sprite names remain `Map_<theme>_Bar`, `Map_<theme>_Mascot`, `Map_<theme>_Table`, and `Map_<theme>_Menu` for the option-panel frames. The torn notebook pages have distinct `Map_<theme>_MenuPaper` names, referenced by ThemeData.MenuSprite. The legacy `_Menu` name is kept for runtime compatibility and belongs to Map_Panels, not the actual menu-paper sheet.

ThemeData and serialized scene references are migrated. Runtime scripts retain the existing sprite lookup names. The original source PNGs under `Assets/4. SlimeAsset/Map/` remain intact. The eight superseded mixed sheets are moved out of Assets into `Tools/RoleMapAtlases-Backup-v1/before/` with their original metadata, alongside all changed files. `manifest.json`, `layout.json` and `SourceHashes.json` record the exact migration.

The existing presentation guard now recognizes both `Mascot` and themed `*_Mascot` names when hiding the legacy oversized decorations, while keeping the header mascot visible. This covers entering the game after choosing a theme in the menu.

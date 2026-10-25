# Illustrated map themes

Basic: sprouts. Spring: blossoms/butterflies. Summer: leafy branches/dew. Autumn: maple leaves. Winter: snowflakes. Sunny: sunflower/sunshine. Night: crescent moon/stars. Cloudy: cloud/rain.

All 32 existing PNGs in Assets/4. SlimeAsset/Map are replaced, preserving their dimensions and original .meta files. Eight new Resources/DoodleLab/Map_*.png atlases provide four named sprites each (Menu, Table, Mascot, Bar) with nine-slice borders on panels. Eight existing ThemeData assets reference those sprites. Existing scenes keep their ThemeData references.

Runtime presentation uses theme illustrations on game boards, options/menu frames, timer borders and map buttons; a matching mascot is shown in the header and menu. Artwork is not double-tinted. The notebook background receives the existing theme tint. Corner art has extra board margin, preserving the central number area. The menu mascot sits beside the exit button.

Validation: Unity 6000.3.9f1 in an isolated project copy, 8 atlas imports/32 sprite references, all ThemeData bindings, 170 slimes, all eight pause/options/resume cycles, no duplicate timer ornaments, no double tint, and Night selection preserved when returning to menu. Final menu placement was rendered separately. C# compiled with two existing unused-field warnings; no game runtime errors. See reports and captured PNGs. A separate manual drag-play session was not part of this check.

Full built-in image_gen prompts are in SourcePrompts.md. Original motifs are shown in OriginalThemes.png. This update's previous files are in ../MapThemes-Backup-v1/before/ with the same relative paths; new resource files have a null Before hash in manifest.json. Previous round-slime artwork remains unchanged.
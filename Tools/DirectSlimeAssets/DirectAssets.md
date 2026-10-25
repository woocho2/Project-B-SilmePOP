# SlimeAsset direct sprite bindings

The game uses serialized sprite references under `Assets/4. SlimeAsset/`. There is no runtime Resources lookup for the skin.

The eight ThemeData assets reference the original files in Map/Menu, Map/Table, Map/Slime and Map/Bar. ThemeData now exposes BarSprite and OptionPanelSprite as well. Original bar/table import settings include nine-slice borders; PNG contents and existing sprite IDs are unchanged.

`Assets/4. SlimeAsset/SlimeSkinAssets.asset` holds the runtime aliases for generic UI sprites and theme buttons. The three entry scenes each contain a SlimeSkinBindings component that registers this asset before other Awake methods. Original body, face, costumes, icons, logo and background files are used wherever matching originals exist.

The purpose-specific atlases and supplementary frame/icon sheets were relocated together from Resources/DoodleLab to `Assets/4. SlimeAsset/Atlases/`, preserving their GUIDs. They remain available for reuse. Supplemental frames and icons without individual source files are referenced directly from these sheets. SoundData loading is unchanged.

All original PNG contents remain unchanged. Original menus include the approved black outlines. Previous serialized files and import settings are in `Tools/DirectSlimeAssets-Backup-v1/before/`. To restore the atlas location, move the entire Atlases directory and its .meta file back to Assets/Resources/DoodleLab, then restore the other backed-up files.

Unity verification covers all three scene entry points, eight themes, original PNG paths on live UI/renderers, timer, options, pause/resume, 170 board slimes, face/costume selection, and absence of runtime errors. Captures and validation are stored beside this document.

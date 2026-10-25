# Torn notebook menu paper

The eight original PNGs at Assets/4. SlimeAsset/Map/Menu are replaced with torn notebook sheets: irregular paper edges, transparent torn binder holes, faint blue ruling, a pale coral margin, and small themed corner doodles. Every original PNG dimension, active slice, GUID, sprite ID and metadata file is preserved.

Each ThemeData.MenuSprite points directly to its original Menu PNG. The runtime menu frame override is removed, and menu backgrounds use a Simple Image so the whole paper artwork and holes render together. Basic is initialized when no theme has been chosen. Main and pause menus keep their dedicated paper through option-panel round trips and scene transitions. The main logo fits inside the paper without covering the corner drawing; pause labels are centered within the buttons.

Validation: Unity 6000.3.9f1 in an isolated project copy. All eight menu asset references, initial Basic, eight menu/option round trips, Night selection into the game, the pause paper, and resume with 170 slimes passed. C# compilation passed with two pre-existing unused-field warnings; no game runtime errors. See Validation.txt and screen captures.

Built-in image_gen mode and full prompts are in SourcePrompts.md. Prior versions are backed up at ../TornMenuPaper-Backup-v1/before/ with identical relative paths. The game board, timer and theme selection still use their dedicated theme assets.
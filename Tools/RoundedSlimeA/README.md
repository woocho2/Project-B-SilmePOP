# Rounded slime A

The approved large, tip-free A body is now used by the live Resources atlas and the original `Assets/4. SlimeAsset/Slime/` PNG files. Body, face, crown, devil and angel textures retain their existing dimensions, GUIDs, sprite IDs, and metadata. Other atlas icons and the two empty selections are unchanged.

The default palette uses light leaf-green shades. Custom body colors still work. Live digits are 0.48 bold text (previously 0.34), positioned in the belly. Dark bodies use white digits; bright bodies use black digits. The face is low and separate; costumes leave the digit area clear. No badge was added.

Validation was run in Unity 6000.3.9f1 using an isolated project copy: 170 active slimes, digits 1-9, green default, red angel without face, blue devil with face, yellow crown with face, mix and options preview. See Validation.txt and the PNG captures. C# compilation passed with only two existing unused-field warnings. Automated checks do not replace a manual drag-play session.

Previous versions: ../RoundedSlimeA-Backup-v1/before/ with the same relative paths. Existing metadata hashes are recorded in PreservedMetaHashes.json. Artwork mode and prompts are in SourcePrompts.md.
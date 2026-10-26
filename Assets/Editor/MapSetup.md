# Level 01 map setup

Open the project in Unity 6.3 LTS and select `Tools > Maps > Build Level 01`.

The command creates:

- `Assets/Scenes/Scene01/Level01.unity`
- Tilemap layers named `Ground`, `Platforms`, `Background`, `Decorations`, and `Foreground`
- Composite collision geometry for `Ground` and `Platforms`
- An island tile palette and reusable tile assets
- Separate prefabs for the rolling barrel, collapsing platform, shooter totem, and projectile

The command is idempotent and can be run again to rebuild the generated map assets.

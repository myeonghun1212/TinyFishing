# Tiny Fishing Demo

Open `Assets/Project/Scenes/SampleScene.unity` and enter Play Mode. The low-poly background,
dock, props, camera, and lighting are directly editable under the scene's `Environment`
hierarchy. The runtime bootstrap only creates gameplay services, fish variants, and the HUD.

To rebuild the editable placeholder background, use
`Tools > NAN Fishing > Build Editable Scene`. This replaces only the `Environment` hierarchy
and preserves unrelated scene objects.

The Canvas and all state panels are also stored directly in the scene. Use
`Tools > NAN Fishing > Build Scene UI` to rebuild the placeholder UI. Runtime state changes
toggle `StartPanel`, `GameplayPanel`, and `ResultPanel` with `SetActive`.

## Editor controls

- Enter: cast
- Space or left mouse: reel
- Left/Right arrows or A/D: follow the fish
- Mouse swipe upward: touch fallback cast

## Android controls

- Hold the phone still for 0.5 seconds to calibrate.
- Move the phone back and swing forward to cast.
- Hold the screen to reel and tilt left/right to follow the fish.
- Use `RECALIBRATE` to reset the neutral tilt.

## Art handoff

Production assets should live under `Art/Characters/Fish`, `Art/Environment`, `Art/Props`,
`Art/Materials`, and `Art/VFX`. Replace the procedural primitives through
`FishDefinition.prefab` and scene prefabs; gameplay code must not depend on model hierarchy.
Use meters, forward +Z, bottom-center pivots for props, and one shared URP material palette
where possible.

Balance values belong in `GameBalanceConfig`; fish-specific values belong in
`FishDefinition`. The runtime-created data makes the demo immediately playable, while asset
files created from those ScriptableObject types can replace it without code changes.

# Changelog

## 1.0.0

The first release.

- Field of View slider (50-120, default 70) in Settings -> Graphics.
- The main menu always keeps the vanilla FOV.
- The helmet HUD is drawn by a separate camera at the vanilla FOV while your FOV is changed, so it does not stretch. World screens (TV, shop) are not affected.
- Switched off automatically while VR (CWVR) is on.
- Config: `Enabled` and `FixHud` switches; the HUD fix turns itself off after repeated errors.
- Public API for other mods (CustomFovApi): suspend the mod, exclude your own HUD objects, read the FOV. See README.

# Custom FOV

**A Field of View slider for Content Warning**, right in the game's own Settings menu.

> Client-side only: you don't need the other players to have it, it works in vanilla lobbies.

## Features

- **Settings -> Graphics -> Field of View** (RU: «Поле зрения (FOV)»), from 50 to 120, default 70 (the vanilla value). It is saved like every other setting and works with a controller too (steps of 5).
- **The HUD stays in place.** The helmet HUD is a tilted 3D panel, so a wide FOV would stretch it off the screen. While your FOV is not 70, the HUD is drawn by a separate camera that keeps the vanilla FOV. TVs, shop screens and other world screens are not touched.
- **Main menu** always uses the vanilla FOV.
- **VR:** switched off while VR (CWVR) is on, since VR controls the camera.

## Config

`BepInEx/config/com.ddk.cwfov.cfg` (appears after the first launch):

- `Enabled` - turn the whole mod off (vanilla FOV and HUD) without removing it.
- `FixHud` - turn off only the separate HUD camera. Use it if the mod conflicts with another camera / HUD mod (the HUD will stretch at a wide FOV again).

If the HUD fix fails three times in a row, it turns itself off for the rest of the session.

## For mod authors

Custom FOV has a small public API so your mod can play nicely with it. You do not need to depend on it hard: use a soft dependency, or call it through reflection.

```csharp
// 1. Your mod needs the camera for itself (a free cam, a cutscene, VR-like view...):
//    switch Custom FOV off while you need it. Use your own unique key.
CustomFovApi.SetSuspended("your.mod.guid", true);   // off
CustomFovApi.SetSuspended("your.mod.guid", false);  // on again

// 2. You put your own UI under the helmet HUD (HelmetUI) and it must keep its own layer/camera:
CustomFovApi.ExcludeFromHud(myTransform);           // Custom FOV leaves it and its children alone

// 3. Read the FOV the player chose:
float fov = CustomFovApi.CurrentFov;
```

Without a reference to the DLL (works even if Custom FOV is not installed):

```csharp
var api = System.Type.GetType("CustomFovApi, CWFov");
api?.GetMethod("SetSuspended")?.Invoke(null, new object[] { "your.mod.guid", true });
```

Add `[BepInDependency("com.ddk.cwfov", BepInDependency.DependencyFlags.SoftDependency)]` to your plugin if you reference the DLL.
## Для русскоязычных

Ползунок поля зрения в настройках игры (Графика). Нужен только вам, у остальных в лобби ставить ничего не надо. Интерфейс шлема не растягивается на большом FOV. В главном меню FOV обычный. При включённом VR (CWVR) мод отключается.

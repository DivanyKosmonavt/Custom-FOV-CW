using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Public hooks for other mods. Everything here is safe to call at any time, from any thread-of-Unity,
/// and does nothing harmful if Custom FOV is not installed (use a soft dependency or reflection, see README).
/// </summary>
public static class CustomFovApi
{
    private static readonly HashSet<string> _suspenders = new HashSet<string>();
    private static readonly HashSet<Transform> _excluded = new HashSet<Transform>();

    /// <summary>True while at least one mod asked Custom FOV to stay out of the way.</summary>
    public static bool Suspended => _suspenders.Count > 0;

    /// <summary>
    /// Switch Custom FOV off (vanilla FOV, HUD back to normal) while your mod needs the camera for itself.
    /// Use your own unique key (for example your plugin GUID). Call again with false to give it back.
    /// </summary>
    public static void SetSuspended(string key, bool suspended)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (suspended) _suspenders.Add(key); else _suspenders.Remove(key);
    }

    /// <summary>
    /// Tell Custom FOV not to touch this object and everything under it. Use it for your own UI that you put
    /// inside the helmet HUD (HelmetUI) and that must keep its own layer or camera.
    /// </summary>
    public static void ExcludeFromHud(Transform t)
    {
        if (t == null) return;
        _excluded.Add(t);
        HudFovPatch.Release(t);
    }

    /// <summary>The FOV the player chose in Settings (vanilla value when Custom FOV is off).</summary>
    public static float CurrentFov
    {
        get
        {
            try
            {
                var s = GameHandler.Instance?.SettingsHandler?.GetSetting<FovSetting>();
                return s != null && !FovPlugin.Off ? s.Value : FovSetting.VanillaFov;
            }
            catch { return FovSetting.VanillaFov; }
        }
    }

    internal static bool IsExcluded(Transform t)
    {
        if (_excluded.Count == 0) return false;
        for (; t != null; t = t.parent)
            if (_excluded.Contains(t)) return true;
        return false;
    }
}

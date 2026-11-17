using BepInEx;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Settings;

// Field of view slider in the game's own Settings menu (Graphics tab).
// Client-side only, so it is vanilla compatible.
[ContentWarningPlugin(FovPlugin.Guid, FovPlugin.Version, true)]
[BepInPlugin(FovPlugin.Guid, "Custom FOV", FovPlugin.Version)]
public class FovPlugin : BaseUnityPlugin
{
    public const string Guid = "com.ddk.cwfov";
    public const string Version = "1.0.0";

    // VR is "on" when CWVR is installed and its DisableVR option is off (or an XR device is running).
    // Then our FOV/HUD tweaks stay off, because VR controls the camera.
    public static bool VrPresent
    {
        get
        {
            try
            {
                if (UnityEngine.XR.XRSettings.isDeviceActive) return true;
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("io.daxcess.cwvr", out var info)) return false;
                if (info.Instance != null && info.Instance.Config.TryGetEntry<bool>("General", "DisableVR", out var e)) return !e.Value;
                return true;
            }
            catch { return false; }
        }
    }

    internal static BepInEx.Configuration.ConfigEntry<bool> CfgEnabled, CfgFixHud;

    // Set when the HUD fix failed repeatedly (e.g. another mod conflicts): we stop touching cameras and UI.
    internal static bool HudBroken;

    internal static bool Off => CfgEnabled != null && !CfgEnabled.Value || CustomFovApi.Suspended || VrPresent;

    private void Awake()
    {
        CfgEnabled = Config.Bind("General", "Enabled", true, "Turn the whole mod off (vanilla FOV and HUD) without removing it. Useful if it conflicts with another mod.");
        CfgFixHud = Config.Bind("General", "FixHud", true, "Draw the helmet HUD with a separate camera so it does not stretch at a wide FOV. Turn off if it conflicts with another camera/UI mod.");
        new Harmony(Guid).PatchAll(typeof(FovPlugin).Assembly);
        Logger.LogInfo("Custom FOV " + Version + " loaded.");
    }
}

// Picked up by SettingsHandler thanks to the ContentWarningSetting attribute.
[ContentWarningSetting]
public class FovSetting : FloatSetting, IExposedSetting
{
    public const float VanillaFov = 70f;

    public FovSetting()
    {
        SliderAmount = 5f;
    }

    public override void ApplyValue() { }

    protected override float GetDefaultValue() => VanillaFov;

    protected override float2 GetMinMaxValue() => new float2(50f, 120f);

    public override string Expose(float result) => Mathf.RoundToInt(result).ToString();

    public SettingCategory GetSettingCategory() => SettingCategory.Graphics;

    public string GetDisplayName()
    {
        bool ru = false;
        try
        {
            var loc = UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocale;
            ru = loc != null ? loc.Identifier.Code.StartsWith("ru") : Application.systemLanguage == SystemLanguage.Russian;
        }
        catch { }
        return ru ? "Поле зрения (FOV)" : "Field of View";
    }
}

[HarmonyPatch(typeof(MainCamera), "GetFov")]
internal static class MainCameraFovPatch
{
    // The game computes baseFOV * zoomFactor (terminal zoom etc.); we just swap the base.
    private static void Postfix(MainCamera __instance, ref float __result)
    {
        try
        {
            if (FovPlugin.Off) return;
            if (!Player.localPlayer) return; // main menu keeps the vanilla FOV
            var s = GameHandler.Instance?.SettingsHandler?.GetSetting<FovSetting>();
            if (s == null) return;
            float baseFov = Traverse.Create(__instance).Field("baseFOV").GetValue<float>();
            if (baseFov > 1f) __result = __result / baseFov * s.Value;
        }
        catch { }
    }
}

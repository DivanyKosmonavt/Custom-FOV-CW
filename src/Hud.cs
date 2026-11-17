using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// The HUD (HelmetUI) is a Screen Space - Camera canvas on the main camera, with tilted 3D plates.
// A wider main-camera FOV distorts it. So while the FOV is changed we render the UI layer with a
// separate overlay camera that keeps the vanilla FOV, and hide that layer from the main camera.
[HarmonyPatch(typeof(UserInterface), "LateUpdate")]
internal static class HudFovPatch
{
    private const int OrigLayer = 5;
    private static int _layer = -1;
    private static readonly System.Collections.Generic.Dictionary<Transform, int> _saved = new System.Collections.Generic.Dictionary<Transform, int>();
    private static Camera _hud;
    private static Canvas[] _canvases;
    private static UserInterface _inst;

    private static void Postfix(UserInterface __instance)
    {
        try
        {
            var mc = MainCamera.instance;
            if (mc == null || mc.Cam == null) return;
            var main = mc.Cam;
            var s = GameHandler.Instance?.SettingsHandler?.GetSetting<FovSetting>();
            float baseFov = Traverse.Create(mc).Field("baseFOV").GetValue<float>();
            bool active = !FovPlugin.Off && FovPlugin.CfgFixHud.Value && !FovPlugin.HudBroken && Player.localPlayer && s != null && baseFov > 1f && Mathf.Abs(s.Value - baseFov) > 0.5f;

            if (_inst != __instance) { _inst = __instance; _canvases = __instance.GetComponentsInChildren<Canvas>(true); }

            if (active)
            {
                if (_hud != null && _hud.transform.parent != main.transform) { MoveHud(false); Object.Destroy(_hud.gameObject); _hud = null; }
                if (_hud == null) Create(main);
                if (_hud == null) return;
                _hud.fieldOfView = baseFov;
                _hud.enabled = true;
                if (_layer < 0) _layer = FreeLayer();
                _hud.cullingMask = 1 << _layer;
                main.cullingMask &= ~(1 << _layer);
                if (Time.frameCount % 5 == 0) MoveHud(true);
                foreach (var c in _canvases) if (c != null && c.name == "HelmetUI" && c.renderMode == RenderMode.ScreenSpaceCamera) c.worldCamera = _hud;
            }
            else if (_hud != null && _hud.enabled)
            {
                _hud.enabled = false;
                MoveHud(false);
                foreach (var c in _canvases) if (c != null && c.name == "HelmetUI" && c.renderMode == RenderMode.ScreenSpaceCamera) c.worldCamera = main;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("CWFov HUD: " + e);
            if (++_fails >= 3) Fail();
        }
    }

    private static int _fails;

    // Give an object (and its children) back its original layer; used by CustomFovApi.ExcludeFromHud.
    internal static void Release(Transform root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (_saved.TryGetValue(t, out var l)) { t.gameObject.layer = l; _saved.Remove(t); }
    }

    // Circuit breaker: after repeated errors, put everything back and never touch cameras/UI again this session.
    private static void Fail()
    {
        FovPlugin.HudBroken = true;
        try
        {
            MoveHud(false);
            var mc = MainCamera.instance;
            if (_canvases != null && mc != null)
                foreach (var c in _canvases) if (c != null && c.name == "HelmetUI" && c.renderMode == RenderMode.ScreenSpaceCamera) c.worldCamera = mc.Cam;
            if (_hud != null) _hud.enabled = false;
        }
        catch { }
        Debug.LogWarning("CWFov: HUD fix disabled after repeated errors (HUD may look stretched at wide FOV).");
    }

    private static int FreeLayer()
    {
        for (int i = 31; i > 7; i--) if (string.IsNullOrEmpty(LayerMask.LayerToName(i))) return i;
        return 31;
    }

    // Only the HelmetUI canvas hierarchy goes to the private layer; world-space screens (TV, shop) stay on layer 5.
    private static void MoveHud(bool toHud)
    {
        if (_canvases == null) return;
        foreach (var c in _canvases)
        {
            if (c == null || c.renderMode != RenderMode.ScreenSpaceCamera || c.name != "HelmetUI") continue;
            foreach (var t in c.GetComponentsInChildren<Transform>(true))
            {
                if (toHud)
                {
                    if (t.gameObject.layer == _layer || CustomFovApi.IsExcluded(t)) continue;
                    _saved[t] = t.gameObject.layer;
                    t.gameObject.layer = _layer;
                }
                else if (_saved.TryGetValue(t, out var l)) t.gameObject.layer = l;
            }
        }
        if (!toHud) _saved.Clear();
    }

    private static void Create(Camera main)
    {
        var go = new GameObject("CWFov_HudCamera");
        go.transform.SetParent(main.transform, false);
        _hud = go.AddComponent<Camera>();
        _hud.CopyFrom(main);
        _hud.cullingMask = 1 << OrigLayer;
        _hud.clearFlags = CameraClearFlags.Depth;
        _hud.depth = main.depth + 1;
        _hud.allowHDR = false;
        _hud.allowMSAA = false;
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        data.renderType = CameraRenderType.Overlay;
        data.renderPostProcessing = false;
        var mainData = main.GetUniversalAdditionalCameraData();
        mainData.cameraStack.Add(_hud);
    }
}

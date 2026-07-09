using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(FastTale.FastTaleMod), "FastTale", "1.0.1", "CircuitLord")]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace FastTale
{
    // Performance mod for A Township Tale. F2 opens a small desktop settings panel:
    //   - MSAA: game defaults to 2x; forced off unless toggled on.
    //   - Overview Camera: the stock flat non-VR main camera. In VR its full-scene
    //     render is discarded (the desktop window is the XR eye view), so it's wasted
    //     work. Disabling its Camera stops that render; its audio listener lives on the
    //     same GameObject and is left untouched.
    // Both default off; toggles persist via MelonPreferences ([FastTale]).
    public class FastTaleMod : MelonMod
    {
        private const int MsaaOn = 2; // game default (2x MSAA)
        private const int MsaaOff = 0;
        private const string OverviewName = "Overview Camera";

        private MelonPreferences_Category _cfg;
        private MelonPreferences_Entry<bool> _cfgMsaa;
        private MelonPreferences_Entry<bool> _cfgOverview;

        private bool _msaaEnabled;     // off
        private bool _overviewEnabled; // off = Overview Camera disabled
        private bool _guiOpen;

        private Camera _overviewCam;
        private int _searchCooldown;
        private Rect _winRect = new Rect(24, 24, 240, 0);

        public override void OnInitializeMelon()
        {
            _cfg = MelonPreferences.CreateCategory("FastTale");
            _cfgMsaa = _cfg.CreateEntry("Msaa", false, description: "MSAA on (2x) vs off");
            _cfgOverview = _cfg.CreateEntry("OverviewCamera", false, description: "Render the stock flat Overview Camera (wasted in VR)");
            _msaaEnabled = _cfgMsaa.Value;
            _overviewEnabled = _cfgOverview.Value;

            QualitySettings.antiAliasing = _msaaEnabled ? MsaaOn : MsaaOff;
            LoggerInstance.Msg($"FastTale ready. F2 = settings. MSAA={(_msaaEnabled ? "on" : "off")}, Overview Camera={(_overviewEnabled ? "on" : "off")}.");
        }

        public override void OnUpdate()
        {
            // Re-assert MSAA each frame: the game's quality controller can flip it back.
            int want = _msaaEnabled ? MsaaOn : MsaaOff;
            if (QualitySettings.antiAliasing != want)
                QualitySettings.antiAliasing = want;

            ApplyOverview();

            var kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame)
                _guiOpen = !_guiOpen;
        }

        // Find the Overview Camera by name (it spawns after world load) and force its
        // enabled state. Cache it so we keep control after disabling it — a disabled
        // camera drops out of Camera.allCameras. Skip stereo cameras: that's the HMD.
        private void ApplyOverview()
        {
            if (_overviewCam == null)
            {
                if (--_searchCooldown > 0)
                    return;
                _searchCooldown = 30;
                foreach (var c in Camera.allCameras)
                {
                    if (c != null && c.name == OverviewName && !c.stereoEnabled)
                    {
                        _overviewCam = c;
                        break;
                    }
                }
                if (_overviewCam == null)
                    return;
            }

            if (_overviewCam.enabled != _overviewEnabled)
                _overviewCam.enabled = _overviewEnabled;
        }

        public override void OnGUI()
        {
            if (_guiOpen)
                _winRect = GUILayout.Window(0xFA57A1E, _winRect, DrawWindow, "FastTale");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4);

            bool msaa = GUILayout.Toggle(_msaaEnabled, _msaaEnabled ? " MSAA: ON (2x)" : " MSAA: OFF");
            if (msaa != _msaaEnabled)
            {
                _msaaEnabled = _cfgMsaa.Value = msaa;
                MelonPreferences.Save();
            }

            bool overview = GUILayout.Toggle(_overviewEnabled, _overviewEnabled ? " Overview Camera: ON" : " Overview Camera: OFF");
            if (overview != _overviewEnabled)
            {
                _overviewEnabled = _cfgOverview.Value = overview;
                MelonPreferences.Save();
            }

            GUILayout.Space(6);
            GUILayout.Label("F2 to close");
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}

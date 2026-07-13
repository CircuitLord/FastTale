using System.Collections.Generic;
using Alta.Chunks;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;

[assembly: MelonInfo(typeof(FastTale.FastTaleMod), "FastTale", "1.0.3", "CircuitLord")]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace FastTale
{
    // perf mod, F2 opens the settings panel
    public class FastTaleMod : MelonMod
    {
        private const int MsaaOn = 2; // game default (2x MSAA)
        private const int MsaaOff = 0;
        private const string OverviewName = "Overview Camera";

        private MelonPreferences_Category _cfg;
        private MelonPreferences_Entry<bool> _cfgMsaa;
        private MelonPreferences_Entry<bool> _cfgOverview;
        private MelonPreferences_Entry<bool> _cfgGrass;
        private MelonPreferences_Entry<bool> _cfgBloom;

        private bool _msaaEnabled;     // off
        private bool _overviewEnabled; // off = Overview Camera disabled
        private bool _guiOpen;

        private static bool _grassEnabled = true;
        private static bool _bloomEnabled = true;
        private static readonly HashSet<GameObject> GrassObjects = new HashSet<GameObject>();
        private static readonly List<PostProcessVolume> Volumes = new List<PostProcessVolume>();
        // original Bloom.active per profile so re-enabling doesn't force bloom where it was off
        private static readonly Dictionary<PostProcessProfile, bool> BloomOriginal = new Dictionary<PostProcessProfile, bool>();

        private Camera _overviewCam;
        private int _searchCooldown;
        private Rect _winRect = new Rect(24, 24, 240, 0);

        public override void OnInitializeMelon()
        {
            _cfg = MelonPreferences.CreateCategory("FastTale");
            _cfgMsaa = _cfg.CreateEntry("Msaa", false, description: "MSAA on (2x) vs off");
            _cfgOverview = _cfg.CreateEntry("OverviewCamera", false, description: "Render the stock flat Overview Camera (wasted in VR)");
            _cfgGrass = _cfg.CreateEntry("Grass", true, description: "Render the small grass tuft meshes");
            _cfgBloom = _cfg.CreateEntry("Bloom", true, description: "Bloom post processing");
            _msaaEnabled = _cfgMsaa.Value;
            _overviewEnabled = _cfgOverview.Value;
            _grassEnabled = _cfgGrass.Value;
            _bloomEnabled = _cfgBloom.Value;

            HarmonyInstance.Patch(
                AccessTools.PropertySetter(typeof(ChunkPrefabPointer.PointerInstance), "Spawned"),
                postfix: new HarmonyMethod(typeof(FastTaleMod), nameof(OnPointerSpawned)));
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(PostProcessVolume), "OnEnable"),
                postfix: new HarmonyMethod(typeof(FastTaleMod), nameof(OnVolumeEnable)));

            QualitySettings.antiAliasing = _msaaEnabled ? MsaaOn : MsaaOff;
            LoggerInstance.Msg("FastTale ready. F2 = settings.");
        }

        public override void OnUpdate()
        {
            // re-assert each frame, the game's quality controller can flip it back
            int want = _msaaEnabled ? MsaaOn : MsaaOff;
            if (QualitySettings.antiAliasing != want)
                QualitySettings.antiAliasing = want;

            ApplyOverview();

            var kb = Keyboard.current;
            if (kb != null && kb.f2Key.wasPressedThisFrame)
                _guiOpen = !_guiOpen;
        }

        // overview cam is the flat desktop camera, wasted render in VR
        // cache it since a disabled camera drops out of Camera.allCameras, skip stereo cams (HMD)
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

        // grass tufts are baked into chunk prefabs under known group names
        // when grass is on (default) we skip entirely so there's zero per-spawn cost
        private static void OnPointerSpawned(ChunkPrefabPointer.PointerInstance __instance)
        {
            if (_grassEnabled)
                return;
            GameObject root = __instance.Spawned;
            if (root == null)
                return;
            CollectGrass(root.transform);
        }

        private static void CollectGrass(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name;
                if (!name.Contains("Grass LO Group") && !name.Contains("_Grass_"))
                    continue;
                GrassObjects.Add(t.gameObject);
                t.gameObject.SetActive(false);
            }
        }

        private static void ApplyGrass()
        {
            if (_grassEnabled)
            {
                // patch was tracking while off, re-enable everything and reset
                GrassObjects.RemoveWhere(go => go == null);
                foreach (var go in GrassObjects)
                    go.SetActive(true);
                GrassObjects.Clear();
                return;
            }

            // turning off, patch skipped already-spawned chunks so scan loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    CollectGrass(root.transform);
            }
        }

        private static void OnVolumeEnable(PostProcessVolume __instance)
        {
            if (!Volumes.Contains(__instance))
                Volumes.Add(__instance);
            if (!_bloomEnabled)
                SetBloom(__instance, false);
        }

        private static void SetBloom(PostProcessVolume volume, bool on)
        {
            // don't touch .profile unless already instantiated, the getter clones it
            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
            if (profile == null || !profile.TryGetSettings<Bloom>(out var bloom))
                return;
            if (!BloomOriginal.ContainsKey(profile))
                BloomOriginal[profile] = bloom.active;
            bloom.active = on && BloomOriginal[profile];
        }

        private static void ApplyBloom()
        {
            for (int i = Volumes.Count - 1; i >= 0; i--)
            {
                var volume = Volumes[i];
                if (volume == null)
                    Volumes.RemoveAt(i);
                else
                    SetBloom(volume, _bloomEnabled);
            }
        }

        public override void OnGUI()
        {
            if (_guiOpen)
                _winRect = GUILayout.Window(0xFA57A1E, _winRect, DrawWindow, "FastTale");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(4);
            GUILayout.Label("Performance Tweaks (default values best)");
            
            bool overview = GUILayout.Toggle(_overviewEnabled, _overviewEnabled ? " Overview Camera: ON" : " Overview Camera: OFF");
            if (overview != _overviewEnabled)
            {
                _overviewEnabled = _cfgOverview.Value = overview;
                MelonPreferences.Save();
            }

            GUILayout.Space(8);
            GUILayout.Label("Graphical Settings");
            
            bool msaa = GUILayout.Toggle(_msaaEnabled, _msaaEnabled ? " MSAA: ON (2x)" : " MSAA: OFF");
            if (msaa != _msaaEnabled)
            {
                _msaaEnabled = _cfgMsaa.Value = msaa;
                MelonPreferences.Save();
            }

            bool grass = GUILayout.Toggle(_grassEnabled, _grassEnabled ? " Grass: ON" : " Grass: OFF");
            if (grass != _grassEnabled)
            {
                _grassEnabled = _cfgGrass.Value = grass;
                MelonPreferences.Save();
                ApplyGrass();
            }

            bool bloom = GUILayout.Toggle(_bloomEnabled, _bloomEnabled ? " Bloom: ON" : " Bloom: OFF");
            if (bloom != _bloomEnabled)
            {
                _bloomEnabled = _cfgBloom.Value = bloom;
                MelonPreferences.Save();
                ApplyBloom();
            }

            GUILayout.Space(6);
            GUILayout.Label("F2 to close");
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}

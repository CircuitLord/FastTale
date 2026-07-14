using System.Collections.Generic;
using Alta.Chunks;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[assembly: MelonInfo(typeof(FastTale.FastTaleMod), "FastTale", "1.0.3", "CircuitLord")]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace FastTale
{
    // perf mod, configured through the mods menu (F10)
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

        private static bool _grassEnabled = true;
        private static bool _bloomEnabled = true;
        private static readonly HashSet<GameObject> GrassObjects = new HashSet<GameObject>();
        private static readonly List<PostProcessVolume> Volumes = new List<PostProcessVolume>();
        // original Bloom.active per profile so re-enabling doesn't force bloom where it was off
        private static readonly Dictionary<PostProcessProfile, bool> BloomOriginal = new Dictionary<PostProcessProfile, bool>();

        private Camera _overviewCam;
        private int _searchCooldown;

        public override void OnInitializeMelon()
        {
            _cfg = MelonPreferences.CreateCategory("FastTale", "Graphical Settings");
            _cfgMsaa = _cfg.CreateEntry("Msaa", false, description: "MSAA on (2x) vs off");
            _cfgGrass = _cfg.CreateEntry("Grass", true, description: "Render the small grass tuft meshes");
            _cfgBloom = _cfg.CreateEntry("Bloom", true, description: "Bloom post processing");

            var advanced = MelonPreferences.CreateCategory("FastTale.Advanced", "Advanced Settings");
            _cfgOverview = advanced.CreateEntry("OverviewCamera", false, description: "Render the stock flat Overview Camera (wasted in VR)");
            _grassEnabled = _cfgGrass.Value;
            _bloomEnabled = _cfgBloom.Value;

            // grass/bloom need re-apply work on change (mods menu edits fire these), msaa/overview re-assert every frame
            _cfgGrass.OnEntryValueChanged.Subscribe((_, on) => { _grassEnabled = on; ApplyGrass(); });
            _cfgBloom.OnEntryValueChanged.Subscribe((_, on) =>
            {
                _bloomEnabled = on;
                ApplyBloom();
                LoggerInstance.Msg($"bloom -> {on} ({Volumes.Count} volumes)");
            });

            HarmonyInstance.Patch(
                AccessTools.PropertySetter(typeof(ChunkPrefabPointer.PointerInstance), "Spawned"),
                postfix: new HarmonyMethod(typeof(FastTaleMod), nameof(OnPointerSpawned)));
            HarmonyInstance.Patch(
                AccessTools.Method(typeof(PostProcessVolume), "OnEnable"),
                postfix: new HarmonyMethod(typeof(FastTaleMod), nameof(OnVolumeEnable)));

            QualitySettings.antiAliasing = _cfgMsaa.Value ? MsaaOn : MsaaOff;
            LoggerInstance.Msg("FastTale ready. Settings in the mods menu (F10).");
        }

        public override void OnUpdate()
        {
            // re-assert each frame, the game's quality controller can flip it back
            int want = _cfgMsaa.Value ? MsaaOn : MsaaOff;
            if (QualitySettings.antiAliasing != want)
                QualitySettings.antiAliasing = want;

            ApplyOverview();
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

            if (_overviewCam.enabled != _cfgOverview.Value)
                _overviewCam.enabled = _cfgOverview.Value;
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

            // key originals by the shared profile: runtime clones appear after we already
            // disabled bloom, and recording a clone's state as "original" locks bloom off
            var key = volume.sharedProfile != null ? volume.sharedProfile : profile;
            if (!BloomOriginal.ContainsKey(key))
            {
                bool original = key != profile && key.TryGetSettings<Bloom>(out var sharedBloom)
                    ? sharedBloom.active
                    : bloom.active;
                BloomOriginal[key] = original;
            }
            bloom.active = on && BloomOriginal[key];
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
    }
}

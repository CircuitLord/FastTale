using MelonLoader;
using UnityEngine;
using UnityEngine.InputSystem;

[assembly: MelonInfo(typeof(FastTale.FastTaleMod), "FastTale", "1.0.0", "CircuitLord")]
[assembly: MelonGame("Alta", "A Township Tale")]

namespace FastTale
{
    /// <summary>
    /// Performance mod for A Township Tale. Toggles MSAA with F2: the game
    /// defaults to 2x via UnityQualitySettingsController.Initialize(); this mod
    /// starts with MSAA off and flips QualitySettings.antiAliasing between 0 and
    /// that default.
    /// </summary>
    public class FastTaleMod : MelonMod
    {
        private const int Off = 0;
        private const int On = 2; // game default (2x MSAA)

        private bool _enabled;

        public override void OnInitializeMelon()
        {
            _enabled = false;
            QualitySettings.antiAliasing = Off;
            LoggerInstance.Msg($"MSAA ready (F2). Default: {Describe(_enabled)} (antiAliasing={QualitySettings.antiAliasing})");
        }

        public override void OnUpdate()
        {
            // Re-assert each frame so the game's Initialize() (or anything else)
            // can't leave MSAA on when we want it off by default.
            int want = _enabled ? On : Off;
            if (QualitySettings.antiAliasing != want)
                QualitySettings.antiAliasing = want;

            var kb = Keyboard.current;
            if (kb == null || !kb.f2Key.wasPressedThisFrame)
                return;

            _enabled = !_enabled;
            QualitySettings.antiAliasing = _enabled ? On : Off;
            LoggerInstance.Msg($"MSAA: {Describe(_enabled)} (antiAliasing={QualitySettings.antiAliasing})");
        }

        private static string Describe(bool on) => on ? "ON (2x)" : "OFF";
    }
}

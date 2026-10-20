using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace OrbusAbilityLabels;

[BepInPlugin("com.horizon.orbusabilitylabels", "OrbusAbilityLabels", "0.2.0")]
public sealed class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource Logger = null!;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<bool> ShowBard = null!;
    internal static ConfigEntry<bool> ShowMusketeer = null!;
    internal static ConfigEntry<float> LabelScale = null!;
    internal static ConfigEntry<float> VerticalOffset = null!;
    internal static ConfigEntry<float> TowardEyeOffset = null!;
    internal static ConfigEntry<bool> EnableDiagnostics = null!;
    internal static ConfigEntry<float> NearbyScanRadius = null!;

    internal static readonly Dictionary<int, ConfigEntry<string>> BardNames = new();
    internal static readonly Dictionary<string, ConfigEntry<string>> MusketeerNames = new();

    public override void Load()
    {
        Logger = Log;
        Enabled = Config.Bind("Labels", "Enabled", true,
            "Show world-space abbreviations above known local ability objects.");
        ShowBard = Config.Bind("Labels", "ShowBard", true,
            "Show temporary Bard instrument IDs; not verified ability names.");
        ShowMusketeer = Config.Bind("Labels", "ShowMusketeer", true,
            "Show the four identified Musketeer orb abbreviations.");
        LabelScale = Config.Bind("Labels", "Scale", 1f,
            new ConfigDescription("Label size multiplier.",
                new AcceptableValueRange<float>(0.5f, 2f)));
        VerticalOffset = Config.Bind("Labels", "AboveOrbMetres", 0.058f,
            new ConfigDescription("World-up distance above the ability object.",
                new AcceptableValueRange<float>(0.015f, 0.18f)));
        TowardEyeOffset = Config.Bind("Labels", "TowardHeadMetres", 0.016f,
            new ConfigDescription("Offset toward headset to avoid clipping into the orb.",
                new AcceptableValueRange<float>(0f, 0.1f)));

        BardNames[2] = Config.Bind("Bard", "Instrument2", "B2", "Temporary ID marker; rename after identifying the skill.");
        BardNames[3] = Config.Bind("Bard", "Instrument3", "B3", "Temporary ID marker; rename after identifying the skill.");
        BardNames[4] = Config.Bind("Bard", "Instrument4", "B4", "Temporary ID marker; rename after identifying the skill.");
        BardNames[5] = Config.Bind("Bard", "Instrument5", "B5", "Temporary ID marker; rename after identifying the skill.");
        BardNames[6] = Config.Bind("Bard", "Instrument6", "B6", "Temporary ID marker; rename after identifying the skill.");
        BardNames[8] = Config.Bind("Bard", "Instrument8", "B8", "Temporary ID marker; rename after identifying the skill.");

        MusketeerNames["ShieldOrb(Clone)"] = Config.Bind("Musketeer", "Shield", "SH", "Short label for ShieldOrb.");
        MusketeerNames["RenewOrb(Clone)"] = Config.Bind("Musketeer", "Renew", "RN", "Short label for RenewOrb.");
        MusketeerNames["GravityOrb(Clone)"] = Config.Bind("Musketeer", "Gravity", "GR", "Short label for GravityOrb.");
        MusketeerNames["BigHealOrb(Clone)"] = Config.Bind("Musketeer", "BigHeal", "BH", "Short label for BigHealOrb; do not assume Cure Wounds.");

        EnableDiagnostics = Config.Bind("Diagnostics", "EnableManualCapture", true,
            "Optional lightweight targeted Ctrl+Shift+L capture.");
        NearbyScanRadius = Config.Bind("Diagnostics", "NearbyScanRadiusMetres", 2f,
            new ConfigDescription("Distance for diagnostic orb capture.",
                new AcceptableValueRange<float>(0.5f, 3f)));

        AddComponent<AbilityLabelVisuals>();
        AddComponent<AbilityLabelDiagnostics>();
        Log.LogInfo("[AbilityLabels] v0.2.0 VISUAL PREVIEW loaded.");
        Log.LogInfo("[AbilityLabels] Musketeer SH/RN/GR/BH; Bard temporary B-number instrument labels.");
        Log.LogInfo("[AbilityLabels] No hotkey needed to see labels; Ctrl+Shift+L remains optional diagnostic.");
    }
}

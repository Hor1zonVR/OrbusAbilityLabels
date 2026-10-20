using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace OrbusAbilityLabels;

[BepInPlugin("com.horizon.orbusabilitylabels", "OrbusAbilityLabels", "0.2.1")]
public sealed class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource Logger = null!;
    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<bool> ShowBard = null!;
    internal static ConfigEntry<bool> ShowBardMeshPanel = null!;
    internal static ConfigEntry<float> BardPanelScale = null!;
    internal static ConfigEntry<float> BardPanelSideOffset = null!;
    internal static ConfigEntry<float> BardPanelHeight = null!;
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
        ShowBardMeshPanel = Config.Bind("Bard Inspector", "ShowTemporaryPanel", true,
            "Temporarily show a compact live Bard slot -> instrument ID -> mesh-name panel.");
        BardPanelScale = Config.Bind("Bard Inspector", "PanelScale", 1.0f,
            new ConfigDescription("Temporary Bard inspector panel size.",
                new AcceptableValueRange<float>(0.5f, 2f)));
        BardPanelSideOffset = Config.Bind("Bard Inspector", "SideOffsetMetres", 0.35f,
            new ConfigDescription("Horizontal position of panel next to Bard instruments.",
                new AcceptableValueRange<float>(-0.5f, 0.5f)));
        BardPanelHeight = Config.Bind("Bard Inspector", "HeightMetres", 0.22f,
            new ConfigDescription("Panel height above Bard instruments.",
                new AcceptableValueRange<float>(0.07f, 0.6f)));

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
        AddComponent<BardMeshPanel>();
        AddComponent<AbilityLabelDiagnostics>();
        Log.LogInfo("[AbilityLabels] v0.2.1 BARD MESH INSPECTOR loaded.");
        Log.LogInfo("[AbilityLabels] Musketeer SH/RN/GR/BH; Bard temporary B-number instrument labels.");
        Log.LogInfo("[AbilityLabels] Bard mesh panel automatically shows slot, instrument ID, mesh and logs loadout changes. No shortcut needed.");
    }
}

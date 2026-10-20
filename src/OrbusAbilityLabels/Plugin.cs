using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace OrbusAbilityLabels;

[BepInPlugin("com.horizon.orbusabilitylabels", "OrbusAbilityLabels", "0.2.2")]
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
    internal static ConfigEntry<float> TextCharacterSize = null!;
    internal static ConfigEntry<float> BardPanelCharacterSize = null!;
    internal static ConfigEntry<float> VerticalOffset = null!;
    internal static ConfigEntry<float> TowardEyeOffset = null!;
    internal static ConfigEntry<bool> EnableDiagnostics = null!;
    internal static ConfigEntry<float> NearbyScanRadius = null!;

    internal static readonly Dictionary<string, ConfigEntry<string>> BardMeshNames = new(System.StringComparer.OrdinalIgnoreCase);
    internal static readonly Dictionary<string, ConfigEntry<string>> MusketeerNames = new();

    public override void Load()
    {
        Logger = Log;
        Enabled = Config.Bind("Labels", "Enabled", true,
            "Show world-space abbreviations above known local ability objects.");
        // New key, since existing v0.2.1 config has ShowTemporaryPanel=true.
        // This preview must NOT show the debug overlay on users' next launch.
        ShowBardMeshPanel = Config.Bind("Bard Inspector", "ShowCompactPanel", false,
            "Optional tiny temporary mesh inspector panel; off by default now Bard is mapped.");
        BardPanelScale = Config.Bind("Bard Inspector", "PanelScale", 1.0f,
            new ConfigDescription("Temporary Bard inspector panel size.",
                new AcceptableValueRange<float>(0.5f, 2f)));
        BardPanelSideOffset = Config.Bind("Bard Inspector", "SideOffsetMetres", 0.53f,
            new ConfigDescription("Horizontal position of panel next to Bard instruments.",
                new AcceptableValueRange<float>(-0.65f, 0.65f)));
        BardPanelHeight = Config.Bind("Bard Inspector", "HeightMetres", 0.17f,
            new ConfigDescription("Panel height above Bard instruments.",
                new AcceptableValueRange<float>(0.07f, 0.6f)));

        BardPanelCharacterSize = Config.Bind("Bard Inspector", "TextSize", 0.003f,
            new ConfigDescription("Physical VR font size for the optional debug panel.",
                new AcceptableValueRange<float>(0.001f, 0.009f)));

        ShowBard = Config.Bind("Labels", "ShowBard", true,
            "Show real Bard abbreviations matched to eight named ability meshes.");
        ShowMusketeer = Config.Bind("Labels", "ShowMusketeer", true,
            "Show the four identified Musketeer orb abbreviations.");
        LabelScale = Config.Bind("Labels", "Scale", 1f,
            new ConfigDescription("Label size multiplier.",
                new AcceptableValueRange<float>(0.5f, 2f)));
        TextCharacterSize = Config.Bind("Labels", "TextSize", 0.0025f,
            new ConfigDescription("Physical VR font size. Reduced greatly from the huge v0.2.1 text.",
                new AcceptableValueRange<float>(0.001f, 0.01f)));
        VerticalOffset = Config.Bind("Labels", "AboveOrbMetres", 0.072f,
            new ConfigDescription("World-up distance above the ability object.",
                new AcceptableValueRange<float>(0.015f, 0.18f)));
        TowardEyeOffset = Config.Bind("Labels", "TowardHeadMetres", 0.016f,
            new ConfigDescription("Offset toward headset to avoid clipping into the orb.",
                new AcceptableValueRange<float>(0f, 0.1f)));

        // Confirmed by user 2026-10-20: bind to LIVE Bard mesh names,
        // NOT InstrumentN object IDs or physical slot positions.
        BardMeshNames["healOrb-01"] = Config.Bind("Bard Abilities", "healOrb-01", "GH", "Gentle Healing");
        BardMeshNames["healOrb-02"] = Config.Bind("Bard Abilities", "healOrb-02", "CW", "Cure Wounds");
        BardMeshNames["atkOrb-02"] = Config.Bind("Bard Abilities", "atkOrb-02", "IJ", "Instrument of Justice");
        BardMeshNames["atkOrb-01"] = Config.Bind("Bard Abilities", "atkOrb-01", "HA", "Harm");
        BardMeshNames["defOrb-04"] = Config.Bind("Bard Abilities", "defOrb-04", "PR", "Protection");
        BardMeshNames["defOrb-02"] = Config.Bind("Bard Abilities", "defOrb-02", "IN", "Inspiration");
        BardMeshNames["defOrb-01"] = Config.Bind("Bard Abilities", "defOrb-01", "SP", "Speed");
        BardMeshNames["defOrb-03"] = Config.Bind("Bard Abilities", "defOrb-03", "DF", "Defense");

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
        Log.LogInfo("[AbilityLabels] v0.2.2 COMPACT BARD LABELS loaded.");
        Log.LogInfo("[AbilityLabels] Bard GH/CW/IJ/HA/PR/IN/SP/DF by live mesh; Musketeer SH/RN/GR/BH.");
        Log.LogInfo("[AbilityLabels] Tiny Bard debug panel OFF by default; can be enabled via Bard Inspector/ShowCompactPanel.");
    }
}

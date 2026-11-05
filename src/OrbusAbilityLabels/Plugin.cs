using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace OrbusAbilityLabels;

[BepInPlugin("com.horizon.orbusabilitylabels", "OrbusAbilityLabels", "0.2.4")]
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
        // The temporary Bard mesh inspector is retired now that all eight
        // abilities are mapped. Do not bind its obsolete user config keys.
        // Its source remains for developers, but the component is not loaded.

        ShowBard = Config.Bind("Labels", "ShowBard", true,
            "Show real Bard abbreviations matched to eight named ability meshes.");
        ShowMusketeer = Config.Bind("Labels", "ShowMusketeer", true,
            "Show the four identified Musketeer orb abbreviations.");
        LabelScale = Config.Bind("Labels", "Scale", 1f,
            new ConfigDescription("Label size multiplier.",
                new AcceptableValueRange<float>(0.5f, 2f)));
        // New config key deliberately: v0.2.2 wrote TextSize=0.0025
        // to existing users' .cfg files. Reusing that key would preserve
        // the too-small old value instead of applying the safer new default.
        // Scale continues to work as a fine-adjustment multiplier.
        TextCharacterSize = Config.Bind("Labels", "ReadableTextSize", 0.004f,
            new ConfigDescription("Bright white label font size in VR. About 60% larger than v0.2.2. " +
                "Replaces legacy TextSize setting.",
                new AcceptableValueRange<float>(0.0015f, 0.012f)));
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

        // Old capture-only diagnostics no longer run in standard builds.
        // Avoid re-creating the two diagnostic .cfg entries on every launch.
        AddComponent<AbilityLabelVisuals>();
        Log.LogInfo("[AbilityLabels] v0.2.4 labels loaded; retired inspector/diagnostics disabled.");
        Log.LogInfo("[AbilityLabels] Bard GH/CW/IJ/HA/PR/IN/SP/DF by live mesh; Musketeer SH/RN/GR/BH.");
        Log.LogInfo("[AbilityLabels] VR text is bright white, 60% larger with the shadow behind; adjust [Labels] ReadableTextSize.");
        Log.LogInfo("[AbilityLabels] Retired Bard inspector and diagnostic components are not loaded.");
    }
}

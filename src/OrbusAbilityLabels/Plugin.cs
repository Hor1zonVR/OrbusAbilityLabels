using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace OrbusAbilityLabels;

[BepInPlugin("com.horizon.orbusabilitylabels", "OrbusAbilityLabels", "0.1.1")]
public sealed class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource Logger = null!;
    internal static ConfigEntry<bool> EnableDiagnostics = null!;
    internal static ConfigEntry<float> NearbyScanRadius = null!;

    public override void Load()
    {
        Logger = Log;
        EnableDiagnostics = Config.Bind("Diagnostics", "EnableManualCapture", true,
            "Allow targeted local-only Bard/Musketeer inspection with Ctrl+Shift+L. No full-scene scan.");
        NearbyScanRadius = Config.Bind("Diagnostics", "NearbyScanRadiusMetres", 2.0f,
            new ConfigDescription("Maximum distance from the headset for known active Musketeer orbs (avoid remote orbs).",
                new AcceptableValueRange<float>(0.5f, 3.0f)));

        // These standard BepInEx options will be editable by RebornMenu later,
        // without any runtime dependency on RebornMenu itself.
        AddComponent<AbilityLabelDiagnostics>();
        Log.LogInfo("[AbilityLabels] v0.1.1 TARGETED DIAGNOSTIC ONLY loaded.");
        Log.LogInfo("[AbilityLabels] No labels or game-ability modifications in this build.");
        Log.LogInfo("[AbilityLabels] Equip Bard or Musketeer, then press Ctrl+Shift+L.");
        Log.LogInfo("[AbilityLabels] Report: BepInEx/AbilityLabelsReport.txt");
    }
}

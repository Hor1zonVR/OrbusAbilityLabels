# OrbusAbilityLabels

Client-side BepInEx IL2CPP mod for tiny labels immediately ABOVE the real Bard instruments and Musketeer orbs in My Orbus Renewed.

## v0.2.3 - brighter, clearer floating ability abbreviations

This is a readability-only update based on feedback that v0.2.2 labels **appeared correctly but were too small and dark**.

- **Pure white text** instead of pale gold, with full alpha.
- **60% larger** default physical VR text (0.004 instead of 0.0025). Still much smaller than the v0.2.1 giant-UI experiment.
- **Shadow moved behind the lettering**; the previous -Z offset could place the dark copy in front of the primary text.
- **New config key:** `[Labels] ReadableTextSize = 0.004`. This deliberately supersedes the old `[Labels] TextSize` key, since BepInEx preserves old values across version upgrades. If the old `TextSize` line is still present in the config, it is ignored by v0.2.3.
- Existing `[Labels] Scale` is still usable to adjust further.
- No changes to Bard mesh mapping, Musketeer ability mapping, game cameras, old diagnostics or VR/controller input.

**Test:** Pull this repository, build and publish v0.2.3 with `release.ps1`, update through RebornManager, then equip Bard and Musketeer. Confirm text is visibly brighter, readable at normal hand distance, still positioned above the correct orb and not excessively large. If your saved `Scale` setting had been modified, it will still multiply the new size. Send a screenshot for any further tuning.

## v0.2.2: fixed giant text; verified Bard labels

The previous v0.2.1 visual test made the text far too large. Both the floating labels and the optional debug panel now use greatly reduced Unity TextMesh.characterSize, plus optional text-size settings. The debug panel is **OFF by default**, including for installations that saved the old ShowTemporaryPanel=true preference (we use a NEW config key).

User-verified Bard CURRENT MESH -> ABILITY NAME -> ABBREVIATION (20 Oct 2026):

| Mesh name | Ability | Label |
| --- | --- | --- |
| healOrb-01 | Gentle Healing | GH |
| healOrb-02 | Cure Wounds | CW |
| atkOrb-02 | Instrument of Justice | IJ |
| atkOrb-01 | Harm | HA |
| defOrb-04 | Protection | PR |
| defOrb-02 | Inspiration | IN |
| defOrb-01 | Speed | SP |
| defOrb-03 | Defense | DF |

Labels are bound to the **actual current MeshFilter.sharedMesh.name**, never the equipped slot number or a hardcoded InstrumentN ID. The mod checks only the local Bard six-slot hierarchy every 0.35 seconds. The previously observed four Musketeer orbs remain SH/RN/GR/BH. Unknown meshes are not given incorrect guessed names.

## Single-session visual test

1. Run release.ps1 from your clone to compile and publish v0.2.2 against saved My Orbus Renewed BepInEx interop, then update AbilityLabels in RebornManager.
2. Launch with Bard. The giant debug UI should be GONE. There should be only **small** GH/CW/IJ/HA/PR/IN/SP/DF labels, each immediately above its orb.
3. Move Bard instruments around and swap some ability loadout slots, especially Speed and Defense. Each abbreviation should follow the correct orb; if a mesh changes while moving, report when labels disappear.
4. Switch to Musketeer and verify its floating SH/RN/GR/BH text is equally small.
5. Send a screenshot or short recording for size, legibility, angle, and placement. If there's a freeze or error, stop and send BepInEx/LogOutput.log.

## Options

After loading once, edit My Orbus Renewed/BepInEx/config/com.horizon.orbusabilitylabels.cfg (with game closed):

- [Labels] Enabled, ShowBard, ShowMusketeer, Scale, **ReadableTextSize** (default 0.004), AboveOrbMetres, TowardHeadMetres (the old TextSize key is ignored)
- [Bard Abilities] eight editable mesh ID -> abbreviation options
- [Musketeer] four editable orb names -> abbreviations
- [Bard Inspector] **ShowCompactPanel** (default false), TextSize (default 0.003), PanelScale, SideOffsetMetres, HeightMetres

Important: v0.2.1's old ShowTemporaryPanel is no longer used; leave it as-is. If you enable ShowCompactPanel, the panel is much smaller and retains automatic change logging to BepInEx/AbilityLabelsBardMap.txt.

## Build and publish to GitHub for RebornManager

From any PowerShell folder:

    $repo = "$env:USERPROFILE\OneDrive\Documents\OrbusAbilityLabels"
    git -C $repo pull --ff-only
    & "$repo\release.ps1"

The release script automatically uses saved My Orbus Renewed interop, compiles and publishes the DLL on GitHub. It doesn't install or touch game files itself. If errors occur, send the terminal output. This version is source-committed and still needs compilation/VR verification.

## Safety

v0.1.0's old 82,000-object scan caused a VR freeze; don't use that old version. Current captures are targeted and optional only (Ctrl+Shift+L). The visual mod doesn't alter ability behaviour, networking or VR inputs. Supported only for observed local rig paths.

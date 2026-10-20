# OrbusAbilityLabels

OrbusVR client-side BepInEx IL2CPP quality-of-life mod that adds small floating text above physical ability orbs.

## v0.2.0 - first visual prototype (still included in v0.2.1)

This is an experimental first visual build for My Orbus Renewed. The source is on GitHub, but the DLL still needs to be compiled and tested locally before it is verified in VR.

**Musketeer:** SH, RN, GR and BH appear above nearby real ShieldOrb(Clone), RenewOrb(Clone), GravityOrb(Clone) and BigHealOrb(Clone) objects only when your local OrbLauncher is active. BH refers to BigHealOrb: it is not a claim that the spell is Cure Wounds.

**Bard:** B2, B3, B4, B5, B6 and B8 are temporary instrument IDs, not actual skill names. The labels attach to the six current InstrumentN(Clone) objects inside the local Bard NotesContainer. These markers will let us validate placement and determine the true ability mapping later.

Labels use VR-facing TextMesh with subtle dark shadow, a small world-up offset and per-label configuration. They automatically hide on class change or when their bound object disappears. No gameplay mechanics or network calls are modified.

**Important:** The prototype finds only the four already-discovered Musketeer names and follows the local Bard instrument hierarchy. It does not run any expensive full-scene Unity object enumeration.

## Test

1. Build and publish v0.2.0 with the local release.ps1 script. Refresh RebornManager and update AbilityLabels in My Orbus Renewed.
2. Launch Orbus, equip Bard, and inspect the B-number markers above the instruments. Check whether they remain above their correct orbs while you move.
3. Switch to Musketeer and check SH, RN, GR and BH, moving your gun and orbs. No shortcut is needed to display labels.
4. Send a screenshot or short recording showing the label position, orientation and scale, plus BepInEx/LogOutput.log if anything is missing.
5. If VR freezes again, stop testing and exit the game; do not retry the old v0.1.0 diagnostic.

## Build and release

Run these commands in PowerShell from any folder:

    $repo = "$env:USERPROFILE\OneDrive\Documents\OrbusAbilityLabels"
    git -C $repo pull --ff-only
    & "$repo\release.ps1"

release.ps1 reuses the previously cached My Orbus Renewed BepInEx interop references instead of prompting for a game installation. It only compiles and publishes the DLL on GitHub; RebornManager installs it into the chosen instance.

## Options

Once loaded, BepInEx/config/com.horizon.orbusabilitylabels.cfg offers:

- Labels: Enabled, ShowBard, ShowMusketeer, Scale, AboveOrbMetres, TowardHeadMetres
- Musketeer: Shield, Renew, Gravity, BigHeal (text is user-editable)
- Bard: Instrument2, Instrument3, Instrument4, Instrument5, Instrument6, Instrument8 (temporary IDs; user-editable)
- Diagnostics: EnableManualCapture and NearbyScanRadiusMetres

Leave an individual label blank to hide it. The eventual RebornMenu settings can use these BepInEx config entries with no dependency today.

## Diagnostics and safety history

v0.1.0 performed a synchronous scan of about 82,000 Unity objects and coincided with a frozen-eye image in VR. Do not use v0.1.0 in VR. v0.1.1 replaced that with a targeted probe and successfully saved three user captures in 12, 4 and 4 ms, respectively. v0.2.0 retains only the smaller optional diagnostic on Ctrl+Shift+L. The visual test does not need that shortcut.

## v0.2.1 - temporary Bard mesh identification UI

This is the recommended build for mapping Bard instruments during ONE VR session. The earlier v0.2.0 visual-label experiment remains, but this build additionally shows a small inspector beside the Bard instruments only while Bard is equipped.

The panel reads the SIX local `EquippedSlot1` slots under the actual `NotesContainer` and updates every 0.35 seconds. Each row displays SLOT, Instrument ID (I3, I4, etc), and the actual MeshFilter.sharedMesh.name (for example `healOrb-01`). Empty slots show `--`. A new/unseen instrument also automatically gets a temporary `B<number>` floating label.

**Suggested single-session workflow:** Equip Bard, open your in-game ability list to learn a skill name, swap one instrument at a time, and read the changed ID/mesh on this temporary panel. Repeat until you have matched all abilities. You don't need to stop the game between swaps. Report the corresponding names afterwards; our code does not claim to read the game's ability names yet.

Every time the local Bard loadout changes, the plugin ALSO appends the entire current six-row mapping to `My Orbus Renewed/BepInEx/AbilityLabelsBardMap.txt`. Send this file after your session alongside the ability names (and optionally screenshots). The auto-log is capped to 120 changed snapshots per session and never scans the whole scene.

The panel is purely read-only and automatically hides on class change. It floats next to the instruments, not on the VR camera. It never edits skills or inventory.

Options under `[Bard Inspector]` in BepInEx config: `ShowTemporaryPanel` (default true), `PanelScale`, `SideOffsetMetres` and `HeightMetres`. Once mapping is done, set ShowTemporaryPanel=false; a future version will remove it.

Build from any PowerShell folder with the usual `git -C $repo pull --ff-only` then `& $repo\release.ps1`. RebornManager will install the GitHub release after publication. If compilation fails, send output instead of manually copying DLLs.

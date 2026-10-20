# v0.1.1 targeted diagnostic (VR freeze mitigation)

v0.1.0 was too broad: each hotkey captured approximately 53,500 GameObjects, 17,900 MonoBehaviours, and 10,500 Renderers **synchronously in Unity's update loop**. User-reported VR eye image freeze followed pressing Ctrl+Shift+L. Although the BepInEx log shows all captures saved without errors, a main-thread render stall is the leading diagnosis. Do not ask anyone to rerun v0.1.0.

The initial My Orbus Renewed captures already found:
- Bard: `PlayerRigv4/Avatar/Bard(Clone)/Instruments/NotesContainer/EquippedSlot1 ... (5)/InstrumentN(Clone)`
- Musketeer: `PlayerRigv4/Controller (right)/OrbLauncher/OrbSlot0..3`
- Active named root orbs: `ShieldOrb(Clone)`, `RenewOrb(Clone)`, `GravityOrb(Clone)`, `BigHealOrb(Clone)`

v0.1.1 only visits these specific local objects with a maximum of 70 nodes per weapon hierarchy; four exact-name Musketeer orbs are checked near the player's eye camera. There is no global `Resources.FindObjectsOfTypeAll` call. No Unity objects or abilities are modified. Capture on Ctrl+Shift+L is optional; there is already enough information to proceed investigating the visual label MVP.

**Do not assume `BigHealOrb(Clone)` means Cure Wounds without an explicit ability ID mapping.** Bard InstrumentN names are not yet mapped to ability names. Only display labels once mapping is verified.

To release locally from My Orbus Renewed, once:
```powershell
$repo = "$env:USERPROFILE\OneDrive\Documents\OrbusAbilityLabels"
git -C $repo pull --ff-only
& "$repo\release.ps1" -GameDir "$env:LOCALAPPDATA\OrbusRebornModdingInstances\My Orbus Renewed"
```
Build references are cached for later runs. This script **does not install into the game**.

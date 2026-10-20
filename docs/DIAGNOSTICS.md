# First diagnostic run

This build intentionally **does not draw labels**. We need to find the *actual* ability identifiers and transforms before labelling to avoid misleading players in combat.

## Capture sequence

1. Equip Bard with its instruments out, preferably in an empty room so nearby players do not fill the report.
2. Press **Ctrl+Shift+L** on the desktop keyboard. Confirm `[AbilityLabels] Capture 1 saved` in `BepInEx/LogOutput.log`.
3. Equip Musketeer and make its ability orbs visible; press **Ctrl+Shift+L** again.
4. Optional: repeat with one equipped ability swapped, to identify what changed.
5. Leave the game. Send `BepInEx/AbilityLabelsReport.txt` with the class/capture order and a note of which abilities were equipped. Check the file for nearby player names before sharing.

The report is in **the launched RebornManager game instance's** `BepInEx` folder. It contains the name/path of nearby renderers and matching component types, not your inventory data, world coordinates or authentication data. Snapshots are manual and limited to 12 each game launch. Each launch starts a fresh report.

## What we need to learn

- Does the weapon or an equipped ability carry a native component name we can identify?
- Are Bard instrument objects and Musketeer orbs parented under a stable local-player weapon transform?
- Do ability changes replace GameObjects, update fields on a reusable slot, or just change materials?
- Is the correct label anchored to the orb or to a separate stable slot transform?

After the log reveals candidates we can write a more focused second diagnostic (if necessary), and only then implement the labels. No guessing from orb appearance.

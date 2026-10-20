using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace OrbusAbilityLabels;

/// <summary>
/// Safe, read-only diagnostics for already-identified local Bard and Musketeer
/// objects. Deliberately NEVER enumerates the game's entire object registry:
/// the original v0.1.0 synchronous ~82,000-object scan could stall VR rendering.
/// </summary>
public sealed class AbilityLabelDiagnostics : MonoBehaviour
{
    public AbilityLabelDiagnostics(IntPtr pointer) : base(pointer) { }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    private const int Ctrl = 0x11;
    private const int Shift = 0x10;
    private const int L = 0x4C;
    private const int MaxCaptures = 8;
    private const int MaxNodes = 70;
    private const int MaxDepth = 3;

    // These are grounded in the v0.1.0 My Orbus Renewed capture.
    // Names identify candidates, NOT guaranteed skill names or ownership.
    private static readonly string[] KnownMusketeerObjects =
    {
        "ShieldOrb(Clone)",
        "RenewOrb(Clone)",
        "GravityOrb(Clone)",
        "BigHealOrb(Clone)"
    };

    private bool _lastChord;
    private bool _busy;
    private int _captures;
    private string _reportPath = "";

    private static bool Held(int code) => (GetAsyncKeyState(code) & 0x8000) != 0;

    private void Start()
    {
        _reportPath = Path.Combine(Paths.BepInExRootPath, "AbilityLabelsReport.txt");
        try
        {
            File.WriteAllText(_reportPath,
                "OrbusAbilityLabels v0.2.0 - TARGETED read-only local diagnostic" +
                Environment.NewLine +
                "No full-scene enumeration. No network, ability data or object changes." +
                Environment.NewLine +
                "Captures inspect only the local rig's weapon nodes and four known orb objects." +
                Environment.NewLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("[AbilityLabels] Report setup failed: " + ex.Message);
        }

        Plugin.Logger.LogInfo("[AbilityLabels] v0.2.0 lightweight diagnostics ready: Ctrl+Shift+L.");
        Plugin.Logger.LogInfo("[AbilityLabels] Global GameObject / Renderer / MonoBehaviour scans disabled.");
    }

    private void Update()
    {
        bool pressed = Application.isFocused && Held(Ctrl) && Held(Shift) && Held(L);
        if (pressed && !_lastChord && !_busy &&
            Plugin.EnableDiagnostics.Value && _captures < MaxCaptures)
            Capture();
        _lastChord = pressed;
    }

    private static string Clean(string? value, int limit = 100)
    {
        if (string.IsNullOrWhiteSpace(value)) return "(unnamed)";
        string s = value.Replace('\r', ' ').Replace('\n', ' ')
            .Replace('\t', ' ').Trim();
        return s.Length > limit ? s.Substring(0, limit) + "..." : s;
    }

    private static string PathOf(Transform? node)
    {
        var parts = new List<string>();
        int count = 0;
        while (node != null && count++ < 9)
        {
            parts.Add(Clean(node.name, 60));
            node = node.parent;
        }
        parts.Reverse();
        return (node != null ? ".../" : "") + string.Join("/", parts);
    }

    private static string NativeType(Component component)
    {
        try
        {
            IntPtr klass = IL2CPP.il2cpp_object_get_class(component.Pointer);
            if (klass != IntPtr.Zero)
            {
                string type = IL2CPP.il2cpp_class_get_name_(klass) ?? "";
                string ns = IL2CPP.il2cpp_class_get_namespace_(klass) ?? "";
                if (type.Length > 0)
                    return ns.Length == 0 ? type : ns + "." + type;
            }
        }
        catch { }
        return component.GetType().Name;
    }

    private static string ComponentTypes(GameObject gameObject)
    {
        var names = new List<string>();
        try
        {
            foreach (Component component in gameObject.GetComponents<Component>())
            {
                if (component == null) continue;
                if (names.Count >= 12) break;
                names.Add(Clean(NativeType(component), 75));
            }
        }
        catch (Exception ex)
        {
            names.Add("(component read failed: " + Clean(ex.Message, 50) + ")");
        }
        return string.Join(", ", names);
    }

    private static void Describe(StringBuilder text, Transform node, string label,
        Vector3 headPosition, bool hasHead)
    {
        try
        {
            GameObject go = node.gameObject;
            text.Append(label).Append(" path=").Append(PathOf(node))
                .Append(" active=").Append(go.activeInHierarchy);
            if (hasHead)
            {
                float distance = Vector3.Distance(headPosition, node.position);
                text.Append(" headsetDistanceMetres=")
                    .Append(distance.ToString("0.00"));
            }
            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                text.Append(" mesh=").Append(Clean(filter.sharedMesh.name, 75));
            text.Append(" types=").AppendLine(ComponentTypes(go));
        }
        catch (Exception ex)
        {
            text.Append(label).Append(" inspection failed: ")
                .AppendLine(Clean(ex.Message));
        }
    }

    private static void SmallHierarchy(StringBuilder text, Transform? root,
        string title, Vector3 head, bool hasHead)
    {
        text.AppendLine("=== " + title + " ===");
        if (root == null)
        {
            text.AppendLine("Not present for this class/session.");
            return;
        }
        var queue = new Queue<(Transform Node, int Depth)>();
        queue.Enqueue((root, 0));
        int checkedCount = 0;
        while (queue.Count != 0 && checkedCount < MaxNodes)
        {
            var current = queue.Dequeue();
            Transform node = current.Node;
            if (node == null) continue;
            checkedCount++;
            Describe(text, node, "LOCAL", head, hasHead);

            if (current.Depth >= MaxDepth) continue;
            try
            {
                int children = Math.Min(node.childCount, 20);
                for (int i = 0; i < children; i++)
                    queue.Enqueue((node.GetChild(i), current.Depth + 1));
            }
            catch (Exception ex)
            {
                text.AppendLine("Children unavailable: " + Clean(ex.Message));
            }
        }
        text.Append("Local hierarchy nodes inspected=").Append(checkedCount)
            .Append(" (max ").Append(MaxNodes).AppendLine(")");
    }

    private static Transform? FindChild(Transform? root, string relative)
    {
        if (root == null) return null;
        try { return root.Find(relative); }
        catch { return null; }
    }

    private static void InspectKnownMusketeerOrbs(StringBuilder text,
        Vector3 head, bool hasHead)
    {
        text.AppendLine("=== KNOWN MUSKETEER ORBS (ACTIVE OBJECTS ONLY) ===");
        foreach (string name in KnownMusketeerObjects)
        {
            try
            {
                // GameObject.Find is an exact-name lookup, not the original
                // Resources.FindObjectsOfTypeAll global inventory scan.
                GameObject go = GameObject.Find(name);
                if (go == null)
                {
                    text.AppendLine(name + " not currently active/found");
                    continue;
                }
                float distance = hasHead
                    ? Vector3.Distance(head, go.transform.position) : -1f;
                if (hasHead && distance > Plugin.NearbyScanRadius.Value)
                {
                    text.AppendLine(name + " found but not near local headset (" +
                        distance.ToString("0.0") + "m); skipped as possibly remote.");
                    continue;
                }
                Describe(text, go.transform, "ORB", head, hasHead);
                // Only show immediate child names; no thousands of particle objects.
                int count = Math.Min(go.transform.childCount, 8);
                for (int j = 0; j < count; j++)
                {
                    Transform child = go.transform.GetChild(j);
                    if (child != null)
                        text.Append("    child=").Append(Clean(child.name, 75))
                            .Append(" active=").AppendLine(child.gameObject.activeInHierarchy.ToString());
                }
            }
            catch (Exception ex)
            {
                text.Append(name).Append(": ")
                    .AppendLine(Clean(ex.Message));
            }
        }
    }

    private void Capture()
    {
        _busy = true;
        _captures++;
        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            var report = new StringBuilder(8000);
            report.AppendLine("================================================");
            report.Append("CAPTURE ").Append(_captures)
                .Append(" UTC ").Append(DateTimeOffset.UtcNow.ToString("O"))
                .AppendLine();
            report.AppendLine("Targeted local inspection only. No full-scene scans.");

            Vector3 head = Vector3.zero;
            bool hasHead = false;
            try
            {
                foreach (Camera camera in Camera.allCameras)
                {
                    if (camera == null || !camera.isActiveAndEnabled ||
                        !camera.stereoEnabled ||
                        !string.Equals(camera.name, "Camera (eye)",
                            StringComparison.Ordinal)) continue;
                    head = camera.transform.position;
                    hasHead = true;
                    break;
                }
            }
            catch { }

            // PlayerRigv4 is the actual local rig found in the user's Renewed logs.
            // Never fall back to other-player avatars or wardrobe preview objects.
            GameObject rig = GameObject.Find("PlayerRigv4");
            Transform? player = rig != null ? rig.transform : null;
            report.AppendLine(player == null
                ? "Local PlayerRigv4 not found."
                : "Local PlayerRigv4 found; stereo camera=" + hasHead);

            SmallHierarchy(report,
                FindChild(player, "Avatar/Bard(Clone)/Instruments/NotesContainer"),
                "BARD EQUIPPED INSTRUMENT SLOTS", head, hasHead);
            SmallHierarchy(report,
                FindChild(player, "Controller (right)/OrbLauncher"),
                "MUSKETEER LOCAL GUN AND FOUR SLOTS", head, hasHead);
            InspectKnownMusketeerOrbs(report, head, hasHead);
            report.AppendLine("END CAPTURE");
            report.AppendLine();

            File.AppendAllText(_reportPath, report.ToString());
            watch.Stop();
            Plugin.Logger.LogInfo("[AbilityLabels] Safe capture " + _captures +
                " saved (" + report.Length + " chars, " +
                watch.ElapsedMilliseconds + "ms).");
            if (watch.ElapsedMilliseconds > 75)
                Plugin.Logger.LogWarning("[AbilityLabels] Capture took " +
                    watch.ElapsedMilliseconds + "ms; avoid repeated testing in VR.");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("[AbilityLabels] Capture failed: " + ex);
        }
        finally { _busy = false; }
    }
}

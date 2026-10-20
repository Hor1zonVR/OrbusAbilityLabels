using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BepInEx;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace OrbusAbilityLabels;

/// <summary>
/// Read-only, bounded, explicitly triggered inspection to discover how Orbus
/// represents the Bard instruments and Musketeer ability orbs. The diagnostic
/// never changes game objects, ability values, inputs, or network state.
/// </summary>
public sealed class AbilityLabelDiagnostics : MonoBehaviour
{
    public AbilityLabelDiagnostics(IntPtr pointer) : base(pointer) { }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    private const int Ctrl = 0x11;
    private const int Shift = 0x10;
    private const int L = 0x4C;
    private const int MaxGameObjects = 85000;
    private const int MaxKeywordRows = 140;
    private const int MaxScriptObjects = 30000;
    private const int MaxScriptRows = 90;
    private const int MaxRendererObjects = 45000;
    private const int MaxNearbyRows = 95;
    private const int MaxCaptures = 12;

    private static readonly string[] Terms =
    {
        "bard", "musketeer", "musket", "flintlock", "rifle", "shotgun",
        "instrument", "xylophone", "mallet", "music", "melody", "note",
        "song", "heal", "shield", "renew", "cure", "decurs", "poison",
        "gravity", "frost", "spell", "ability", "skill", "projectile",
        "orb", "bullet", "spellslot", "quickslot"
    };

    private sealed class NearbyRenderer
    {
        internal Renderer Renderer = null!;
        internal float Distance;
        internal float Forward;
    }

    private bool _lastChord;
    private bool _scanning;
    private int _captureCount;
    private string _reportPath = "";

    private static bool Held(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;

    private void Start()
    {
        _reportPath = Path.Combine(Paths.BepInExRootPath, "AbilityLabelsReport.txt");
        try
        {
            File.WriteAllText(_reportPath,
                "OrbusAbilityLabels v0.1.0 - local, manually triggered diagnostic" + Environment.NewLine +
                "Snapshots do not read game account credentials or ability field values." + Environment.NewLine +
                "Review object names before sharing this file (other players may be nearby)." +
                Environment.NewLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("[AbilityLabels] Cannot prepare report: " + ex.Message);
        }
        Plugin.Logger.LogInfo("[AbilityLabels] Ctrl+Shift+L captures one ability scene snapshot; maximum 12 per launch.");
    }

    private void Update()
    {
        bool chord = Application.isFocused && Held(Ctrl) && Held(Shift) && Held(L);
        if (chord && !_lastChord && !_scanning && _captureCount < MaxCaptures &&
            Plugin.EnableDiagnostics.Value)
            Capture();
        _lastChord = chord;
    }

    private static string Clean(string? value, int maxLength = 82)
    {
        if (string.IsNullOrWhiteSpace(value)) return "(unnamed)";
        string result = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
        return result.Length <= maxLength ? result : result.Substring(0, maxLength) + "...";
    }

    private static bool Matches(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        string s = raw.ToLowerInvariant();
        foreach (string term in Terms)
        {
            // Avoid the word "Orbus" itself accidentally matching "orb".
            if (term == "orb")
            {
                if (s.Contains("orb") && !s.Contains("orbus")) return true;
            }
            else if (s.Contains(term)) return true;
        }
        return false;
    }

    private static bool Live(GameObject go)
    {
        try { return go.scene.IsValid() && go.scene.isLoaded; }
        catch { return false; }
    }

    private static string PathOf(Transform? node, int maxDepth = 8)
    {
        if (node == null) return "(null)";
        var parts = new List<string>();
        Transform? current = node;
        while (current != null && parts.Count < maxDepth)
        {
            parts.Add(Clean(current.gameObject.name, 65));
            current = current.parent;
        }
        parts.Reverse();
        return (current == null ? "" : ".../") + string.Join("/", parts);
    }

    private static string NativeType(Component component)
    {
        try
        {
            IntPtr klass = IL2CPP.il2cpp_object_get_class(component.Pointer);
            if (klass != IntPtr.Zero)
            {
                string type = IL2CPP.il2cpp_class_get_name_(klass) ?? "";
                string space = IL2CPP.il2cpp_class_get_namespace_(klass) ?? "";
                if (type.Length > 0) return (space.Length == 0 ? "" : space + ".") + type;
            }
        }
        catch { }
        return component.GetType().FullName ?? component.GetType().Name;
    }

    private static string Components(GameObject go, int limit = 9)
    {
        var parts = new List<string>();
        try
        {
            foreach (Component component in go.GetComponents<Component>())
            {
                if (component == null) continue;
                if (parts.Count >= limit) break;
                parts.Add(Clean(NativeType(component), 90));
            }
        }
        catch { return "(components unavailable)"; }
        return parts.Count == 0 ? "(none)" : string.Join(", ", parts);
    }

    private static Camera? HeadsetCamera()
    {
        Camera? fallback = null;
        try
        {
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera == null || !camera.isActiveAndEnabled) continue;
                if (fallback == null) fallback = camera;
                string name = camera.name ?? "";
                if (camera.stereoEnabled &&
                    name.IndexOf("UI on Top", StringComparison.OrdinalIgnoreCase) < 0)
                    return camera;
            }
            if (Camera.main != null && Camera.main.isActiveAndEnabled)
                return Camera.main;
        }
        catch { }
        return fallback;
    }

    private static void ScanNamedObjects(StringBuilder report)
    {
        report.AppendLine("=== LOADED GAME OBJECTS WITH ABILITY-RELATED NAMES ===");
        int inspected = 0, matches = 0, errors = 0;
        try
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (++inspected > MaxGameObjects) break;
                if (go == null) continue;
                try
                {
                    if (!Matches(go.name)) continue;
                    matches++;
                    if (matches > MaxKeywordRows) continue;
                    report.Append("OBJECT active=").Append(go.activeInHierarchy)
                        .Append(" live=").Append(Live(go))
                        .Append(" path=").Append(PathOf(go.transform))
                        .Append(" types=").AppendLine(Components(go));
                }
                catch { errors++; }
            }
        }
        catch (Exception ex) { report.AppendLine("Object enumeration: " + Clean(ex.Message)); }
        report.Append("Objects inspected=").Append(inspected).Append(" matching=")
            .Append(matches).Append(" printed=").Append(Math.Min(matches, MaxKeywordRows))
            .Append(" errors=").AppendLine(errors.ToString());
        report.AppendLine();
    }

    private static void ScanScripts(StringBuilder report)
    {
        report.AppendLine("=== NATIVE GAME SCRIPT TYPES MATCHING ABILITY TERMS ===");
        int inspected = 0, matches = 0, errors = 0;
        var unique = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (MonoBehaviour component in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
            {
                if (++inspected > MaxScriptObjects) break;
                if (component == null) continue;
                try
                {
                    string type = NativeType(component);
                    if (!Matches(type)) continue;
                    matches++;
                    if (!unique.Add(type) || unique.Count > MaxScriptRows) continue;
                    report.Append("SCRIPT ").Append(Clean(type, 130))
                        .Append(" attachedTo=").AppendLine(PathOf(component.transform, 6));
                }
                catch { errors++; }
            }
        }
        catch (Exception ex) { report.AppendLine("Script enumeration: " + Clean(ex.Message)); }
        report.Append("Scripts inspected=").Append(inspected)
            .Append(" matches=").Append(matches)
            .Append(" unique=").Append(unique.Count)
            .Append(" printed=").Append(Math.Min(unique.Count, MaxScriptRows))
            .Append(" errors=").AppendLine(errors.ToString());
        report.AppendLine();
    }

    private static void ScanNearbyRenderers(StringBuilder report)
    {
        report.AppendLine("=== VISIBLE RENDERERS NEAR HEADSET ===");
        Camera? head = HeadsetCamera();
        if (head == null)
        {
            report.AppendLine("No active camera found; nearby inspection skipped.");
            return;
        }

        report.AppendLine("Camera name=" + Clean(head.name) +
                          " stereo=" + head.stereoEnabled +
                          ". Distances are relative to headset (no world coordinates).");
        var nearby = new List<NearbyRenderer>();
        int inspected = 0, errors = 0;
        Vector3 origin = head.transform.position;
        Vector3 facing = head.transform.forward;
        float radius = Plugin.NearbyScanRadius.Value;

        try
        {
            foreach (Renderer renderer in Resources.FindObjectsOfTypeAll<Renderer>())
            {
                if (++inspected > MaxRendererObjects) break;
                if (renderer == null) continue;
                try
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                        !Live(renderer.gameObject)) continue;
                    Vector3 center = renderer.bounds.center;
                    Vector3 offset = center - origin;
                    float distance = offset.magnitude;
                    if (distance < 0.15f || distance > radius) continue;
                    if (Vector3.Dot(facing, offset / distance) < -0.2f) continue;
                    // Ignore enormous rooms, terrain and background structures.
                    Vector3 size = renderer.bounds.size;
                    if (size.x > 1.8f || size.y > 1.8f || size.z > 1.8f) continue;
                    nearby.Add(new NearbyRenderer
                    {
                        Renderer = renderer,
                        Distance = distance,
                        Forward = Vector3.Dot(facing, offset / distance)
                    });
                }
                catch { errors++; }
            }
        }
        catch (Exception ex) { report.AppendLine("Renderer enumeration: " + Clean(ex.Message)); }

        nearby.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        int shown = 0;
        var seenPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (NearbyRenderer item in nearby)
        {
            if (shown >= MaxNearbyRows) break;
            try
            {
                Renderer renderer = item.Renderer;
                string path = PathOf(renderer.transform, 7);
                if (!seenPaths.Add(path)) continue;
                shown++;
                string mesh = "(no MeshFilter)";
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    mesh = Clean(filter.sharedMesh.name);

                report.Append("NEAR d=").Append(item.Distance.ToString("0.00"))
                    .Append("m front=").Append(item.Forward.ToString("0.00"))
                    .Append(" path=").Append(path)
                    .Append(" mesh=").Append(mesh)
                    .Append(" components=").Append(Components(renderer.gameObject, 7));

                Transform? parent = renderer.transform.parent;
                if (parent != null)
                    report.Append(" parentTypes=").Append(Components(parent.gameObject, 7));
                report.AppendLine();
            }
            catch { errors++; }
        }
        report.Append("Renderers inspected=").Append(inspected)
            .Append(" nearby=").Append(nearby.Count)
            .Append(" printed=").Append(shown)
            .Append(" errors=").AppendLine(errors.ToString());
        report.AppendLine();
    }

    private void Capture()
    {
        _scanning = true;
        _captureCount++;
        try
        {
            var report = new StringBuilder(50000);
            report.AppendLine("===============================================================");
            report.Append("CAPTURE ").Append(_captureCount)
                .Append(" UTC ").Append(DateTimeOffset.UtcNow.ToString("O")).AppendLine();
            report.AppendLine("Player should record separately: class, equipped abilities, and which is being held.");
            report.AppendLine("Read-only: no ability state, account text or world coordinates are read.");
            report.AppendLine();

            ScanNamedObjects(report);
            ScanScripts(report);
            ScanNearbyRenderers(report);

            report.AppendLine("END CAPTURE " + _captureCount);
            report.AppendLine();
            File.AppendAllText(_reportPath, report.ToString());
            Plugin.Logger.LogInfo("[AbilityLabels] Capture " + _captureCount +
                " saved to " + _reportPath +
                " (" + report.Length + " characters).");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("[AbilityLabels] Snapshot failed: " + ex);
        }
        finally { _scanning = false; }
    }
}

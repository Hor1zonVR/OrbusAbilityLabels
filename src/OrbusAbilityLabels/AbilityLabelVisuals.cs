using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbusAbilityLabels;

/// <summary>
/// World-space visual prototype. No Harmony patches, network calls,
/// scene-wide scans, original orb edits, or altered controller interactions.
/// </summary>
public sealed class AbilityLabelVisuals : MonoBehaviour
{
    public AbilityLabelVisuals(IntPtr pointer) : base(pointer) { }

    private sealed class Label
    {
        public GameObject Root = null!;
        public TextMesh Front = null!;
        public TextMesh Shadow = null!;
        public Transform? Target;
        public string Caption = "";
    }

    private static readonly string[] KnownOrbs =
    {
        "ShieldOrb(Clone)", "RenewOrb(Clone)",
        "GravityOrb(Clone)", "BigHealOrb(Clone)"
    };

    private readonly Dictionary<string, Label> _labels = new(StringComparer.Ordinal);
    private Transform? _rig;
    private Camera? _eye;
    private Font? _font;
    private float _nextRefresh;
    private float _lastError;

    private void Start()
    {
        try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("[AbilityLabels] Font unavailable: " + ex.Message);
        }
        Plugin.Logger.LogInfo("[AbilityLabels] v0.2.2: compact labels with Bard mesh-name mapping (8 abilities).");
        Plugin.Logger.LogInfo("[AbilityLabels] No global object enumeration. No gameplay modifications.");
    }

    private static Transform? Child(Transform? parent, string path)
    {
        if (parent == null) return null;
        try { return parent.Find(path); }
        catch { return null; }
    }

    private Camera? FindEye()
    {
        try
        {
            foreach (Camera camera in Camera.allCameras)
                if (camera != null && camera.isActiveAndEnabled &&
                    camera.stereoEnabled && camera.name == "Camera (eye)")
                    return camera;
        }
        catch { }
        return null;
    }

    private void Update()
    {
        if (Time.realtimeSinceStartup < _nextRefresh) return;
        _nextRefresh = Time.realtimeSinceStartup + 0.35f;
        try { Refresh(); }
        catch (Exception ex)
        {
            if (Time.realtimeSinceStartup - _lastError > 10f)
            {
                _lastError = Time.realtimeSinceStartup;
                Plugin.Logger.LogWarning("[AbilityLabels] Refresh failed: " + ex.Message);
            }
        }
    }

    private void Refresh()
    {
        foreach (Label label in _labels.Values) label.Target = null;
        if (!Plugin.Enabled.Value) return;

        GameObject player = GameObject.Find("PlayerRigv4");
        if (player == null) return;
        _rig = player.transform;
        _eye = FindEye();
        if (_eye == null) return;

        Transform? notes = Child(_rig, "Avatar/Bard(Clone)/Instruments/NotesContainer");
        if (Plugin.ShowBard.Value && notes != null && notes.gameObject.activeInHierarchy)
            BindBard(notes);

        Transform? gun = Child(_rig, "Controller (right)/OrbLauncher");
        if (Plugin.ShowMusketeer.Value && gun != null && gun.gameObject.activeInHierarchy)
            BindMusketeer(gun);
    }

    private void BindBard(Transform notes)
    {
        for (int i = 0; i < 6; i++)
        {
            string slotName = i == 0 ? "EquippedSlot1" : "EquippedSlot1 (" + i + ")";
            Transform? slot = Child(notes, slotName);
            if (slot == null) continue;

            for (int j = 0; j < Math.Min(slot.childCount, 8); j++)
            {
                Transform item = slot.GetChild(j);
                if (item == null || !item.gameObject.activeInHierarchy) continue;
                string name = item.name;
                if (!name.StartsWith("Instrument", StringComparison.Ordinal) ||
                    !name.EndsWith("(Clone)", StringComparison.Ordinal)) continue;

                // Bind by the current rendered MESH, not slot or InstrumentN.
                // That also works for the two newly identified defense meshes.
                MeshFilter filter = item.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                {
                    string meshName = filter.sharedMesh.name;
                    if (Plugin.BardMeshNames.TryGetValue(meshName, out var config))
                        Bind("Bard." + i, item, config.Value);
                }
                break;
            }
        }
    }

    private void BindMusketeer(Transform gun)
    {
        for (int i = 0; i < KnownOrbs.Length; i++)
        {
            string name = KnownOrbs[i];
            if (!Plugin.MusketeerNames.TryGetValue(name, out var config)) continue;
            // Only four exact named lookups periodically; never a full scan.
            GameObject orb = GameObject.Find(name);
            if (orb == null || !orb.activeInHierarchy) continue;
            if (Vector3.Distance(gun.position, orb.transform.position) > 1.0f)
                continue;
            if (Vector3.Distance(_eye!.transform.position, orb.transform.position) > 1.45f)
                continue;
            Bind("Musketeer." + i, orb.transform, config.Value);
        }
    }

    private void Bind(string key, Transform target, string raw)
    {
        string caption = (raw ?? "").Trim();
        if (caption.Length == 0) return;
        if (caption.Length > 5) caption = caption.Substring(0, 5);

        if (!_labels.TryGetValue(key, out Label? label))
        {
            label = CreateLabel(key);
            if (label == null) return;
            _labels.Add(key, label);
        }
        label.Target = target;
        if (label.Caption != caption)
        {
            label.Caption = caption;
            label.Front.text = caption;
            label.Shadow.text = caption;
        }
    }

    private Label? CreateLabel(string key)
    {
        if (_rig == null) return null;
        try
        {
            var root = new GameObject("AbilityLabels." + key);
            root.layer = 0;
            root.transform.SetParent(_rig, false);
            TextMesh front = root.AddComponent<TextMesh>();
            SetupText(front, new Color(1f, 0.96f, 0.82f, 1f));

            var backing = new GameObject("Shadow");
            backing.layer = 0;
            backing.transform.SetParent(root.transform, false);
            backing.transform.localPosition = new Vector3(-0.00065f, -0.00065f, -0.0006f);
            TextMesh shadow = backing.AddComponent<TextMesh>();
            SetupText(shadow, new Color(0.05f, 0.04f, 0.06f, 0.96f));

            root.SetActive(false);
            return new Label { Root = root, Front = front, Shadow = shadow };
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("[AbilityLabels] Label creation failed: " + ex.Message);
            return null;
        }
    }

    private void SetupText(TextMesh t, Color color)
    {
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.fontSize = 48;
        t.characterSize = Plugin.TextCharacterSize.Value;
        t.fontStyle = FontStyle.Bold;
        t.color = color;
        if (_font != null)
        {
            t.font = _font;
            MeshRenderer renderer = t.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = _font.material;
        }
    }

    private void LateUpdate()
    {
        Camera? eye = _eye;
        bool enabled = Plugin.Enabled.Value && eye != null && eye.isActiveAndEnabled;
        Vector3 head = enabled ? eye!.transform.position : Vector3.zero;

        foreach (Label label in _labels.Values)
        {
            Transform? target = label.Target;
            bool visible = enabled && target != null && target.gameObject.activeInHierarchy;
            if (visible)
            {
                try
                {
                    Vector3 basePosition = target!.position +
                        Vector3.up * Plugin.VerticalOffset.Value;
                    Vector3 towardHead = head - basePosition;
                    float distance = towardHead.magnitude;
                    if (distance < 0.17f || distance > 1.6f)
                        visible = false;
                    else
                    {
                        Vector3 position = basePosition +
                            towardHead / distance * Plugin.TowardEyeOffset.Value;
                        Transform t = label.Root.transform;
                        t.position = position;
                        t.rotation = Quaternion.LookRotation(position - head, Vector3.up);
                        t.localScale = Vector3.one * Plugin.LabelScale.Value;
                    }
                }
                catch { visible = false; }
            }
            if (label.Root != null && label.Root.activeSelf != visible)
                label.Root.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        foreach (Label label in _labels.Values)
            if (label.Root != null) UnityEngine.Object.Destroy(label.Root);
        _labels.Clear();
    }
}

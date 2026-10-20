using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using UnityEngine;

namespace OrbusAbilityLabels;

/// <summary>
/// Temporary read-only Bard mesh identification panel.
/// Scans ONLY six children of the confirmed local Bard NotesContainer.
/// Never searches the global scene or reads game networking / save data.
/// </summary>
public sealed class BardMeshPanel : MonoBehaviour
{
    public BardMeshPanel(IntPtr pointer) : base(pointer) { }

    private const int Slots = 6;
    private const int MaxChildrenPerSlot = 10;
    private const int MaxSnapshots = 120;

    private float _nextRefresh;
    private float _lastError;
    private string _previousState = "";
    private string _reportPath = "";
    private int _snapshots;
    private Camera? _eye;
    private Transform? _notes;
    private GameObject? _panelRoot;
    private TextMesh? _text;
    private TextMesh? _shadow;
    private Font? _font;

    private void Start()
    {
        _reportPath = Path.Combine(Paths.BepInExRootPath,
            "AbilityLabelsBardMap.txt");
        try
        {
            File.WriteAllText(_reportPath,
                "OrbusAbilityLabels v0.2.2 - Bard equipped instrument snapshots" +
                Environment.NewLine +
                "InstrumentN and mesh ID are Unity object identifiers, NOT verified ability names." +
                Environment.NewLine +
                "Swap one Bard ability at a time; this file records changed equipment automatically." +
                Environment.NewLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("[AbilityLabels] Bard mapping file unavailable: " +
                ex.Message);
        }

        try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("[AbilityLabels] Bard panel font unavailable: " +
                ex.Message);
        }
        Plugin.Logger.LogInfo("[AbilityLabels] Bard mesh inspector ready; live panel only with Bard equipped.");
        Plugin.Logger.LogInfo("[AbilityLabels] Bard map log: BepInEx/AbilityLabelsBardMap.txt");
    }

    private static Transform? TryFind(Transform? root, string path)
    {
        if (root == null) return null;
        try { return root.Find(path); }
        catch { return null; }
    }

    private Camera? FindEye()
    {
        try
        {
            foreach (Camera camera in Camera.allCameras)
            {
                if (camera != null && camera.isActiveAndEnabled &&
                    camera.stereoEnabled && camera.name == "Camera (eye)")
                    return camera;
            }
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
                Plugin.Logger.LogWarning("[AbilityLabels] Bard mesh panel error: " +
                    ex.Message);
            }
        }
    }

    private void Refresh()
    {
        if (!Plugin.ShowBardMeshPanel.Value)
        {
            _notes = null;
            SetVisible(false);
            return;
        }

        // Root is the *local* PlayerRigv4, never a remote player or mirror.
        GameObject player = GameObject.Find("PlayerRigv4");
        Transform? root = player != null ? player.transform : null;
        Transform? notes = TryFind(root,
            "Avatar/Bard(Clone)/Instruments/NotesContainer");

        if (notes == null || !notes.gameObject.activeInHierarchy)
        {
            _notes = null;
            _previousState = "";
            SetVisible(false);
            return;
        }

        _eye = FindEye();
        if (_eye == null)
        {
            _notes = null;
            SetVisible(false);
            return;
        }

        _notes = notes;
        EnsurePanel(root!);
        var text = new StringBuilder(256);
        var state = new StringBuilder(200);
        text.AppendLine("BARD MESHES");
        text.AppendLine("SLOT  ID   MESH");

        for (int index = 0; index < Slots; index++)
        {
            string slotName = index == 0
                ? "EquippedSlot1"
                : "EquippedSlot1 (" + index + ")";
            Transform? slot = TryFind(notes, slotName);
            int id = -1;
            string mesh = "(empty)";

            if (slot != null && slot.gameObject.activeInHierarchy)
            {
                int children = Math.Min(slot.childCount, MaxChildrenPerSlot);
                for (int childIndex = 0; childIndex < children; childIndex++)
                {
                    Transform candidate = slot.GetChild(childIndex);
                    if (candidate == null || !candidate.gameObject.activeInHierarchy)
                        continue;
                    string name = candidate.name;
                    if (!name.StartsWith("Instrument", StringComparison.Ordinal) ||
                        !name.EndsWith("(Clone)", StringComparison.Ordinal))
                        continue;

                    const int prefix = 10;  // "Instrument"
                    const int suffix = 7;   // "(Clone)"
                    string number = name.Substring(prefix,
                        name.Length - prefix - suffix);
                    if (!int.TryParse(number, out id))
                    {
                        id = -1;
                        continue;
                    }

                    mesh = "(no mesh)";
                    MeshFilter filter = candidate.GetComponent<MeshFilter>();
                    if (filter != null && filter.sharedMesh != null)
                        mesh = filter.sharedMesh.name;
                    break;
                }
            }

            string idText = id < 0 ? "--" : "I" + id;
            text.Append(index + 1).Append("      ")
                .Append(idText).Append("   ")
                .AppendLine(mesh);
            state.Append(index + 1).Append(':')
                .Append(idText).Append(':').Append(mesh).Append('|');
        }

        text.Append("Optional debug view");
        string current = state.ToString();

        if (_text != null && _text.text != text.ToString())
        {
            _text.text = text.ToString();
            if (_shadow != null) _shadow.text = _text.text;
        }

        if (current != _previousState)
        {
            _previousState = current;
            RecordChange(text.ToString());
        }
    }

    private void RecordChange(string rows)
    {
        if (_snapshots >= MaxSnapshots) return;
        _snapshots++;
        try
        {
            File.AppendAllText(_reportPath,
                "=== BARD LOADOUT " + _snapshots +
                " at UTC " + DateTimeOffset.UtcNow.ToString("O") +
                " ===" + Environment.NewLine +
                rows + Environment.NewLine + Environment.NewLine);
            Plugin.Logger.LogInfo("[AbilityLabels] Bard loadout snapshot " +
                _snapshots + " saved (change detected).");
        }
        catch (Exception ex)
        {
            if (_snapshots == 1)
                Plugin.Logger.LogWarning("[AbilityLabels] Bard loadout write failed: " +
                    ex.Message);
        }
    }

    private void EnsurePanel(Transform root)
    {
        if (_panelRoot != null && _text != null) return;
        var panel = new GameObject("AbilityLabels.BardMeshInspector");
        panel.layer = 0;
        panel.transform.SetParent(root, false);
        TextMesh text = panel.AddComponent<TextMesh>();
        Configure(text, new Color(1f, 0.96f, 0.81f, 1f));

        var shadowObject = new GameObject("BardMeshInspectorShadow");
        shadowObject.layer = 0;
        shadowObject.transform.SetParent(panel.transform, false);
        shadowObject.transform.localPosition =
            new Vector3(-0.00065f, -0.00065f, -0.0006f);
        TextMesh shadow = shadowObject.AddComponent<TextMesh>();
        Configure(shadow, new Color(0.03f, 0.025f, 0.045f, 0.98f));

        _panelRoot = panel;
        _text = text;
        _shadow = shadow;
        panel.SetActive(false);
    }

    private void Configure(TextMesh t, Color color)
    {
        t.anchor = TextAnchor.UpperLeft;
        t.alignment = TextAlignment.Left;
        t.fontSize = 40;
        t.characterSize = Plugin.BardPanelCharacterSize.Value;
        t.fontStyle = FontStyle.Bold;
        t.lineSpacing = 1.05f;
        t.color = color;
        if (_font == null) return;
        t.font = _font;
        MeshRenderer renderer = t.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.sharedMaterial = _font.material;
    }

    private void LateUpdate()
    {
        bool visible = Plugin.ShowBardMeshPanel.Value &&
            _panelRoot != null && _notes != null &&
            _notes.gameObject.activeInHierarchy &&
            _eye != null && _eye.isActiveAndEnabled;

        if (visible)
        {
            try
            {
                Transform notes = _notes!;
                Camera eye = _eye!;
                // Position beside, and slightly above, the physical Bard row.
                // It does NOT follow head movement as a HUD.
                Vector3 pos = notes.position +
                    notes.right * Plugin.BardPanelSideOffset.Value +
                    Vector3.up * Plugin.BardPanelHeight.Value;

                float distance = Vector3.Distance(eye.transform.position, pos);
                if (distance > 2f || distance < 0.25f)
                {
                    visible = false;
                }
                else
                {
                    Transform transform = _panelRoot!.transform;
                    transform.position = pos;
                    transform.rotation = Quaternion.LookRotation(
                        pos - eye.transform.position, Vector3.up);
                    transform.localScale = Vector3.one *
                        Plugin.BardPanelScale.Value;
                }
            }
            catch { visible = false; }
        }
        SetVisible(visible);
    }

    private void SetVisible(bool show)
    {
        if (_panelRoot != null && _panelRoot.activeSelf != show)
            _panelRoot.SetActive(show);
    }

    private void OnDestroy()
    {
        if (_panelRoot != null)
            UnityEngine.Object.Destroy(_panelRoot);
        _panelRoot = null;
        _text = null;
        _shadow = null;
    }
}

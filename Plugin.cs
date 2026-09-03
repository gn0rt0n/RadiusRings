using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace RadiusRings;

[BepInPlugin(Guid, "Radius Rings", Version)]
public class Plugin : BaseUnityPlugin
{
    private const string Guid = "EleventhTower.valheim.radiusrings";
    private const string Version = "1.0.0";

    private static readonly Color RingColor = new Color(0.25f, 0.9f, 1f, 0.85f);
    private static readonly Color HighlightColor = new Color(1f, 0.78f, 0.15f, 0.95f);
    private const float RingWidth = 0.06f;
    private const float HighlightWidth = 0.11f;
    private const float GroundOffset = 0.05f;
    private const float LabelHeight = 0.55f;

    private ConfigEntry<KeyboardShortcut> _toggleKey;
    private ConfigEntry<float> _maxRadius;
    private ConfigEntry<float> _ringSpacing;
    private ConfigEntry<int> _segments;
    private ConfigEntry<float> _updateInterval;
    private ConfigEntry<int> _highlightEvery;

    private bool _visible;
    private GameObject _root;
    private readonly List<LineRenderer> _rings = new();
    private readonly List<TextMesh> _labels = new();
    private float _nextRefresh;

    private void Awake()
    {
        _toggleKey = Config.Bind("General", "Toggle Key",
            new KeyboardShortcut(KeyCode.R, KeyCode.RightAlt),
            "Shows or hides the distance rings around your character.");
        _maxRadius = Config.Bind("General", "Max Radius", 50f,
            new ConfigDescription("Rings are drawn out to this distance, in metres.",
                new AcceptableValueRange<float>(5f, 500f)));
        _ringSpacing = Config.Bind("General", "Ring Spacing", 5f,
            new ConfigDescription("Distance between rings, in metres.",
                new AcceptableValueRange<float>(1f, 50f)));
        _segments = Config.Bind("General", "Ring Segments", 64,
            new ConfigDescription("Points per ring. Higher is smoother but costs more per refresh.",
                new AcceptableValueRange<int>(12, 128)));
        _updateInterval = Config.Bind("General", "Update Interval", 0.15f,
            new ConfigDescription("Seconds between ring refreshes while visible. Lower is smoother but costs more.",
                new AcceptableValueRange<float>(0.02f, 1f)));
        _highlightEvery = Config.Bind("General", "Highlight Every", 2,
            "Every Nth ring (counting from the innermost) is drawn brighter and thicker, "
            + "e.g. 2 highlights 10m/20m/30m... when spacing is 5m. 0 disables highlighting.");

        _maxRadius.SettingChanged += (_, _) => RebuildIfVisible();
        _ringSpacing.SettingChanged += (_, _) => RebuildIfVisible();
        _segments.SettingChanged += (_, _) => RebuildIfVisible();
        _highlightEvery.SettingChanged += (_, _) => RebuildIfVisible();
    }

    private void Update()
    {
        if (_toggleKey.Value.IsDown())
        {
            _visible = !_visible;
            if (_visible)
            {
                RebuildRings();
            }
            else if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        if (!_visible || Player.m_localPlayer == null)
        {
            return;
        }

        if (_root == null)
        {
            RebuildRings();
        }

        if (Time.time < _nextRefresh)
        {
            return;
        }

        _nextRefresh = Time.time + _updateInterval.Value;
        RefreshRings();
    }

    private void RebuildIfVisible()
    {
        if (_visible)
        {
            RebuildRings();
        }
    }

    private void RebuildRings()
    {
        if (_root != null)
        {
            Destroy(_root);
        }

        _rings.Clear();
        _labels.Clear();

        _root = new GameObject("RadiusRings");
        _root.SetActive(true);

        Material material = new Material(Shader.Find("Sprites/Default"));
        int segments = _segments.Value;
        float spacing = _ringSpacing.Value;
        int ringCount = Mathf.Max(1, Mathf.RoundToInt(_maxRadius.Value / spacing));

        for (int i = 1; i <= ringCount; i++)
        {
            bool highlighted = _highlightEvery.Value > 0 && i % _highlightEvery.Value == 0;

            GameObject ringObject = new GameObject($"Ring_{i}");
            ringObject.transform.SetParent(_root.transform, worldPositionStays: false);
            LineRenderer line = ringObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = segments + 1;
            line.material = material;
            line.widthMultiplier = highlighted ? HighlightWidth : RingWidth;
            Color color = highlighted ? HighlightColor : RingColor;
            line.startColor = color;
            line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            _rings.Add(line);

            GameObject labelObject = new GameObject($"Label_{i}");
            labelObject.transform.SetParent(_root.transform, worldPositionStays: false);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = $"{i * spacing:0}m";
            label.characterSize = 0.15f;
            label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            _labels.Add(label);
        }

        RefreshRings();
    }

    private void RefreshRings()
    {
        Player player = Player.m_localPlayer;
        if (player == null || _root == null)
        {
            return;
        }

        Vector3 center = player.transform.position;
        center.y = 0f;
        float spacing = _ringSpacing.Value;
        int segments = _segments.Value;
        Camera cam = Camera.main;

        for (int i = 0; i < _rings.Count; i++)
        {
            float radius = (i + 1) * spacing;
            LineRenderer line = _rings[i];
            for (int j = 0; j <= segments; j++)
            {
                float angle = j / (float)segments * Mathf.PI * 2f;
                float x = center.x + Mathf.Sin(angle) * radius;
                float z = center.z + Mathf.Cos(angle) * radius;
                Vector3 point = new Vector3(x, center.y, z);
                if (!ZoneSystem.instance.GetGroundHeight(point, out float height))
                {
                    height = player.transform.position.y;
                }
                point.y = height + GroundOffset;
                line.SetPosition(j, point);
            }

            TextMesh label = _labels[i];
            Vector3 labelPoint = new Vector3(center.x, center.y, center.z + radius);
            if (!ZoneSystem.instance.GetGroundHeight(labelPoint, out float labelHeight))
            {
                labelHeight = player.transform.position.y;
            }
            labelPoint.y = labelHeight + LabelHeight;
            label.transform.position = labelPoint;
            if (cam != null)
            {
                label.transform.forward = cam.transform.forward;
            }
        }
    }
}

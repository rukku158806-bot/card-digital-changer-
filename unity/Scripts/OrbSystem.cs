using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages 8 elemental orbs that orbit the player cube.
/// Orbs are created at runtime as child GameObjects using simple coloured sprites
/// (or an assigned prefab).  Orbs pulse in scale when selected.
/// </summary>
public class OrbSystem : MonoBehaviour
{
    // ── Orb data class ────────────────────────────────────────────────────────

    [Serializable]
    public class OrbData
    {
        public string    name;
        public Color     color;
        [HideInInspector] public GameObject go;
        [HideInInspector] public float      angleOffset;  // current orbit angle in degrees
        [HideInInspector] public bool       isSelected;
        [HideInInspector] public float      pulseTime;    // accumulator for sin-pulse
    }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Orb Prefab (leave null for procedural circle)")]
    public GameObject orbPrefab;

    [Header("Orbit Parameters")]
    public float orbitRadius = GameConstants.ORB_ORBIT_RADIUS;
    public float orbitSpeed  = GameConstants.ORB_ORBIT_SPEED;   // deg/sec

    // ── Internal state ────────────────────────────────────────────────────────
    private readonly List<OrbData> _orbs         = new List<OrbData>();
    private readonly List<OrbData> _selectedOrbs = new List<OrbData>();
    private bool                   _selectionMode;

    // Static orb definitions — name + colour
    private static readonly (string name, Color color)[] OrbDefs =
    {
        ("Fire",      new Color(1.00f, 0.35f, 0.05f)),
        ("Water",     new Color(0.10f, 0.55f, 1.00f)),
        ("Nature",    new Color(0.20f, 0.80f, 0.20f)),
        ("Lightning", new Color(1.00f, 0.95f, 0.10f)),
        ("Dark",      new Color(0.40f, 0.10f, 0.60f)),
        ("Crystal",   new Color(0.60f, 0.90f, 1.00f)),
        ("Earth",     new Color(0.55f, 0.35f, 0.10f)),
        ("Light",     new Color(1.00f, 1.00f, 0.85f)),
    };

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        float angleStep = 360f / OrbDefs.Length;

        for (int i = 0; i < OrbDefs.Length; i++)
        {
            (string orbName, Color orbColor) = OrbDefs[i];

            OrbData data = new OrbData
            {
                name        = orbName,
                color       = orbColor,
                angleOffset = i * angleStep,
                isSelected  = false,
                pulseTime   = 0f,
            };

            data.go = CreateOrbGO(orbName, orbColor);
            _orbs.Add(data);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        foreach (OrbData orb in _orbs)
        {
            // Advance orbit angle
            orb.angleOffset += orbitSpeed * dt;

            float rad = orb.angleOffset * Mathf.Deg2Rad;
            float x   = Mathf.Cos(rad) * orbitRadius;
            float y   = Mathf.Sin(rad) * orbitRadius;
            orb.go.transform.localPosition = new Vector3(x, y, -0.1f);

            // Scale pulse when selected
            if (orb.isSelected)
            {
                orb.pulseTime += dt * GameConstants.ORB_PULSE_SPEED;
                float pulse = 1f + (Mathf.Sin(orb.pulseTime) * 0.5f + 0.5f)
                                   * (GameConstants.ORB_PULSE_SCALE - 1f);
                orb.go.transform.localScale = Vector3.one * 0.25f * pulse;
            }
            else
            {
                orb.go.transform.localScale = Vector3.one * 0.25f;
                orb.pulseTime = 0f;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Enter selection mode; clears any previous selection.</summary>
    public void StartCombatSelection()
    {
        _selectionMode = true;
        _selectedOrbs.Clear();
        foreach (OrbData orb in _orbs) orb.isSelected = false;
    }

    /// <summary>Toggle selection on an OrbData instance (max 2 at a time).</summary>
    public void SelectOrb(OrbData orb)
    {
        if (!_selectionMode || orb == null) return;

        if (orb.isSelected)
        {
            orb.isSelected = false;
            _selectedOrbs.Remove(orb);
            return;
        }

        if (_selectedOrbs.Count >= 2) return;   // already 2 selected

        orb.isSelected = true;
        _selectedOrbs.Add(orb);
    }

    /// <summary>Select an orb by element name (case-insensitive convenience overload).</summary>
    public void SelectOrbByName(string elementName)
    {
        OrbData orb = _orbs.Find(
            o => string.Equals(o.name, elementName, StringComparison.OrdinalIgnoreCase));
        if (orb != null) SelectOrb(orb);
    }

    /// <summary>
    /// Confirm the current combo selection.
    /// Returns (multiplier, comboName) or (0, "") if fewer than 2 orbs are selected.
    /// Resets selection state.
    /// </summary>
    public (float multiplier, string comboName) ConfirmCombo()
    {
        _selectionMode = false;

        if (_selectedOrbs.Count < 2)
        {
            foreach (OrbData orb in _orbs) orb.isSelected = false;
            _selectedOrbs.Clear();
            return (0f, string.Empty);
        }

        string nameA = _selectedOrbs[0].name;
        string nameB = _selectedOrbs[1].name;

        (float multiplier, string comboName) result = ComboCalculator.Calculate(nameA, nameB);

        foreach (OrbData orb in _orbs) orb.isSelected = false;
        _selectedOrbs.Clear();

        return result;
    }

    /// <summary>Read-only access to all OrbData entries (for UI display).</summary>
    public IReadOnlyList<OrbData> GetOrbs() => _orbs.AsReadOnly();

    // ── Internal helpers ──────────────────────────────────────────────────────

    private GameObject CreateOrbGO(string orbName, Color col)
    {
        GameObject go;

        if (orbPrefab != null)
        {
            go = Instantiate(orbPrefab, transform);
        }
        else
        {
            go = new GameObject($"Orb_{orbName}");
            go.transform.SetParent(transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = CreateCircleSprite();
            sr.color        = col;
            sr.sortingOrder = 5;
        }

        go.name = $"Orb_{orbName}";
        go.transform.localScale = Vector3.one * 0.25f;

        // 2D point light for glow (URP / 2D Renderer required)
        try
        {
            var light2D = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light2D.color                = col;
            light2D.intensity            = 1.5f;
            light2D.pointLightOuterRadius = 0.4f;
        }
        catch
        {
            // URP not available — silently skip the light component
        }

        return go;
    }

    /// <summary>Procedurally generate a filled circle sprite (64×64 px).</summary>
    private static Sprite CreateCircleSprite()
    {
        const int size = 64;
        Texture2D tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float     r    = size * 0.5f;
        Vector2   ctr  = new Vector2(r, r);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x, y), ctr);
                float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(r - 1.5f, r, dist));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

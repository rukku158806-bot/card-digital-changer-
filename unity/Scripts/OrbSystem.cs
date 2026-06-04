using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages 8 elemental orbs that orbit the player cube.
/// Orbs are created at runtime as child GameObjects using simple coloured sprites.
/// </summary>
public class OrbSystem : MonoBehaviour
{
    // ── Orb data ──────────────────────────────────────────────────────────────

    [Serializable]
    public class OrbData
    {
        public string    name;
        public Color     color;
        [HideInInspector] public GameObject go;
        [HideInInspector] public float      angleOffset;  // degrees
        [HideInInspector] public bool       isSelected;
        [HideInInspector] public float      pulseTime;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Orb Prefab (optional — a plain circle sprite is created if null)")]
    public GameObject orbPrefab;

    [Header("Orbit Parameters")]
    public float orbitRadius = GameConstants.ORB_ORBIT_RADIUS;
    public float orbitSpeed  = GameConstants.ORB_ORBIT_SPEED;   // deg/sec

    // ── Private state ─────────────────────────────────────────────────────────
    private List<OrbData>  _orbs          = new List<OrbData>();
    private List<OrbData>  _selectedOrbs  = new List<OrbData>();
    private bool           _selectionMode = false;

    // Base orb definitions
    private static readonly (string name, Color color)[] OrbDefs =
    {
        ("Fire",      new Color(1.0f, 0.35f, 0.05f)),
        ("Water",     new Color(0.1f, 0.55f, 1.0f )),
        ("Nature",    new Color(0.2f, 0.8f,  0.2f )),
        ("Lightning", new Color(1.0f, 0.95f, 0.1f )),
        ("Dark",      new Color(0.4f, 0.1f,  0.6f )),
        ("Crystal",   new Color(0.6f, 0.9f,  1.0f )),
        ("Earth",     new Color(0.55f,0.35f, 0.1f )),
        ("Light",     new Color(1.0f, 1.0f,  0.85f)),
    };

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        float angleStep = 360f / OrbDefs.Length;
        for (int i = 0; i < OrbDefs.Length; i++)
        {
            var (orbName, orbColor) = OrbDefs[i];

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
        float delta = Time.deltaTime;
        for (int i = 0; i < _orbs.Count; i++)
        {
            OrbData orb = _orbs[i];

            // Advance orbit angle
            orb.angleOffset += orbitSpeed * delta;

            float rad = orb.angleOffset * Mathf.Deg2Rad;
            float x   = Mathf.Cos(rad) * orbitRadius;
            float y   = Mathf.Sin(rad) * orbitRadius;
            orb.go.transform.localPosition = new Vector3(x, y, -0.1f);

            // Pulse selected orbs
            if (orb.isSelected)
            {
                orb.pulseTime += delta * GameConstants.ORB_PULSE_SPEED;
                float pulse = 1f + (Mathf.Sin(orb.pulseTime) * 0.5f + 0.5f)
                                   * (GameConstants.ORB_PULSE_SCALE - 1f);
                orb.go.transform.localScale = Vector3.one * pulse * 0.25f;
            }
            else
            {
                orb.go.transform.localScale = Vector3.one * 0.25f;
                orb.pulseTime = 0f;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Enter selection mode; up to 2 orbs may be selected for a combo.</summary>
    public void StartCombatSelection()
    {
        _selectionMode = true;
        _selectedOrbs.Clear();
        foreach (var orb in _orbs) orb.isSelected = false;
    }

    /// <summary>Toggle selection on an orb (max 2).</summary>
    public void SelectOrb(OrbData orb)
    {
        if (!_selectionMode) return;

        if (orb.isSelected)
        {
            orb.isSelected = false;
            _selectedOrbs.Remove(orb);
            return;
        }

        if (_selectedOrbs.Count >= 2) return;

        orb.isSelected = true;
        _selectedOrbs.Add(orb);
    }

    /// <summary>Select orb by name (convenience overload).</summary>
    public void SelectOrbByName(string name)
    {
        OrbData orb = _orbs.Find(o => o.name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (orb != null) SelectOrb(orb);
    }

    /// <summary>
    /// Confirm the current selection and calculate the combo result.
    /// Returns (multiplier, comboName).  Resets selection state.
    /// Returns (0, "") if fewer than 2 orbs are selected.
    /// </summary>
    public (float multiplier, string comboName) ConfirmCombo()
    {
        _selectionMode = false;

        if (_selectedOrbs.Count < 2) return (0f, string.Empty);

        string nameA = _selectedOrbs[0].name;
        string nameB = _selectedOrbs[1].name;

        var result = ComboCalculator.Calculate(nameA, nameB);

        // Deselect all
        foreach (var orb in _orbs) orb.isSelected = false;
        _selectedOrbs.Clear();

        return result;
    }

    /// <summary>Returns the list of all OrbData (read-only access for UI etc.).</summary>
    public IReadOnlyList<OrbData> GetOrbs() => _orbs;

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

            // Create a simple circle using a SpriteRenderer
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite         = CreateCircleSprite();
            sr.color          = col;
            sr.sortingOrder   = 5;
        }

        go.name = $"Orb_{orbName}";
        go.transform.localScale = Vector3.one * 0.25f;

        // Add a glow-style point light so orbs look vibrant in URP/2D
        var light2D = go.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light2D.color     = col;
        light2D.intensity = 1.5f;
        light2D.pointLightOuterRadius = 0.4f;

        return go;
    }

    // Generates a simple filled circle sprite procedurally.
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
                float dist = Vector2.Distance(new Vector2(x, y), ctr);
                float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(r - 1.5f, r, dist));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

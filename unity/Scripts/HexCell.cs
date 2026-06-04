using UnityEngine;

/// <summary>
/// Placed on every hex tile GameObject. Stores grid coordinates, zone flags,
/// and occupancy state. Also manages visual highlight states.
/// </summary>
public class HexCell : MonoBehaviour
{
    // ── Grid position ─────────────────────────────────────────────────────────
    public int col;
    public int row;

    // ── Zone flags ────────────────────────────────────────────────────────────
    public bool isPlayerZone;
    public bool isEnemyZone;

    // ── Occupancy ─────────────────────────────────────────────────────────────
    public bool       isOccupied { get; private set; }
    public GameObject occupant   { get; private set; }

    // ── Renderer cache ────────────────────────────────────────────────────────
    private SpriteRenderer _sr;
    private Color          _baseColor;

    private static readonly Color ColorReachable = new Color(0.2f, 0.6f, 1.0f, 0.55f);
    private static readonly Color ColorSelected  = new Color(1.0f, 0.9f, 0.2f, 0.75f);
    private static readonly Color ColorEnemy     = new Color(1.0f, 0.3f, 0.3f, 0.55f);

    // ── Highlight type ────────────────────────────────────────────────────────
    public enum HighlightType { Reachable, Selected, Enemy }

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Tint the tile to indicate its state.</summary>
    public void Highlight(HighlightType type = HighlightType.Reachable)
    {
        if (_sr == null) return;
        switch (type)
        {
            case HighlightType.Reachable: _sr.color = ColorReachable; break;
            case HighlightType.Selected:  _sr.color = ColorSelected;  break;
            case HighlightType.Enemy:     _sr.color = ColorEnemy;     break;
        }
    }

    /// <summary>Restore the original tile colour.</summary>
    public void Unhighlight()
    {
        if (_sr != null) _sr.color = _baseColor;
    }

    /// <summary>Register a GameObject as this cell's occupant.</summary>
    public void SetOccupant(GameObject go)
    {
        occupant   = go;
        isOccupied = go != null;
    }

    /// <summary>Remove occupant reference and mark cell as free.</summary>
    public void ClearOccupant()
    {
        occupant   = null;
        isOccupied = false;
    }
}

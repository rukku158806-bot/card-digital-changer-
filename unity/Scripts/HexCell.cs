using UnityEngine;

/// <summary>
/// Placed on every hex tile GameObject. Stores grid coordinates and occupant data.
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

    // Highlight colours
    private static readonly Color HighlightReachable = new Color(0.2f, 0.6f, 1f,  0.55f);
    private static readonly Color HighlightSelected  = new Color(1f,   0.9f, 0.2f,0.75f);
    private static readonly Color HighlightEnemy     = new Color(1f,   0.3f, 0.3f,0.55f);

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _sr        = GetComponent<SpriteRenderer>();
        if (_sr != null) _baseColor = _sr.color;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Tint the tile to show it is reachable.</summary>
    public void Highlight(HighlightType type = HighlightType.Reachable)
    {
        if (_sr == null) return;
        switch (type)
        {
            case HighlightType.Reachable: _sr.color = HighlightReachable; break;
            case HighlightType.Selected:  _sr.color = HighlightSelected;  break;
            case HighlightType.Enemy:     _sr.color = HighlightEnemy;     break;
        }
    }

    /// <summary>Restore original tile colour.</summary>
    public void Unhighlight()
    {
        if (_sr != null) _sr.color = _baseColor;
    }

    /// <summary>Register an occupant on this cell.</summary>
    public void SetOccupant(GameObject go)
    {
        occupant   = go;
        isOccupied = go != null;
    }

    /// <summary>Remove occupant from this cell.</summary>
    public void ClearOccupant()
    {
        occupant   = null;
        isOccupied = false;
    }

    public enum HighlightType { Reachable, Selected, Enemy }
}

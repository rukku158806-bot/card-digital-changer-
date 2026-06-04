using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and manages a 5-column × 4-row hex grid using even-row offset coordinates.
/// Columns 0–1  → player zone (PlayerTilePrefab)
/// Column  2    → neutral zone (NeutralTilePrefab)
/// Columns 3–4  → enemy zone  (EnemyTilePrefab)
///
/// Hover highlight: blue tint on reachable (player-zone, unoccupied) tiles.
/// </summary>
public class HexGrid : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Tile Prefabs")]
    public GameObject NeutralTilePrefab;
    public GameObject PlayerTilePrefab;
    public GameObject EnemyTilePrefab;

    [Header("Grid Dimensions")]
    public int cols = GameConstants.GRID_COLS;   // 5
    public int rows = GameConstants.GRID_ROWS;   // 4

    [Header("Hex Spacing")]
    [Tooltip("Horizontal distance between tile centres.")]
    public float hexWidth  = 1.1f;
    [Tooltip("Vertical distance between tile centres (before 0.75 stagger).")]
    public float hexHeight = 1.0f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private HexCell[,] _cells;
    private HexCell    _hoveredCell;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        BuildGrid();
    }

    // ── Grid construction ─────────────────────────────────────────────────────

    private void BuildGrid()
    {
        _cells = new HexCell[cols, rows];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                bool isPlayerZone = (c <= GameConstants.PLAYER_ZONE_MAX_COL);
                bool isEnemyZone  = (c >= GameConstants.ENEMY_ZONE_MIN_COL);

                GameObject prefab;
                if (isPlayerZone)      prefab = PlayerTilePrefab;
                else if (isEnemyZone)  prefab = EnemyTilePrefab;
                else                   prefab = NeutralTilePrefab;

                // Fall back to neutral if a prefab slot is not assigned
                if (prefab == null) prefab = NeutralTilePrefab;

                Vector3    worldPos = CellToWorld(c, r);
                GameObject tileGO  = Instantiate(prefab, worldPos, Quaternion.identity, transform);
                tileGO.name = $"Tile_{c}_{r}";

                HexCell cell = tileGO.GetComponent<HexCell>();
                if (cell == null) cell = tileGO.AddComponent<HexCell>();

                cell.col          = c;
                cell.row          = r;
                cell.isPlayerZone = isPlayerZone;
                cell.isEnemyZone  = isEnemyZone;

                _cells[c, r] = cell;
            }
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Returns the HexCell at (col, row), or null if out of bounds.</summary>
    public HexCell GetCell(int col, int row)
    {
        if (col < 0 || col >= cols || row < 0 || row >= rows) return null;
        return _cells[col, row];
    }

    /// <summary>Returns all valid neighbours of the given cell (up to 6).</summary>
    public List<HexCell> GetNeighbors(HexCell cell)
    {
        var result = new List<HexCell>();
        int c = cell.col;
        int r = cell.row;

        // Offset-coordinate neighbour directions differ for even/odd rows.
        // Using "even-row" offset: even rows are NOT shifted, odd rows shift right by 0.5.
        int[][] evenRowDirs =
        {
            new[] { +1,  0 }, new[] { -1,  0 },   // right, left
            new[] {  0, +1 }, new[] {  0, -1 },   // upper-right, lower-right
            new[] { -1, +1 }, new[] { -1, -1 },   // upper-left,  lower-left
        };
        int[][] oddRowDirs =
        {
            new[] { +1,  0 }, new[] { -1,  0 },
            new[] { +1, +1 }, new[] { +1, -1 },
            new[] {  0, +1 }, new[] {  0, -1 },
        };

        int[][] dirs = (r % 2 == 0) ? evenRowDirs : oddRowDirs;

        foreach (int[] d in dirs)
        {
            HexCell neighbour = GetCell(c + d[0], r + d[1]);
            if (neighbour != null) result.Add(neighbour);
        }
        return result;
    }

    /// <summary>Convert grid (col, row) to Unity world position.</summary>
    public Vector3 CellToWorld(int col, int row)
    {
        float xOffset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;
        float x = col * hexWidth + xOffset;
        float y = row * hexHeight * 0.75f;
        return new Vector3(x, y, 0f);
    }

    /// <summary>Find the HexCell whose centre is closest to a world position.</summary>
    public HexCell WorldToCell(Vector3 worldPos)
    {
        HexCell closest = null;
        float   minDist = float.MaxValue;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float d = Vector3.SqrMagnitude(worldPos - CellToWorld(c, r));
                if (d < minDist)
                {
                    minDist = d;
                    closest = _cells[c, r];
                }
            }
        }
        return closest;
    }

    /// <summary>Highlight a set of cells with the given type.</summary>
    public void HighlightCells(IEnumerable<HexCell> cells, HexCell.HighlightType type)
    {
        foreach (HexCell cell in cells) cell.Highlight(type);
    }

    /// <summary>Remove all tile highlights.</summary>
    public void ClearAllHighlights()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (_cells[c, r] != null) _cells[c, r].Unhighlight();
    }

    // ── Mouse hover highlight ─────────────────────────────────────────────────

    private void Update()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        HexCell cell = WorldToCell(mouseWorld);
        if (cell == _hoveredCell) return;

        if (_hoveredCell != null) _hoveredCell.Unhighlight();
        _hoveredCell = cell;

        if (_hoveredCell != null && _hoveredCell.isPlayerZone && !_hoveredCell.isOccupied)
            _hoveredCell.Highlight(HexCell.HighlightType.Reachable);
    }
}

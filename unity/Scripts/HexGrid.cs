using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and manages a 5-column × 4-row hex grid using even-row offset coordinates.
/// Columns 0-2 are the player zone; columns 3-4 are the enemy zone.
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

    [Header("Hex Size")]
    [Tooltip("Distance from tile centre to a flat edge (horizontal spacing).")]
    public float hexWidth  = 1.1f;
    public float hexHeight = 1.0f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private HexCell[,] _cells;

    // Track last hovered cell for un-highlighting
    private HexCell _hoveredCell;

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
                bool isPlayerZone = c < 3;
                bool isEnemyZone  = c >= 3;

                GameObject prefab = isPlayerZone ? PlayerTilePrefab
                                  : isEnemyZone  ? EnemyTilePrefab
                                  : NeutralTilePrefab;

                if (prefab == null) prefab = NeutralTilePrefab;

                Vector3 worldPos = CellToWorld(c, r);
                GameObject tileGO = Instantiate(prefab, worldPos, Quaternion.identity, transform);
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

    /// <summary>Returns the HexCell at (col, row) or null if out of bounds.</summary>
    public HexCell GetCell(int col, int row)
    {
        if (col < 0 || col >= cols || row < 0 || row >= rows) return null;
        return _cells[col, row];
    }

    /// <summary>Returns all valid neighbours of the given cell (up to 6).</summary>
    public List<HexCell> GetNeighbors(HexCell cell)
    {
        var neighbours = new List<HexCell>();
        int c = cell.col;
        int r = cell.row;

        // Offset-grid neighbour directions differ for even/odd rows.
        // Using "even-row" offset (rows where r%2==0 shift left).
        int[][] evenRowDirs = {
            new[]{+1, 0}, new[]{-1, 0},  // right, left
            new[]{ 0,+1}, new[]{ 0,-1},  // up-right, down-right (even)
            new[]{-1,+1}, new[]{-1,-1}   // up-left,  down-left  (even)
        };
        int[][] oddRowDirs = {
            new[]{+1, 0}, new[]{-1, 0},
            new[]{+1,+1}, new[]{+1,-1},
            new[]{ 0,+1}, new[]{ 0,-1}
        };

        int[][] dirs = (r % 2 == 0) ? evenRowDirs : oddRowDirs;

        foreach (var d in dirs)
        {
            HexCell neighbour = GetCell(c + d[0], r + d[1]);
            if (neighbour != null) neighbours.Add(neighbour);
        }
        return neighbours;
    }

    /// <summary>Convert grid (col, row) to Unity world position.</summary>
    public Vector3 CellToWorld(int col, int row)
    {
        float xOffset = (row % 2 == 0) ? 0f : hexWidth * 0.5f;
        float x = col * hexWidth + xOffset;
        float y = row * hexHeight * 0.75f;
        return new Vector3(x, y, 0f);
    }

    /// <summary>Find the HexCell closest to a world position.</summary>
    public HexCell WorldToCell(Vector3 worldPos)
    {
        HexCell closest = null;
        float   minDist = float.MaxValue;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float d = Vector3.Distance(worldPos, CellToWorld(c, r));
                if (d < minDist)
                {
                    minDist = d;
                    closest = _cells[c, r];
                }
            }
        }
        return closest;
    }

    // ── Hover highlight ───────────────────────────────────────────────────────

    private void Update()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        HexCell cell = WorldToCell(mouseWorld);

        if (cell == _hoveredCell) return;

        // Un-highlight previous
        if (_hoveredCell != null) _hoveredCell.Unhighlight();

        _hoveredCell = cell;

        // Only highlight reachable / player zone tiles
        if (_hoveredCell != null && _hoveredCell.isPlayerZone && !_hoveredCell.isOccupied)
            _hoveredCell.Highlight(HexCell.HighlightType.Reachable);
    }

    /// <summary>Highlight a specific set of cells (e.g. reachable from player position).</summary>
    public void HighlightCells(IEnumerable<HexCell> cells, HexCell.HighlightType type)
    {
        foreach (var c in cells) c.Highlight(type);
    }

    /// <summary>Clear all tile highlights.</summary>
    public void ClearAllHighlights()
    {
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                _cells[c, r].Unhighlight();
    }
}

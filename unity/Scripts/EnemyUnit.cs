using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour attached to every enemy GameObject.
/// Handles HP, movement toward the player, adjacency attacks, and a world-space HP bar.
///
/// Tag the prefab with one of: Triangle / Circle / Diamond / Pentagon / Star
/// Set enemyType in the Inspector to match.
/// </summary>
public class EnemyUnit : MonoBehaviour
{
    // ── Enemy type enum ───────────────────────────────────────────────────────
    public enum EnemyType { Grunt, Support, Crystal, Golem, Void }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Type & Stats")]
    public EnemyType enemyType = EnemyType.Grunt;
    public float     maxHp     = 50f;
    public float     damage    = GameConstants.ENEMY_DAMAGE;
    public float     moveSpeed = GameConstants.ENEMY_MOVE_SPEED;

    [Header("HP Bar (World Space Canvas — auto-created if null)")]
    public Slider hpBarSlider;

    // ── Runtime ───────────────────────────────────────────────────────────────
    public  float    currentHp  { get; private set; }
    public  HexCell  currentCell { get; private set; }
    private HexGrid  _grid;
    private bool     _isMoving;

    // ── Events ────────────────────────────────────────────────────────────────
    public event System.Action<EnemyUnit> OnDefeated;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        currentHp = maxHp;
        if (hpBarSlider == null) BuildHPBar();
        RefreshHPBar();
    }

    private void Start()
    {
        _grid = FindObjectOfType<HexGrid>();
        if (_grid != null)
        {
            currentCell = _grid.WorldToCell(transform.position);
            if (currentCell != null) currentCell.SetOccupant(gameObject);
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Place this enemy on a specific cell.</summary>
    public void PlaceOnCell(HexCell cell, HexGrid grid)
    {
        _grid = grid;
        if (currentCell != null) currentCell.ClearOccupant();
        currentCell = cell;
        currentCell.SetOccupant(gameObject);
        transform.position = grid.CellToWorld(cell.col, cell.row);
    }

    /// <summary>Apply damage to this enemy.</summary>
    public void TakeDamage(float amount)
    {
        currentHp = Mathf.Max(0f, currentHp - amount);
        RefreshHPBar();

        if (currentHp <= 0f)
        {
            if (currentCell != null) currentCell.ClearOccupant();
            OnDefeated?.Invoke(this);
            Destroy(gameObject);
        }
    }

    /// <summary>Move one step toward the player's cell. Call during enemy turn.</summary>
    public void MoveToward(HexCell playerCell)
    {
        if (_grid == null || _isMoving || playerCell == null) return;
        if (currentCell == null) return;

        // Find the neighbour of currentCell that is closest to playerCell
        var neighbours = _grid.GetNeighbors(currentCell);
        HexCell best     = null;
        float   bestDist = float.MaxValue;

        foreach (HexCell n in neighbours)
        {
            if (n.isOccupied) continue;   // skip occupied tiles
            if (!n.isEnemyZone && !IsAdjacentToPlayerZone(n)) continue; // stay in enemy half

            float d = HexDistance(n, playerCell);
            if (d < bestDist)
            {
                bestDist = d;
                best     = n;
            }
        }

        if (best != null)
            StartCoroutine(MoveCoroutine(best));
    }

    /// <summary>Attack the player if this enemy is adjacent to the player's cell.</summary>
    public void AttackPlayer(PlayerController player)
    {
        if (player == null || currentCell == null) return;

        HexCell playerCell = player.GetCurrentCell();
        if (playerCell == null) return;

        if (_grid == null) return;

        bool adjacent = _grid.GetNeighbors(currentCell).Contains(playerCell);
        if (adjacent)
            player.TakeDamage(damage);
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private IEnumerator MoveCoroutine(HexCell target)
    {
        _isMoving = true;

        if (currentCell != null) currentCell.ClearOccupant();

        Vector3 startPos = transform.position;
        Vector3 endPos   = _grid.CellToWorld(target.col, target.row);

        float elapsed  = 0f;
        float duration = Vector3.Distance(startPos, endPos) / moveSpeed;
        duration = Mathf.Max(duration, 0.1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        currentCell = target;
        currentCell.SetOccupant(gameObject);
        _isMoving = false;
    }

    private void RefreshHPBar()
    {
        if (hpBarSlider != null)
            hpBarSlider.value = (maxHp > 0f) ? currentHp / maxHp : 0f;
    }

    /// <summary>Approximate hex distance (Manhattan in offset coords).</summary>
    private static float HexDistance(HexCell a, HexCell b)
    {
        return Mathf.Abs(a.col - b.col) + Mathf.Abs(a.row - b.row);
    }

    private static bool IsAdjacentToPlayerZone(HexCell cell)
    {
        // Column 2 (neutral) borders both zones; allow enemies to move there
        return cell.col <= GameConstants.ENEMY_ZONE_MIN_COL;
    }

    // ── World-space HP bar construction ───────────────────────────────────────

    private void BuildHPBar()
    {
        // Root canvas
        GameObject canvasGO = new GameObject("EnemyHPCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.6f, 0f);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform crt = canvasGO.GetComponent<RectTransform>();
        crt.sizeDelta  = new Vector2(1f, 0.15f);
        crt.localScale = Vector3.one * 0.01f;

        // Background image
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill image
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        Image fillImg = fillGO.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.85f, 0.2f);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Slider component on canvas root
        hpBarSlider                   = canvasGO.AddComponent<Slider>();
        hpBarSlider.minValue          = 0f;
        hpBarSlider.maxValue          = 1f;
        hpBarSlider.value             = 1f;
        hpBarSlider.fillRect          = fillRT;
        hpBarSlider.interactable      = false;
        hpBarSlider.transition        = Selectable.Transition.None;
    }
}

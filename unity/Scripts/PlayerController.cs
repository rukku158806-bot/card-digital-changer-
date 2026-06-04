using System.Collections;
using UnityEngine;

/// <summary>
/// MonoBehaviour attached to the player's glowing blue cube.
/// Handles click-to-move on the hex grid (player zone only, adjacent cells only),
/// tracks HP and Energy, and delegates orb animation to OrbSystem.
/// </summary>
[RequireComponent(typeof(OrbSystem))]
public class PlayerController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Grid Reference")]
    public HexGrid hexGrid;

    [Header("Stats")]
    public float hp     = GameConstants.PLAYER_MAX_HP;
    public int   energy = GameConstants.PLAYER_MAX_ENERGY;
    public int   lives  = GameConstants.PLAYER_MAX_LIVES;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private HexCell    _currentCell;
    private OrbSystem  _orbSystem;
    private bool       _isMoving;
    private bool       _playerTurnActive;

    // ── Events (BattleManager subscribes) ────────────────────────────────────
    public event System.Action<float, float> OnHPChanged;    // (current, max)
    public event System.Action<int>          OnEnergyChanged; // (current)
    public event System.Action               OnPlayerDead;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _orbSystem = GetComponent<OrbSystem>();
    }

    private void Start()
    {
        // Snap to whatever cell the player starts on
        if (hexGrid != null)
        {
            _currentCell = hexGrid.WorldToCell(transform.position);
            if (_currentCell != null) _currentCell.SetOccupant(gameObject);
        }
    }

    private void Update()
    {
        if (!_playerTurnActive || _isMoving) return;

        if (Input.GetMouseButtonDown(0))
            HandleClickToMove();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Enable/disable player input for moving.</summary>
    public void SetPlayerTurnActive(bool active) => _playerTurnActive = active;

    /// <summary>Place the player on a specific cell at game start.</summary>
    public void PlaceOnCell(HexCell cell)
    {
        if (_currentCell != null) _currentCell.ClearOccupant();
        _currentCell = cell;
        if (_currentCell != null)
        {
            _currentCell.SetOccupant(gameObject);
            transform.position = hexGrid.CellToWorld(cell.col, cell.row);
        }
    }

    /// <returns>The cell the player currently occupies.</returns>
    public HexCell GetCurrentCell() => _currentCell;

    /// <summary>Apply damage to the player.</summary>
    public void TakeDamage(float amount)
    {
        hp = Mathf.Max(0f, hp - amount);
        OnHPChanged?.Invoke(hp, GameConstants.PLAYER_MAX_HP);

        if (hp <= 0f)
        {
            lives--;
            if (lives <= 0)
            {
                OnPlayerDead?.Invoke();
            }
            else
            {
                hp = GameConstants.PLAYER_MAX_HP;
                OnHPChanged?.Invoke(hp, GameConstants.PLAYER_MAX_HP);
            }
        }
    }

    /// <summary>Consume energy. Returns false if insufficient.</summary>
    public bool SpendEnergy(int amount)
    {
        if (energy < amount) return false;
        energy -= amount;
        OnEnergyChanged?.Invoke(energy);
        return true;
    }

    /// <summary>Restore energy by the given amount (capped at max).</summary>
    public void RestoreEnergy(int amount)
    {
        energy = Mathf.Min(GameConstants.PLAYER_MAX_ENERGY, energy + amount);
        OnEnergyChanged?.Invoke(energy);
    }

    /// <summary>Animate movement to a target hex cell via coroutine.</summary>
    public void MoveToCell(HexCell target)
    {
        if (target == null || _isMoving) return;
        StartCoroutine(MoveToCellCoroutine(target));
    }

    // ── Input handling ────────────────────────────────────────────────────────

    private void HandleClickToMove()
    {
        if (Camera.main == null || hexGrid == null) return;

        Vector3  mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        HexCell clicked = hexGrid.WorldToCell(mouseWorld);
        if (clicked == null) return;

        // Must be in player zone
        if (!clicked.isPlayerZone) return;

        // Must be adjacent to current cell
        if (_currentCell != null && !hexGrid.GetNeighbors(_currentCell).Contains(clicked)) return;

        // Must be unoccupied (or be the current cell)
        if (clicked.isOccupied && clicked != _currentCell) return;

        MoveToCell(clicked);
    }

    // ── Movement coroutine ────────────────────────────────────────────────────

    private IEnumerator MoveToCellCoroutine(HexCell target)
    {
        _isMoving = true;

        // Vacate old cell
        if (_currentCell != null) _currentCell.ClearOccupant();

        Vector3 startPos = transform.position;
        Vector3 endPos   = hexGrid.CellToWorld(target.col, target.row);

        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, endPos) / GameConstants.PLAYER_MOVE_SPEED;
        duration = Mathf.Max(duration, 0.1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Smooth-step for ease in/out
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;
        _currentCell = target;
        _currentCell.SetOccupant(gameObject);

        _isMoving = false;
    }
}

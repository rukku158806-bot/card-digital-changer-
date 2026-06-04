using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that orchestrates the battle loop:
///   PlayerTurn → (OK pressed) → EnemyTurn → (all enemies acted) → PlayerTurn → …
///
/// Wave progression: when all enemies are defeated, SpawnWave(nextWave) is called.
/// </summary>
public class BattleManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static BattleManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Scene References")]
    public HexGrid         hexGrid;
    public PlayerController player;
    public UIManager        uiManager;

    [Header("Enemy Prefabs (assign in Inspector)")]
    public GameObject gruntPrefab;
    public GameObject supportPrefab;
    public GameObject crystalPrefab;
    public GameObject golemPrefab;
    public GameObject voidPrefab;

    // ── State ─────────────────────────────────────────────────────────────────
    public enum TurnPhase { PlayerTurn, EnemyTurn }

    public TurnPhase currentPhase { get; private set; } = TurnPhase.PlayerTurn;
    public int       currentWave  { get; private set; } = 1;
    public int       score        { get; private set; } = 0;

    private readonly List<EnemyUnit> _activeEnemies = new List<EnemyUnit>();
    private bool _gameOver;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Subscribe to player events
        if (player != null)
        {
            player.OnHPChanged     += (cur, max) => uiManager?.UpdateHP(cur, max);
            player.OnEnergyChanged += (e)         => uiManager?.UpdateEnergy(e);
            player.OnPlayerDead    += HandleGameOver;
        }

        SpawnWave(currentWave);
        BeginPlayerTurn();
    }

    // ── Turn flow ─────────────────────────────────────────────────────────────

    private void BeginPlayerTurn()
    {
        currentPhase = TurnPhase.PlayerTurn;
        player?.SetPlayerTurnActive(true);
        player?.RestoreEnergy(1);   // regain 1 energy each player turn

        uiManager?.UpdateHP(player.hp, GameConstants.PLAYER_MAX_HP);
        uiManager?.UpdateEnergy(player.energy);
    }

    /// <summary>Called by UIManager when the OK button is pressed.</summary>
    public void OnOKButtonPressed()
    {
        if (_gameOver || currentPhase != TurnPhase.PlayerTurn) return;
        player?.SetPlayerTurnActive(false);
        StartCoroutine(RunEnemyTurn());
    }

    private IEnumerator RunEnemyTurn()
    {
        currentPhase = TurnPhase.EnemyTurn;

        // Give each enemy a chance to move then attack
        // Iterate over a copy so removals (death) don't break enumeration
        EnemyUnit[] snapshot = _activeEnemies.ToArray();

        foreach (EnemyUnit enemy in snapshot)
        {
            if (enemy == null) continue;

            HexCell playerCell = player?.GetCurrentCell();
            if (playerCell != null)
                enemy.MoveToward(playerCell);

            // Small delay between each enemy action for readability
            yield return new WaitForSeconds(0.4f);

            if (enemy != null)
                enemy.AttackPlayer(player);

            yield return new WaitForSeconds(0.15f);
        }

        yield return new WaitForSeconds(0.3f);
        BeginPlayerTurn();
    }

    // ── Wave management ───────────────────────────────────────────────────────

    /// <summary>Spawn enemies for the given wave number on enemy-zone tiles.</summary>
    public void SpawnWave(int waveNum)
    {
        _activeEnemies.Clear();

        int enemyCount = GameConstants.ENEMIES_BASE_COUNT
                       + (waveNum - 1) * GameConstants.ENEMIES_PER_WAVE;

        float hpScale = Mathf.Pow(GameConstants.ENEMY_HP_SCALE, waveNum - 1);

        // Collect free enemy-zone cells
        List<HexCell> spawnCells = GetFreeEnemyCells();

        for (int i = 0; i < enemyCount && i < spawnCells.Count; i++)
        {
            GameObject prefab = ChoosePrefabForWave(waveNum, i);
            if (prefab == null) continue;

            GameObject  go   = Instantiate(prefab);
            EnemyUnit   unit = go.GetComponent<EnemyUnit>();
            if (unit == null) unit = go.AddComponent<EnemyUnit>();

            unit.maxHp    = unit.maxHp * hpScale;
            unit.PlaceOnCell(spawnCells[i], hexGrid);
            unit.OnDefeated += OnEnemyDefeated;

            _activeEnemies.Add(unit);
        }

        uiManager?.ShowWaveAnnouncement(waveNum);
    }

    // ── Enemy callbacks ───────────────────────────────────────────────────────

    /// <summary>Called when any enemy's HP reaches zero.</summary>
    public void OnEnemyDefeated(EnemyUnit enemy)
    {
        _activeEnemies.Remove(enemy);
        score += GameConstants.SCORE_PER_KILL;
        uiManager?.UpdateScore(score);

        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (_activeEnemies.Count == 0)
        {
            score += GameConstants.SCORE_PER_WAVE;
            uiManager?.UpdateScore(score);

            currentWave++;
            StartCoroutine(NextWaveDelay());
        }
    }

    private IEnumerator NextWaveDelay()
    {
        yield return new WaitForSeconds(1.5f);
        SpawnWave(currentWave);
        BeginPlayerTurn();
    }

    // ── Game Over ─────────────────────────────────────────────────────────────

    private void HandleGameOver()
    {
        _gameOver = true;
        player?.SetPlayerTurnActive(false);
        uiManager?.ShowGameOver(score);
    }

    // ── Attack / combat API ────────────────────────────────────────────────────

    /// <summary>Execute an Attack action: apply combo damage to all adjacent enemies.</summary>
    public void ExecuteAttack()
    {
        if (currentPhase != TurnPhase.PlayerTurn || _gameOver) return;
        if (!player.SpendEnergy(1)) return;

        var (multiplier, comboName) = player.GetComponent<OrbSystem>()?.ConfirmCombo()
                                      ?? (1f, "Basic Attack");
        if (multiplier <= 0f) { multiplier = 1f; comboName = "Basic Attack"; }

        float totalDamage = GameConstants.BASE_DAMAGE * multiplier;

        HexCell playerCell = player.GetCurrentCell();
        if (playerCell == null || hexGrid == null) return;

        var neighbours = hexGrid.GetNeighbors(playerCell);
        bool hit = false;

        foreach (EnemyUnit enemy in _activeEnemies.ToArray())
        {
            if (enemy == null || enemy.currentCell == null) continue;
            if (neighbours.Contains(enemy.currentCell))
            {
                enemy.TakeDamage(totalDamage);
                uiManager?.ShowDamagePopup(enemy.transform.position, totalDamage, comboName);
                hit = true;
            }
        }

        if (!hit)
        {
            // Still show popup at player position for feedback
            uiManager?.ShowDamagePopup(player.transform.position, totalDamage, comboName);
        }
    }

    /// <summary>Execute a Cannon action: ranged hit on the first enemy in the same row.</summary>
    public void ExecuteCannon()
    {
        if (currentPhase != TurnPhase.PlayerTurn || _gameOver) return;
        if (!player.SpendEnergy(2)) return;

        HexCell playerCell = player.GetCurrentCell();
        if (playerCell == null) return;

        EnemyUnit target = null;
        float     minCol = float.MaxValue;

        foreach (EnemyUnit enemy in _activeEnemies)
        {
            if (enemy == null || enemy.currentCell == null) continue;
            if (enemy.currentCell.row == playerCell.row && enemy.currentCell.col > playerCell.col)
            {
                if (enemy.currentCell.col < minCol)
                {
                    minCol = enemy.currentCell.col;
                    target = enemy;
                }
            }
        }

        if (target != null)
        {
            target.TakeDamage(GameConstants.CANNON_DAMAGE);
            uiManager?.ShowDamagePopup(target.transform.position, GameConstants.CANNON_DAMAGE, "Cannon");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private List<HexCell> GetFreeEnemyCells()
    {
        var result = new List<HexCell>();
        for (int r = 0; r < GameConstants.GRID_ROWS; r++)
        {
            for (int c = GameConstants.ENEMY_ZONE_MIN_COL; c < GameConstants.GRID_COLS; c++)
            {
                HexCell cell = hexGrid.GetCell(c, r);
                if (cell != null && !cell.isOccupied) result.Add(cell);
            }
        }
        return result;
    }

    private GameObject ChoosePrefabForWave(int wave, int index)
    {
        // Rotate through prefab types; higher waves introduce tougher enemies
        int typeIndex = (index + wave - 1) % 5;
        switch (typeIndex)
        {
            case 0: return gruntPrefab   != null ? gruntPrefab   : voidPrefab;
            case 1: return supportPrefab != null ? supportPrefab : gruntPrefab;
            case 2: return crystalPrefab != null ? crystalPrefab : gruntPrefab;
            case 3: return golemPrefab   != null ? golemPrefab   : gruntPrefab;
            case 4: return voidPrefab    != null ? voidPrefab    : gruntPrefab;
            default: return gruntPrefab;
        }
    }
}

// GameConstants.cs — All tuning values for the hex battle game.
// No namespace: keep global for easy access across all scripts.

public static class GameConstants
{
    // ── Combat ────────────────────────────────────────────────────────────────
    public const float BASE_DAMAGE        = 20f;
    public const float CANNON_DAMAGE      = 10f;
    public const float ENEMY_DAMAGE       = 15f;

    // ── Player stats ──────────────────────────────────────────────────────────
    public const float PLAYER_MAX_HP      = 100f;
    public const int   PLAYER_MAX_ENERGY  = 5;
    public const int   PLAYER_MAX_LIVES   = 3;

    // ── Orb system ────────────────────────────────────────────────────────────
    public const float ORB_ORBIT_RADIUS   = 1.5f;
    public const float ORB_ORBIT_SPEED    = 90f;   // degrees per second
    public const float ORB_PULSE_SCALE    = 1.4f;
    public const float ORB_PULSE_SPEED    = 8f;

    // ── Movement ──────────────────────────────────────────────────────────────
    public const float PLAYER_MOVE_SPEED  = 5f;
    public const float ENEMY_MOVE_SPEED   = 3f;
    public const float MOVE_LERP_SPEED    = 8f;

    // ── Grid ──────────────────────────────────────────────────────────────────
    public const int   GRID_COLS          = 5;
    public const int   GRID_ROWS          = 4;
    public const float HEX_SIZE           = 1.0f;  // distance centre-to-corner
    public const int   PLAYER_ZONE_MAX_COL = 1;    // columns 0-1 are player zone
    public const int   ENEMY_ZONE_MIN_COL  = 3;    // columns 3-4 are enemy zone

    // ── Tile highlights ───────────────────────────────────────────────────────
    public const float HOVER_ALPHA        = 0.5f;

    // ── UI / Popups ───────────────────────────────────────────────────────────
    public const float POPUP_RISE_SPEED        = 1.8f;
    public const float POPUP_LIFETIME          = 1.2f;
    public const float WAVE_ANNOUNCE_DURATION  = 2.5f;

    // ── Wave difficulty scaling ───────────────────────────────────────────────
    public const int   ENEMIES_BASE_COUNT = 3;
    public const int   ENEMIES_PER_WAVE   = 1;      // additional enemies each wave
    public const float ENEMY_HP_SCALE     = 1.25f;  // multiplier per wave

    // ── Score ─────────────────────────────────────────────────────────────────
    public const int   SCORE_PER_KILL     = 100;
    public const int   SCORE_PER_WAVE     = 500;
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all HUD elements:
///   Top bar  — HP slider (green), Energy slider (blue), Lives hearts, Score text
///   Left bar — Attack / Cannon / Combo / OK buttons
///   Overlays — Wave announcement, Combo chart panel, Game-over screen
///   World    — Floating damage popups via DamagePopup prefab
/// </summary>
public class UIManager : MonoBehaviour
{
    // ── Inspector — Top HUD ───────────────────────────────────────────────────
    [Header("Top HUD")]
    public Slider    HPBar;
    public Slider    EnergyBar;
    public Transform LivesPanel;      // Parent holding heart icons
    public TMP_Text  ScoreText;
    public TMP_Text  WaveLabel;

    // ── Inspector — Left Sidebar Buttons ──────────────────────────────────────
    [Header("Sidebar Buttons")]
    public Button AttackBtn;
    public Button CannonBtn;
    public Button ComboBtn;
    public Button OKBtn;

    // ── Inspector — Panels / Overlays ─────────────────────────────────────────
    [Header("Panels")]
    public GameObject ComboChartPanel;
    public GameObject GameOverPanel;
    public TMP_Text   GameOverScoreText;
    public TMP_Text   WaveAnnouncementText;

    // ── Inspector — Popup ─────────────────────────────────────────────────────
    [Header("Damage Popup")]
    [Tooltip("Prefab with a DamagePopup component.")]
    public GameObject DamagePopupPrefab;

    // ── Inspector — Heart Icon ────────────────────────────────────────────────
    [Header("Lives")]
    [Tooltip("Prefab / image used for each heart icon in LivesPanel.")]
    public GameObject HeartIconPrefab;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private int _currentLives = GameConstants.PLAYER_MAX_LIVES;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Wire sidebar buttons → BattleManager
        if (AttackBtn != null) AttackBtn.onClick.AddListener(OnAttackPressed);
        if (CannonBtn != null) CannonBtn.onClick.AddListener(OnCannonPressed);
        if (ComboBtn  != null) ComboBtn .onClick.AddListener(ShowComboChart);
        if (OKBtn     != null) OKBtn    .onClick.AddListener(OnOKPressed);

        // Start hidden
        if (ComboChartPanel     != null) ComboChartPanel.SetActive(false);
        if (GameOverPanel       != null) GameOverPanel.SetActive(false);
        if (WaveAnnouncementText != null) WaveAnnouncementText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Initialise bars to full
        UpdateHP(GameConstants.PLAYER_MAX_HP, GameConstants.PLAYER_MAX_HP);
        UpdateEnergy(GameConstants.PLAYER_MAX_ENERGY);
        RebuildLives(_currentLives);
        UpdateScore(0);
    }

    // ── Top HUD updaters ──────────────────────────────────────────────────────

    /// <summary>Refresh the HP slider (green bar).</summary>
    public void UpdateHP(float current, float max)
    {
        if (HPBar != null)
            HPBar.value = (max > 0f) ? current / max : 0f;
    }

    /// <summary>Refresh the Energy slider (blue bar).</summary>
    public void UpdateEnergy(int current)
    {
        if (EnergyBar != null)
            EnergyBar.value = (float)current / GameConstants.PLAYER_MAX_ENERGY;
    }

    /// <summary>Rebuild heart icons in LivesPanel to reflect remaining lives.</summary>
    public void UpdateLives(int lives)
    {
        _currentLives = lives;
        RebuildLives(lives);
    }

    /// <summary>Update the score display.</summary>
    public void UpdateScore(int newScore)
    {
        if (ScoreText != null)
            ScoreText.text = $"Score: {newScore:N0}";
    }

    // ── Wave announcement ─────────────────────────────────────────────────────

    /// <summary>Flash "Wave N" text on screen for a few seconds.</summary>
    public void ShowWaveAnnouncement(int wave)
    {
        if (WaveLabel != null)
            WaveLabel.text = $"Wave {wave}";

        if (WaveAnnouncementText != null)
            StartCoroutine(FlashWaveText(wave));
    }

    private IEnumerator FlashWaveText(int wave)
    {
        WaveAnnouncementText.text = $"— Wave {wave} —";
        WaveAnnouncementText.gameObject.SetActive(true);
        yield return new WaitForSeconds(GameConstants.WAVE_ANNOUNCE_DURATION);
        WaveAnnouncementText.gameObject.SetActive(false);
    }

    // ── Combo chart ───────────────────────────────────────────────────────────

    /// <summary>Toggle the combo reference chart panel.</summary>
    public void ShowComboChart()
    {
        if (ComboChartPanel == null) return;
        bool showing = ComboChartPanel.activeSelf;
        ComboChartPanel.SetActive(!showing);
    }

    // ── Damage popup ──────────────────────────────────────────────────────────

    /// <summary>Spawn a floating damage number at a world position.</summary>
    public void ShowDamagePopup(Vector3 worldPos, float damage, string comboName)
    {
        if (DamagePopupPrefab == null)
        {
            // Fallback: create a temporary runtime popup
            GameObject go = new GameObject("DamagePopup");
            DamagePopup popup = go.AddComponent<DamagePopup>();
            popup.Initialise(damage, comboName, worldPos);
            return;
        }

        GameObject popupGO = Instantiate(DamagePopupPrefab, worldPos, Quaternion.identity);
        DamagePopup dp = popupGO.GetComponent<DamagePopup>();
        if (dp == null) dp = popupGO.AddComponent<DamagePopup>();
        dp.Initialise(damage, comboName, worldPos);
    }

    // ── Game over ─────────────────────────────────────────────────────────────

    /// <summary>Show the game-over overlay with the final score.</summary>
    public void ShowGameOver(int finalScore)
    {
        if (GameOverPanel != null) GameOverPanel.SetActive(true);
        if (GameOverScoreText != null)
            GameOverScoreText.text = $"Final Score: {finalScore:N0}";
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void OnAttackPressed()
    {
        // Open orb selection, then execute after player picks orbs
        OrbSystem orbs = BattleManager.Instance?.player?.GetComponent<OrbSystem>();
        if (orbs != null) orbs.StartCombatSelection();
        BattleManager.Instance?.ExecuteAttack();
    }

    private void OnCannonPressed()
    {
        BattleManager.Instance?.ExecuteCannon();
    }

    private void OnOKPressed()
    {
        BattleManager.Instance?.OnOKButtonPressed();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RebuildLives(int lives)
    {
        if (LivesPanel == null) return;

        // Clear existing icons
        foreach (Transform child in LivesPanel)
            Destroy(child.gameObject);

        if (HeartIconPrefab == null) return;

        for (int i = 0; i < lives; i++)
        {
            GameObject heart = Instantiate(HeartIconPrefab, LivesPanel);
            heart.name = $"Heart_{i}";
        }
    }
}

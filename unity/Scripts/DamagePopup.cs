using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Spawned at a world position; floats upward and fades out over
/// GameConstants.POPUP_LIFETIME seconds.
/// Attach to a prefab that has a Canvas (World Space) + TextMeshProUGUI child,
/// OR the script will create one at runtime.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    // ── Inspector (optional — auto-created if null) ────────────────────────────
    [Header("Text reference (optional — created at runtime if null)")]
    public TextMeshProUGUI label;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private float  _elapsed   = 0f;
    private float  _lifetime  = GameConstants.POPUP_LIFETIME;
    private Color  _startColor;
    private Canvas _canvas;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (label == null) BuildLabel();
    }

    /// <summary>Initialise text, colour, and starting world position.</summary>
    public void Initialise(float damage, string comboName, Vector3 worldPos)
    {
        transform.position = worldPos + Vector3.up * 0.3f;

        if (label != null)
        {
            bool isCombo = !string.IsNullOrEmpty(comboName) && comboName != "Basic Attack";
            label.text = isCombo
                ? $"<b>{damage:F0}</b>\n<size=60%>{comboName}</size>"
                : $"<b>{damage:F0}</b>";

            _startColor       = isCombo ? new Color(1f, 0.85f, 0.1f) : Color.white;
            label.color       = _startColor;
        }
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t   = _elapsed / _lifetime;

        // Rise
        transform.position += Vector3.up * GameConstants.POPUP_RISE_SPEED * Time.deltaTime;

        // Fade
        if (label != null)
        {
            Color c = _startColor;
            c.a       = Mathf.Lerp(1f, 0f, t);
            label.color = c;
        }

        if (_elapsed >= _lifetime) Destroy(gameObject);
    }

    // ── Auto-build ─────────────────────────────────────────────────────────────

    private void BuildLabel()
    {
        // World-space canvas
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode        = RenderMode.WorldSpace;
        _canvas.sortingOrder      = 20;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta      = new Vector2(2f, 1f);
        rt.localScale     = Vector3.one * 0.01f;

        // TMPro label child
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(transform, false);

        label = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment           = TextAlignmentOptions.Center;
        label.fontSize            = 48;
        label.fontStyle           = FontStyles.Bold;
        label.enableWordWrapping  = false;
        label.color               = Color.white;

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin  = Vector2.zero;
        trt.anchorMax  = Vector2.one;
        trt.offsetMin  = Vector2.zero;
        trt.offsetMax  = Vector2.zero;

        _startColor = Color.white;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Spawned at a world position; floats upward and fades out over
/// GameConstants.POPUP_LIFETIME seconds.
///
/// Setup options:
///   A) Assign a prefab with a World-Space Canvas + TextMeshProUGUI child — set the
///      'label' field in the Inspector before calling Initialise().
///   B) Leave 'label' null — BuildLabel() will create a Canvas + TMPro at runtime.
/// </summary>
public class DamagePopup : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Text reference (auto-created if null)")]
    public TextMeshProUGUI label;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private float _elapsed;
    private Color _startColor;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (label == null) BuildLabel();
    }

    /// <summary>
    /// Set the popup text and starting world position.
    /// Call immediately after instantiation.
    /// </summary>
    public void Initialise(float damage, string comboName, Vector3 worldPos)
    {
        transform.position = worldPos + Vector3.up * 0.3f;
        _elapsed = 0f;

        if (label == null) return;

        bool isCombo = !string.IsNullOrEmpty(comboName) && comboName != "Basic Attack";

        label.text = isCombo
            ? $"<b>{damage:F0}</b>\n<size=60%>{comboName}</size>"
            : $"<b>{damage:F0}</b>";

        _startColor = isCombo ? new Color(1f, 0.85f, 0.1f) : Color.white;
        label.color = _startColor;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / GameConstants.POPUP_LIFETIME;

        // Rise upward
        transform.position += Vector3.up * GameConstants.POPUP_RISE_SPEED * Time.deltaTime;

        // Fade out
        if (label != null)
        {
            Color c = _startColor;
            c.a         = Mathf.Lerp(1f, 0f, t);
            label.color = c;
        }

        if (_elapsed >= GameConstants.POPUP_LIFETIME)
            Destroy(gameObject);
    }

    // ── Runtime canvas construction ───────────────────────────────────────────

    private void BuildLabel()
    {
        // World-space canvas
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta  = new Vector2(2f, 1f);
        rt.localScale = Vector3.one * 0.01f;

        // TMPro label as child
        GameObject textGO = new GameObject("PopupText");
        textGO.transform.SetParent(transform, false);

        label                 = textGO.AddComponent<TextMeshProUGUI>();
        label.alignment       = TextAlignmentOptions.Center;
        label.fontSize        = 48;
        label.fontStyle       = FontStyles.Bold;
        label.enableWordWrapping = false;
        label.color           = Color.white;

        RectTransform trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        _startColor = Color.white;
    }
}

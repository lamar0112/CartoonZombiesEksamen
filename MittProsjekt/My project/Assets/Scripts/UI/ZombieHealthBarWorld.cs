using UnityEngine;
using UnityEngine.UI;

// ZombieHealthBarWorld — World Space Canvas + Image fill over zombie (PG2202-08 UI i 3D-verden).
// Pensum: abonnerer på ZombieHealth.OnHealthChanged; billboards mot kamera der implementert.
// Ekstra: bygges i kode for rask iterasjon — kan prefabes senere hvis dere vil style i Inspector.
[RequireComponent(typeof(ZombieHealth))]
public class ZombieHealthBarWorld : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.15f, 0f);
    [SerializeField] private Vector2 worldSize = new Vector2(1.2f, 0.16f);

    private ZombieHealth _health;
    private Transform    _barRoot;
    private Image          _fill;
    private Transform      _cam;

    private void Awake()
    {
        _health = GetComponent<ZombieHealth>();
        _health.OnHealthChanged += OnHealthChanged;

        _barRoot = new GameObject("WorldHealthBar").transform;
        _barRoot.SetParent(transform, false);
        _barRoot.localPosition = localOffset;

        GameObject canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(_barRoot, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = worldSize * 100f;
        rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        var bg = bgGo.AddComponent<Image>();
        bg.sprite = UiWhiteSprite();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(bgGo.transform, false);
        var fillRt = fillGo.AddComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(1f, 1f);
        fillRt.offsetMin = new Vector2(4f, 4f);
        fillRt.offsetMax = new Vector2(-4f, -4f);
        _fill = fillGo.AddComponent<Image>();
        _fill.sprite = UiWhiteSprite();
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fill.color = new Color(0.25f, 0.9f, 0.35f, 0.95f);

        OnHealthChanged(_health.CurrentHealth, _health.MaxHealth);
    }

    private static Sprite UiWhiteSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void OnDestroy()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    private void LateUpdate()
    {
        if (_barRoot == null || _health == null || _health.IsDead)
        {
            if (_barRoot != null) _barRoot.gameObject.SetActive(false);
            return;
        }

        _barRoot.gameObject.SetActive(true);
        _cam = Camera.main != null ? Camera.main.transform : null;
        if (_cam == null) return;

        Vector3 toCam = _barRoot.position - _cam.position;
        if (toCam.sqrMagnitude > 0.0001f)
            _barRoot.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }

    private void OnHealthChanged(int current, int max)
    {
        if (_fill == null || max <= 0) return;
        float t = Mathf.Clamp01((float)current / max);
        _fill.fillAmount = t;
        _fill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.15f), new Color(0.25f, 0.9f, 0.35f), t);
    }
}

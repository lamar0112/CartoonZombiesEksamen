using TMPro;
using UnityEngine;

// DamagePopup — verdensrom-TMP som stiger og fades (PG2202-08; Instantiate fra PlayerShooting).
// Pensum: statisk Spawn(); Update for bevegelse og alfaskikk.
// Ekstra: stor font for lesbarhet i world-space — kompromiss mellom avstand og UI-tykkelse (typisk polish).
public class DamagePopup : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private float lifetime      = 0.9f;
    [SerializeField] private float riseSpeed     = 2.2f;
    [SerializeField] private float fadeStartAt   = 0.5f; // normalisert levetid (0-1)

    private TextMeshPro _tmp;
    private float       _elapsed;
    private float       _totalLife;
    private Color       _startColor;

    // Statisk fabrikk — andre scripts bruker DamagePopup.Spawn() i stedet for Instantiate
    public static DamagePopup Spawn(int damage, Vector3 worldPos)
    {
        GameObject go = new GameObject("DamagePopup");
        go.transform.position = worldPos + Vector3.up * 1.6f;

        DamagePopup popup = go.AddComponent<DamagePopup>();
        popup.Init(damage);
        return popup;
    }

    private void Init(int damage)
    {
        _tmp                  = gameObject.AddComponent<TextMeshPro>();
        _tmp.text             = $"-{damage}";
        // World-space TMP: liten font forsvinner på avstand — hold font stor nok til å leses i spillet.
        _tmp.fontSize         = 42f;
        _tmp.fontStyle        = FontStyles.Bold;
        _tmp.alignment        = TextAlignmentOptions.Center;
        _tmp.color            = new Color(1f, 0.25f, 0.1f, 1f); // knall-rødt
        _tmp.outlineWidth     = 0.35f;
        _tmp.outlineColor     = new Color32(0, 0, 0, 240);
        _tmp.sortingOrder     = 500;

        _startColor = _tmp.color;
        _totalLife  = lifetime;

        // Slight world shrink so numbers are not huge on humanoid-scale characters
        transform.localScale = Vector3.one * 0.035f;

        Destroy(gameObject, lifetime + 0.1f);
    }

    private void Update()
    {
        if (_tmp == null) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _totalLife);

        // Beveger seg opp
        transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

        // Kamera-billboard — alltid mot kameraet (PG2202-03)
        if (Camera.main != null)
            transform.rotation = Camera.main.transform.rotation;

        // Fade ut i siste del av levetiden
        if (t >= fadeStartAt)
        {
            float alpha = 1f - Mathf.InverseLerp(fadeStartAt, 1f, t);
            Color c = _startColor;
            c.a    = alpha;
            _tmp.color = c;
        }
    }
}

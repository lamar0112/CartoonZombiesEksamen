using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// ParkourCoinsDisplay — TMP-linje for CityParkourManager-fremgang (PG2202-08; PG2202-06 abonnement på events).
// Pensum: kun aktiv i Level01_By; viser collected/total for sonene.
// Ekstra: oppretter eget TextMeshProUGUI i Awake hvis menyverktøy har satt opp container — mindre manuell UI-draging.
public class ParkourCoinsDisplay : MonoBehaviour
{
    private TextMeshProUGUI _tmp;

    private void Awake()
    {
        var textGo = new GameObject("ParkourCoinsText");
        textGo.transform.SetParent(transform, false);
        var rt = textGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        _tmp = textGo.AddComponent<TextMeshProUGUI>();
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.fontSize = 15f;
        _tmp.richText = true;
        _tmp.color = new Color(1f, 0.94f, 0.55f, 0.98f);
        try
        {
            _tmp.outlineWidth = 0.22f;
            _tmp.outlineColor = new Color32(0, 0, 0, 180);
        }
        catch { /* TMP-material kan mangle i edge cases */ }
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != GameSceneNames.Level01By)
        {
            gameObject.SetActive(false);
            return;
        }

        CityParkourManager cpm = CityParkourManager.Instance;
        if (cpm == null)
        {
            gameObject.SetActive(false);
            return;
        }

        cpm.OnZone1Progress.AddListener((_, __) => Redraw());
        cpm.OnZone2Progress.AddListener((_, __) => Redraw());
        Redraw();
    }

    private void Redraw()
    {
        CityParkourManager m = CityParkourManager.Instance;
        if (m == null || _tmp == null) return;

        _tmp.text =
            $"<b>Parkour-mynter</b>  " +
            $"<color=#88EEFF>Sone 1:</color> {m.CollectedZone1}/{m.TotalCoinsZone1}  " +
            $"<color=#FFAA66>·</color>  " +
            $"<color=#88EEFF>Sone 2:</color> {m.CollectedZone2}/{m.TotalCoinsZone2}";
    }
}

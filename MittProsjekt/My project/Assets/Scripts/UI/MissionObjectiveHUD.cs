using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// MissionObjectiveHUD — kort tekst om mål/kontroller; TMP + CanvasGroup fade (PG2202-08 UI).
// Pensum: skjul etter tid og ved første skudd (GameFeel); scene-navn styrer strand vs by-hjelp.
// Ekstra: runtime-opprettet «strip» hvis referanser mangler — robusthet etter verktøy-import; nevnes i rapport.
public class MissionObjectiveHUD : MonoBehaviour
{
    [Tooltip("Kort hjelp om seier/tap — eget felt under oppdragspanelet.")]
    [SerializeField] private TextMeshProUGUI goalsHelpText;

    [Tooltip("Sekunder før panelet fades ut automatisk.")]
    [SerializeField] private float autoHideDelay = 25f;

    private PlayerShooting _shooting;
    private bool           _hidden;
    private bool           _isBeachLevel;
    private CanvasGroup    _group;

    private TextMeshProUGUI ResolveGoalsText()
    {
        if (goalsHelpText != null) return goalsHelpText;
        var found = GameObject.Find("LevelGoalsText");
        if (found != null)
        {
            var t = found.GetComponent<TextMeshProUGUI>();
            if (t != null) goalsHelpText = t;
        }
        if (goalsHelpText == null)
            goalsHelpText = CreateRuntimeGoalsStrip();
        return goalsHelpText;
    }

    private TextMeshProUGUI CreateRuntimeGoalsStrip()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || canvas.transform.Find("GoalsHintPanel") != null) return null;

        var panel = new GameObject("GoalsHintPanel");
        panel.transform.SetParent(canvas.transform, false);

        _group = panel.AddComponent<CanvasGroup>();

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(12f, 14f);
        rt.sizeDelta        = new Vector2(540f, 78f);

        var img = panel.AddComponent<Image>();
        img.color         = new Color(0.04f, 0.06f, 0.12f, 0.78f);
        img.raycastTarget = false;

        var textGo = new GameObject("LevelGoalsText");
        textGo.transform.SetParent(panel.transform, false);
        var tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12, 8);
        tr.offsetMax = new Vector2(-12, -8);

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = " ";
        tmp.richText  = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.fontSize  = 14f;
        tmp.color     = new Color(0.92f, 0.94f, 0.98f);
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 160);
        return tmp;
    }

    private void Start()
    {
        var tmp = ResolveGoalsText();
        if (tmp == null) return;

        if (_group == null && tmp.transform.parent != null)
            _group = tmp.transform.parent.GetComponent<CanvasGroup>();

        _isBeachLevel = SceneManager.GetActiveScene().name == GameSceneNames.Level02StrandSkog;

        _shooting = FindFirstObjectByType<PlayerShooting>();
        if (_shooting != null)
            _shooting.OnWeaponFired += OnFirstShot;

        ApplyGoalsLayout(tmp);

        if (_isBeachLevel)
        {
            tmp.text =
                "<b>Strand/skog — mål</b>\n" +
                "<size=90%><b>Seier:</b> Drep zombier → båt → øy → <b>kiste</b>.  " +
                "<b>Tap:</b> HP = 0.  Oransje <b>▲</b> = retning til neste mål.</size>";
        }
        else
        {
            tmp.text =
                "<b>By — mål</b>\n" +
                "<size=90%><b>Seier:</b> Drep zombier → parkour (begge soner) → <b>exit</b> til strand.  " +
                "<b>Tap:</b> HP = 0.  Oransje <b>▲</b> = retning til pistol / bil / exit.</size>";
        }

        ApplyControlHints();
        StartCoroutine(AutoHide());
    }

    private static void ApplyGoalsLayout(TextMeshProUGUI tmp)
    {
        tmp.enableAutoSizing = false;
        tmp.fontSize         = 14f;
        tmp.lineSpacing      = 18f;
        tmp.richText         = true;
        tmp.alignment        = TextAlignmentOptions.TopLeft;
        tmp.color            = new Color(0.92f, 0.94f, 0.98f, 0.98f);
    }

    private void ApplyControlHints()
    {
        var go   = GameObject.Find("ControlHintsText");
        var ctrl = go != null ? go.GetComponent<TextMeshProUGUI>() : null;
        if (ctrl == null) return;

        ctrl.fontSize = 12f;
        ctrl.color    = new Color(0.75f, 0.78f, 0.82f);

        ctrl.text = _isBeachLevel
            ? "WASD beveg · Shift sprint · Space hopp · Musepeker sikt · LMB skyt · R last · ESC pause · Y juks · <b>F = båt</b> når låst opp"
            : "WASD beveg · Shift sprint · Space hopp · Musepeker sikt · LMB skyt · R last · ESC pause · Y juks · <b>F = bil</b> når nær";
    }

    // Fades ut automatisk etter autoHideDelay sekunder
    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideDelay);
        yield return FadeOut(1.2f);
        _hidden = true;
    }

    private void OnFirstShot()
    {
        if (_hidden) return;
        var tmp = ResolveGoalsText();
        if (tmp == null) return;

        if (_isBeachLevel)
        {
            tmp.text =
                "<b>Påminnelse</b>\n" +
                "<size=90%>Båt = F når låst opp.  Kisten på øya gir seier.  " +
                "Hold deg på land / NavMesh — dypt vann stopper deg.</size>";
        }
        else
        {
            tmp.text =
                "<b>Påminnelse</b>\n" +
                "<size=90%>Fullfør begge parkour-sonene, bruk så den grønne exiten.  " +
                "Bil = F nær bilen.  WASD kjører.</size>";
        }

        StopAllCoroutines();
        StartCoroutine(AutoHide());
    }

    private IEnumerator FadeOut(float duration)
    {
        if (_group == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t             += Time.unscaledDeltaTime;
            _group.alpha   = 1f - Mathf.Clamp01(t / duration);
            yield return null;
        }
        if (_group != null) _group.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (_shooting != null)
            _shooting.OnWeaponFired -= OnFirstShot;
    }
}

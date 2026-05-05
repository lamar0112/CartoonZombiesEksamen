using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Nederste «mål / win-lose»-hjelp — må IKKE bruke samme TMP som MissionManager (oppdragstekst).
public class MissionObjectiveHUD : MonoBehaviour
{
    [Tooltip("Kort hjelp om seier/tap — eget felt under oppdragspanelet.")]
    [SerializeField] private TextMeshProUGUI goalsHelpText;

    private PlayerShooting _shooting;
    private bool           _hasFired;
    private bool           _isBeachLevel;

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
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 108f);
        rt.sizeDelta = new Vector2(720f, 102f);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.04f, 0.05f, 0.09f, 0.72f);
        img.raycastTarget = false;

        var textGo = new GameObject("LevelGoalsText");
        textGo.transform.SetParent(panel.transform, false);
        var tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(12, 8);
        tr.offsetMax = new Vector2(-12, -8);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = " ";
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.fontSize = 15;
        tmp.color = Color.white;
        return tmp;
    }

    private void Start()
    {
        var tmp = ResolveGoalsText();
        if (tmp == null) return;

        _isBeachLevel = SceneManager.GetActiveScene().name == GameSceneNames.Level02StrandSkog;

        _shooting = FindFirstObjectByType<PlayerShooting>();
        if (_shooting != null)
            _shooting.OnWeaponFired += OnFirstShot;

        ApplyGoalsLayout(tmp);
        if (_isBeachLevel)
        {
            tmp.text =
                "<b>Beach — quick goals</b>\n" +
                "<size=90%><b>Win:</b> Waves → boat → island → <b>chest</b>.  <b>Lose:</b> HP = 0.\n" +
                "Orange <b>▲</b> above compass ring = walk direction to the current hint.</size>";
        }
        else
        {
            tmp.text =
                "<b>City — quick goals</b>\n" +
                "<size=90%><b>Win:</b> Waves → parkour (both) → <b>exit</b> to beach.  <b>Lose:</b> HP = 0.\n" +
                "Orange <b>▲</b> = direction to pistol / car / exit when active.</size>";
        }

        ApplyControlHints();
    }

    private static void ApplyGoalsLayout(TextMeshProUGUI tmp)
    {
        tmp.enableAutoSizing = false;
        tmp.fontSize = 15;
        tmp.lineSpacing = 22f;
        tmp.richText = true;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(0.94f, 0.95f, 0.98f, 0.98f);
        var rt = tmp.rectTransform;
        if (rt != null && rt.sizeDelta.y > 120f)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 118f);
    }

    private void ApplyControlHints()
    {
        var go = GameObject.Find("ControlHintsText");
        var ctrl = go != null ? go.GetComponent<TextMeshProUGUI>() : null;
        if (ctrl == null) return;

        if (_isBeachLevel)
        {
            ctrl.text =
                "WASD move · Shift sprint · Space jump · Mouse look · LMB shoot · R reload · ESC pause · Y cheats · <b>F = boat</b> when unlocked";
        }
        else
        {
            ctrl.text =
                "WASD move · Shift sprint · Space jump · Mouse look · LMB shoot · R reload · ESC pause · Y cheats · <b>F = car</b> when near";
        }
    }

    private void OnFirstShot()
    {
        var tmp = ResolveGoalsText();
        if (_hasFired || tmp == null) return;
        _hasFired = true;

        if (_isBeachLevel)
        {
            tmp.text =
                "<b>Reminder</b>\n" +
                "<size=90%>Boat = F when unlocked. Chest on island wins. Stay on land / NavMesh — deep water blocks you.</size>";
        }
        else
        {
            tmp.text =
                "<b>Reminder</b>\n" +
                "<size=90%>Finish both parkour zones, then use the green exit. Car = F near DrivableCar.</size>";
        }
    }

    private void OnDestroy()
    {
        if (_shooting != null)
            _shooting.OnWeaponFired -= OnFirstShot;
    }
}

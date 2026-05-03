using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Oppdragstekst nederst — tydelig mål, seier og tap.
public class MissionObjectiveHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI objectiveText;

    private PlayerShooting _shooting;
    private bool           _hasFired;
    private bool           _isBeachLevel;

    private void Start()
    {
        if (objectiveText == null) return;

        _isBeachLevel = SceneManager.GetActiveScene().name == GameSceneNames.Level02StrandSkog;

        _shooting = FindFirstObjectByType<PlayerShooting>();
        if (_shooting != null)
            _shooting.OnWeaponFired += OnFirstShot;

        if (_isBeachLevel)
        {
            objectiveText.text =
                "<b>Mål — strand og skog</b>\n" +
                "<b>Seier:</b> Når alle bølger er ferdig og du har nådd <b>kisten på øya</b> (gull-markering i verdenen), vinner du spillet.\n" +
                "<b>Tap:</b> HP går til null.\n" +
                "Du kan <b>ikke</b> gå på vann — bruk land, båtstien og båt når den er låst opp. Oransje pil = siste zombie eller vei videre.";
        }
        else
        {
            objectiveText.text =
                "<b>Mål — byen</b>\n" +
                "<b>Seier:</b> Drepe alle zombier i bølgene → fullfør <b>begge parkour-sonene</b> → gå gjennom <b>utgangen</b> (grønn trigger / pil) til strand-nivå.\n" +
                "<b>Tap:</b> HP går til null.\n" +
                "Kun bilen merket «<b>DrivableCar</b>» kan kjøres: stå nærme → <b>F</b> → WASD + mellomrom (brems). Oransje pil hjelper deg.";
        }
    }

    private void OnFirstShot()
    {
        if (_hasFired || objectiveText == null) return;
        _hasFired = true;
        if (_isBeachLevel)
        {
            objectiveText.text =
                "<b>Påminnelse</b>\n" +
                "• Fullfør bølgene → lås opp båt → ro til øya → <b>gå inn i kisten</b> for seier.\n" +
                "• Vann er blokkert til fots. ESC = pause · Y = cheat · F = bil.";
        }
        else
        {
            objectiveText.text =
                "<b>Påminnelse</b>\n" +
                "• Parkour → utgang når alt er grønt. Deretter neste nivå.\n" +
                "• ESC = pause · Y = cheat · F = bil ved DrivableCar.";
        }
    }

    private void OnDestroy()
    {
        if (_shooting != null)
            _shooting.OnWeaponFired -= OnFirstShot;
    }
}

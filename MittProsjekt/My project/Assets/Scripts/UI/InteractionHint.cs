using TMPro;
using UnityEngine;

// InteractionHint — singleton TMP-linje for kontekst (F for bil m.m.) (PG2202-08; PG2202-12 delt tjeneste).
// Pensum: Show/Hide fra CarInteraction eller andre interaksjoner.
// Ekstra: sentral hint i stedet for mange små UI-elementer — mindre rot i Canvas-hierarkiet.
public class InteractionHint : MonoBehaviour
{
    public static InteractionHint Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI label;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (label != null) label.gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        if (label == null) return;
        label.text = message;
        label.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    public void Hide() => Show("");
}

using TMPro;
using UnityEngine;

// Enkel hint-linje (f.eks. «[F] Gå inn i bilen») — CarInteraction styrer innholdet.
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

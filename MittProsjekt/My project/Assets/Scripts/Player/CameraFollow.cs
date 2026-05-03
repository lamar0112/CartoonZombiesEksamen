using UnityEngine;
using UnityEngine.SceneManagement;

// Over-the-shoulder tredjeperson kamera (PG2202-04)
// LateUpdate kjøres etter all annen Update - unngår jitter når spilleren beveger seg
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float     mouseSens     = 2f;
    [SerializeField] private float     minPitch      = -15f;
    [SerializeField] private float     maxPitch      =  25f; // lavere maks så kamera ikke peker rett ned

    // Over-the-shoulder offset: negativ X = venstre skulder
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0.6f, 1.6f, -4.5f); // høyre skulder → spiller til venstre, crosshair til høyre

    private float pitch = 8f; // starter litt ned, ikke for bratt

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindCameraToPlayer();
    }

    // Start() er én frame etter OnEnable — gir spilleren tid til å fullføre sin Awake() før vi kobler kamera
    private void Start()
    {
        if (target == null)
            BindCameraToPlayer();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindCameraToPlayer();
    }

    // Etter sceneskifte må kamera finne Player på nytt (serialized target fra forrige scene er borte) (PG2202-12)
    private void BindCameraToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Full pause / cheat-meny: ikke oppdater kamera (unngår «ser meg rundt» med fryst spill)
        if (Time.timeScale <= 0f) return;
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen) return;

        pitch -= Input.GetAxis("Mouse Y") * mouseSens;
        pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Kameraets rotasjon = spillerens horisontal-rotasjon + kamera-pitch
        Quaternion camRot = Quaternion.Euler(pitch, target.eulerAngles.y, 0f);
        transform.position = target.position + camRot * shoulderOffset;
        transform.rotation = camRot;
    }

    public void SetTarget(Transform t) => target = t;

    /// <summary>Kall etter teleport / scene load — unngår én frame med feil kameraposisjon.</summary>
    public void SnapToTargetNow()
    {
        if (target == null) return;

        Quaternion camRot = Quaternion.Euler(pitch, target.eulerAngles.y, 0f);
        transform.SetPositionAndRotation(
            target.position + camRot * shoulderOffset,
            camRot);
    }
}

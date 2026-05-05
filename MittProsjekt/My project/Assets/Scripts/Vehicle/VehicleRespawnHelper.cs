using UnityEngine;

// VehicleRespawnHelper — flytt bil/båt tilbake til «hjem»-posisjon (PG2202-04 input, PG2202-04 Rigidbody).
// Pensum: lagre start-pos/rot i Awake; tast (standard B) når ingen sitter i kjøretøyet.
// Ekstra: legg på samme GameObject som CarController / CarInteraction; strand: samme script på båt-prefaben.
[DisallowMultipleComponent]
public class VehicleRespawnHelper : MonoBehaviour
{
    [SerializeField] private KeyCode respawnKey = KeyCode.B;
    [Tooltip("Hvis satt: brukes denne posisjonen. Ellers lagres transform ved Awake.")]
    [SerializeField] private Transform homeTransform;

    private Vector3 _homePos;
    private Quaternion _homeRot;
    private Rigidbody _rb;
    private CarController _car;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody>();
        _car = GetComponent<CarController>();
        if (homeTransform != null)
        {
            _homePos = homeTransform.position;
            _homeRot = homeTransform.rotation;
        }
        else
        {
            _homePos = transform.position;
            _homeRot = transform.rotation;
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen) return;

        if (_car != null && _car.IsOccupied) return;

        if (Input.GetKeyDown(respawnKey))
            RespawnToHome();
    }

    /// <summary>Kall fra CheatMenu eller annen logikk — nullstill fysikk og plasser på «hjem».</summary>
    public void RespawnToHome()
    {
        if (homeTransform != null)
        {
            _homePos = homeTransform.position;
            _homeRot = homeTransform.rotation;
        }

        transform.SetPositionAndRotation(_homePos, _homeRot);
        if (_rb != null)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }
    }
}

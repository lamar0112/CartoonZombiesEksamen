using System.Collections;
using UnityEngine;

// Håndterer skyting via Raycast fra kameraet (ikke fysiske prosjektiler)
// Raycast er standard tilnærming for hitscan-våpen i Unity (PG2202-04 kollisjon)
public class PlayerShooting : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int   damage        = 25;
    [SerializeField] private float range         = 50f;  // maks rekkevidde på skuddet

    [Header("Ammo")]
    [SerializeField] private int   maxAmmo       = 30;
    [SerializeField] private float reloadTime    = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptySound;  // klikke-lyd når tom
    [Tooltip("Relative to Player AudioSource volume (master from pause menu).")]
    [Range(0f, 1f)] [SerializeField] private float shootVolumeScale  = 1f;
    [Range(0f, 1f)] [SerializeField] private float reloadVolumeScale = 1f;
    [Range(0f, 1f)] [SerializeField] private float emptyVolumeScale  = 1f;

    [Header("Visuals")]
    [SerializeField] private GameObject muzzleFlashPrefab; // VFX-prefab for munnflamme
    [SerializeField] private Transform  muzzlePoint;       // tom GameObject på våpenet
    [SerializeField] private GameObject hitEffectPrefab;   // gnist-VFX når kule treffer
    [SerializeField] private float      muzzleFlashLifetime = 0.4f;

    // Private felt
    private int         currentAmmo;
    private bool        isReloading = false;
    private AudioSource audioSource; // AudioSource er Unity sin lyd-komponent (PG2202-10)
    private Camera      cam;         // bruker Main Camera til Raycast

    // UI-event - HUDController abonnerer for å vise ammo-teller
    // (UnityEvent ikke pensum, men løs kobling er bedre enn direkte referanse)
    public UnityEngine.Events.UnityEvent<int, int> OnAmmoChanged;

    /// <summary>Kalles når et skudd faktisk avfyres (ikke tomt magasin-klikk).</summary>
    public event System.Action OnWeaponFired;

    private void Awake()
    {
        // Hvis en WeaponPickup finnes i scenen, starter spilleren uten våpen (PG2202-04 collision)
        // Disabler her i Awake — Start() kjøres ikke før vi re-enabler via WeaponPickup.OnTriggerEnter
        // Include inactive pick-ups so a disabled prefab still gates shooting correctly.
        if (FindFirstObjectByType<WeaponPickup>(FindObjectsInactive.Include) != null)
            enabled = false;
    }

    private void Start()
    {
        currentAmmo = maxAmmo;
        audioSource = GetComponent<AudioSource>();

        // Henter Main Camera - må ha tag "MainCamera" (Unity setter dette automatisk)
        cam = Camera.main;

        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    private void Update()
    {
        // Skyt ikke hvis reloader eller spillet er pauset (Update kjører fortsatt; timeScale=0 gir deltaTime 0)
        if (isReloading) return;
        if (Time.timeScale == 0f) return;

        // Venstre museklikk - gammelt Input System (PG2202-02 scripting)
        if (Input.GetMouseButtonDown(0))
            TryShoot();

        // Manuell reload med R
        if (Input.GetKeyDown(KeyCode.R) && currentAmmo < maxAmmo)
            StartCoroutine(Reload());
    }

    private void TryShoot()
    {
        if (currentAmmo <= 0)
        {
            // Tom - spill klikke-lyd og start reload automatisk
            PlaySound(emptySound, emptyVolumeScale);
            StartCoroutine(Reload());
            return;
        }

        currentAmmo--;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
        OnWeaponFired?.Invoke();

        PlaySound(shootSound, shootVolumeScale);
        ShowMuzzleFlash();

        // Raycast fra midten av kameraet rett fremover
        // Dette treffer det kameraet sikter på, uavhengig av spillerens posisjon
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, range, Physics.AllLayers, QueryTriggerInteraction.Collide))
        {
            ShowHitEffect(hit.point, hit.normal);
            ZombieHealth zombie = hit.collider.GetComponentInParent<ZombieHealth>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage);
                // Vis flytende skadetall over zombie (PG2202-08)
                DamagePopup.Spawn(damage, hit.point);
            }
        }

        // Auto-reload når tom
        if (currentAmmo <= 0)
            StartCoroutine(Reload());
    }

    // Coroutine - venter reloadTime sekunder, deretter fyller opp ammo (PG2202-08)
    private IEnumerator Reload()
    {
        isReloading = true;
        PlaySound(reloadSound, reloadVolumeScale);

        yield return new WaitForSecondsRealtime(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    // Instansierer muzzle flash VFX (CFXR e.l. trenger ofte litt lengre levetid enn ett frame)
    private void ShowMuzzleFlash()
    {
        if (muzzleFlashPrefab == null || muzzlePoint == null) return;

        // Instantiate lager en kopi av prefaben i scenen (PG2202-08)
        GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Destroy(flash, Mathf.Max(0.05f, muzzleFlashLifetime));
    }

    // Treff-VFX - roterer effekten slik at den peker langs treff-normalen
    private void ShowHitEffect(Vector3 position, Vector3 normal)
    {
        if (hitEffectPrefab == null) return;

        GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
        Destroy(effect, 1f);
    }

    // Spiller lyd via AudioSource.PlayOneShot - overlapper ikke med seg selv (PG2202-10)
    private void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    // Getter for reload-status - HUD kan vise "Reloader..." tekst
    public bool IsReloading  => isReloading;
    public int  CurrentAmmo  => currentAmmo;
    public int  MaxAmmo      => maxAmmo;
}

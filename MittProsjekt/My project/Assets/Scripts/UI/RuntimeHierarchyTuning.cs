using UnityEngine;

// RuntimeHierarchyTuning — valgfritt panel i scenen (F10) for tuning uten EditorWindow (PG2202-08 UI via IMGUI).
// Pensum: endrer GameBalance-statics og kan kalle ZombieSpawner.ApplyRuntimeSpawnTuning.
// Ekstra: legg under GameplaySystems som tomt GameObject; i rapport: «runtime-verktøy i tillegg til CartoonZombies → Settings».
public class RuntimeHierarchyTuning : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.F10;
    [SerializeField] private bool startHidden = true;

    private bool _open;
    private ZombieSpawner _spawner;
    private GUIStyle _box;
    private bool _styles;

    private float _zDmg = 1.25f;
    private float _pGun = 0.82f;
    private float _scatter = 22f;
    private float _wideChance = 0.42f;
    private int _minZ = 14;

    private void Start()
    {
        _open = !startHidden;
        _spawner = FindFirstObjectByType<ZombieSpawner>();
        SyncFieldsFromStatics();
    }

    private void SyncFieldsFromStatics()
    {
        _zDmg     = GameBalance.ZombieDamageToPlayerMultiplier;
        _pGun     = GameBalance.PlayerGunDamageMultiplier;
        if (_spawner != null)
        {
            _scatter    = _spawner.RuntimeScatterRadius;
            _wideChance = _spawner.RuntimeWideScatterChance;
            _minZ       = _spawner.RuntimeMinimumZombiesAlive;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _open = !_open;
            if (_open) SyncFieldsFromStatics();
        }
    }

    private void OnGUI()
    {
        if (!_open) return;
        InitStyles();

        const float w = 320f;
        const float h = 420f;
        float x = 12f;
        float y = 60f;
        GUI.Box(new Rect(x, y, w, h), "  RUNTIME TUNING  [" + toggleKey + "]  ", _box);

        float lx = x + 12f;
        float ly = y + 36f;
        float line = 26f;

        GUI.Label(new Rect(lx, ly, w - 24, 22), "Zombie → player skade ×");
        _zDmg = GUI.HorizontalSlider(new Rect(lx, ly + 18, w - 36, 16), _zDmg, 0.5f, 2.5f);
        ly += line + 18f;

        GUI.Label(new Rect(lx, ly, w - 24, 22), "Player skudd → zombie ×");
        _pGun = GUI.HorizontalSlider(new Rect(lx, ly + 18, w - 36, 16), _pGun, 0.4f, 1.5f);
        ly += line + 18f;

        if (_spawner == null)
            _spawner = FindFirstObjectByType<ZombieSpawner>();

        if (_spawner != null)
        {
            GUI.Label(new Rect(lx, ly, w - 24, 40), "Spawn (ZombieSpawner i denne scenen)");
            ly += 22f;
            GUI.Label(new Rect(lx, ly, w - 24, 22), "Scatter-radius");
            _scatter = GUI.HorizontalSlider(new Rect(lx, ly + 18, w - 36, 16), _scatter, 0f, 60f);
            ly += line + 18f;
            GUI.Label(new Rect(lx, ly, w - 24, 22), "Wide-map sjanse (0–1)");
            _wideChance = GUI.HorizontalSlider(new Rect(lx, ly + 18, w - 36, 16), _wideChance, 0f, 1f);
            ly += line + 18f;
            GUI.Label(new Rect(lx, ly, w - 24, 22), "Minimum zombier levende");
            _minZ = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(lx, ly + 18, w - 36, 16), _minZ, 0, 40));
            ly += line + 18f;
        }

        ly += 8f;
        if (GUI.Button(new Rect(lx, ly, w - 24, 32), "Bruk verdier nå"))
        {
            GameBalance.ZombieDamageToPlayerMultiplier = _zDmg;
            GameBalance.PlayerGunDamageMultiplier      = _pGun;
            if (_spawner != null)
                _spawner.ApplyRuntimeSpawnTuning(_scatter, _wideChance, _minZ);
        }
        ly += 40f;

        if (GUI.Button(new Rect(lx, ly, w - 24, 28), "Reset GameBalance defaults"))
        {
            GameBalance.ResetToDefaults();
            SyncFieldsFromStatics();
        }
        ly += 34f;

        GUI.Label(new Rect(lx, ly, w - 24, 60),
            "Tips: NavMesh/zombie-på-vann fikses i editor (Bake, layers). Se Documentation/FREMGANGSMATE_…");
    }

    private void InitStyles()
    {
        if (_styles) return;
        _styles = true;
        _box = new GUIStyle(GUI.skin.box) { fontSize = 13, fontStyle = FontStyle.Bold };
    }
}

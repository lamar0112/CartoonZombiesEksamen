using UnityEngine;
using UnityEngine.UI;

// EnemyCompassHUD — UI-pil mot mål: MissionManager, siste zombie, utgang, ZoneTrigger (PG2202-08 UI; PG2202-02 løs kobling).
// Pensum: RectTransform-rotasjon mot world-posisjon; skjul når ingen mål.
// Ekstra: prioriteringskjede (oppdrag → zombie → exit) gir tydelig veiledning utover ren «nearest enemy».
[DisallowMultipleComponent]
public class EnemyCompassHUD : MonoBehaviour
{
    [SerializeField] private RectTransform arrow;
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Skalerer pil-UI for bedre lesbarhet (påvirker ikke din TMP-HUD).")]
    [SerializeField] private float arrowVisualScale = 1.35f;

    private ZombieSpawner _spawner;
    private Transform     _player;
    private Transform     _missionTarget; // satt av MissionManager for å peke mot aktivt oppdrag

    private void Start()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable     = false;
        }
        if (arrow != null && !Mathf.Approximately(arrowVisualScale, 1f))
            arrow.localScale = new Vector3(arrowVisualScale, arrowVisualScale, 1f);
        ResolveRefs();
    }

    private void Update()
    {
        if (arrow == null) return;

        ResolveRefs();
        if (_player == null)
        {
            SetVisible(false);
            return;
        }

        // Oppdragspil må virke selv om ZombieSpawner mangler eller ikke er konfigurert ennå.
        Transform target = null;
        if (_missionTarget != null)
            target = _missionTarget;
        else if (MissionManager.Instance != null && MissionManager.Instance.ShouldCompassPreferNearestZombie())
            target = FindNearestLivingZombieTransform();
        else if (_spawner != null)
        {
            if (_spawner.ZombiesAlive == 1)
                target = FindOnlyLivingZombie();
            else if (_spawner.AllWavesDone && _spawner.ZombiesAlive == 0)
                target = FindExitOrObjectiveTransform();
        }

        if (target == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 flat = target.position - _player.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.25f)
        {
            SetVisible(false);
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        Vector3 camFwd = cam.transform.forward;
        camFwd.y = 0f;
        if (camFwd.sqrMagnitude < 0.0001f) return;
        camFwd.Normalize();

        float signed = Vector3.SignedAngle(camFwd, flat.normalized, Vector3.up);
        arrow.localEulerAngles = new Vector3(0f, 0f, -signed);
    }

    private void ResolveRefs()
    {
        if (_spawner == null)
            _spawner = FindFirstObjectByType<ZombieSpawner>();
        if (_player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
    }

    private static Transform FindNearestLivingZombieTransform()
    {
        ZombieHealth[] all = FindObjectsByType<ZombieHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Transform best = null;
        float bestD = float.MaxValue;
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        Vector3 p = playerGo != null ? playerGo.transform.position : Vector3.zero;

        foreach (ZombieHealth z in all)
        {
            if (z == null || z.IsDead) continue;
            float d = Vector3.SqrMagnitude(z.transform.position - p);
            if (d < bestD)
            {
                bestD = d;
                best = z.transform;
            }
        }
        return best;
    }

    private static Transform FindOnlyLivingZombie()
    {
        ZombieHealth[] all = FindObjectsByType<ZombieHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Transform found = null;
        int n = 0;
        foreach (ZombieHealth z in all)
        {
            if (z == null || z.IsDead) continue;
            n++;
            found = z.transform;
        }
        return n == 1 ? found : null;
    }

    /// <summary>Først eksplisitt markør i scenen, ellers ZoneTrigger.</summary>
    private static Transform FindExitOrObjectiveTransform()
    {
        CompassObjectiveMarker[] markers =
            FindObjectsByType<CompassObjectiveMarker>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (markers != null && markers.Length > 0 && markers[0] != null)
            return markers[0].GetExitTarget();

        ZoneTrigger zt = FindFirstObjectByType<ZoneTrigger>();
        return zt != null ? zt.transform : null;
    }

    // Kalles fra MissionManager når et oppdrag med pil er aktivt
    public void SetMissionTarget(Transform t)   => _missionTarget = t;
    public void ClearMissionTarget()            => _missionTarget = null;

    private void SetVisible(bool on)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = on ? 1f : 0f;
        else if (arrow != null)
            arrow.gameObject.SetActive(on);
    }
}

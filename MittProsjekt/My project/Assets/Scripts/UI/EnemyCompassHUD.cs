using UnityEngine;
using UnityEngine.UI;

// Pil mot siste zombie (når én gjenstår), eller mot ZoneTrigger når alle bølger er ferdig (PG2202-08).
[DisallowMultipleComponent]
public class EnemyCompassHUD : MonoBehaviour
{
    [SerializeField] private RectTransform arrow;
    [SerializeField] private CanvasGroup canvasGroup;

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

        // Mission arrow must work even if ZombieSpawner is missing or not configured yet.
        Transform target = null;
        if (_missionTarget != null)
            target = _missionTarget;
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

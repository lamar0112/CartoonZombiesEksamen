using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tredjeperson CharacterController-bevegelse med mus-rotasjon (PG2202-04)
// CharacterController håndterer kollisjoner uten Rigidbody
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed        = 5f;
    [SerializeField] private float jumpForce        = 9f;   // Hopp-kraft (PG2202-04)
    [SerializeField] private float gravity          = -20f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Void recovery (under map / safety collider)")]
    [Tooltip("World Y der spillbar bakke ca. ligger (ofte ~0). Sett lik en gulv-flises Position Y om hele kartet er flyttet.")]
    [SerializeField] private float approximatePlayableGroundY = 0f;
    [Tooltip("Recovery starter når spiller-Y er så langt under «bakken» (unngår tidlig trigger i hopp/kanter).")]
    [SerializeField] private float fallDepthBeforeVoidRecover = 12f;
    [SerializeField] private float voidRecoverCooldown        = 0.85f;

    private CharacterController cc;
    private Vector3 verticalVelocity;
    private float   nextVoidRecoverTime;
    private float   voidRecoverBelowYCached;
    private Vector3 lastDryGroundPosition;
    private Animator locomotionAnimator;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Låser og skjuler musepeker under spilling
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Valgfritt: tomt GameObject «GroundLevelReference» i scenen — bruk dets Y som bakkenivå.
        GameObject groundRef = GameObject.Find("GroundLevelReference");
        float groundY = approximatePlayableGroundY;
        if (groundRef != null)
            groundY = groundRef.transform.position.y;
        voidRecoverBelowYCached = groundY - fallDepthBeforeVoidRecover;
        locomotionAnimator = GetComponentInChildren<Animator>();
        if (locomotionAnimator != null)
            locomotionAnimator.applyRootMotion = false;
        lastDryGroundPosition = transform.position;

        StartCoroutine(AlignSpawnAfterPhysicsReady());
    }

    /// <summary>Wait a frame so MeshColliders from loaded scenes are registered before raycast snap.</summary>
    private IEnumerator AlignSpawnAfterPhysicsReady()
    {
        yield return null;
        AlignSpawnToGroundInLevelScenes();
        yield return null;
        AlignSpawnToGroundInLevelScenes();
    }

    /// <summary>After load, snap feet to first solid mesh under the player (spawn Y is often wrong vs Kenney roads).</summary>
    private void AlignSpawnToGroundInLevelScenes()
    {
        string sn = SceneManager.GetActiveScene().name;
        if (sn != "Level01_By" && sn != "Level02_StrandSkog") return;

        Vector3 p = transform.position;
        Vector3 origin = p + Vector3.up * 800f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2000f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return;
        if (WaterDetection.IsWaterCollider(hit.collider)) return;

        float stand = cc.height * 0.5f + cc.skinWidth + 0.05f;
        float targetY = hit.point.y + stand;
        if (Mathf.Abs(p.y - targetY) > 0.25f || p.y < hit.point.y + 0.08f)
        {
            transform.position = new Vector3(p.x, targetY, p.z);
            Physics.SyncTransforms();
            lastDryGroundPosition = transform.position;
        }
    }

    private void Update()
    {
        // Ikke beveg spilleren eller roter kamera når spillet er pauset / cheat-meny
        if (Time.timeScale == 0f) return;
        if (CheatMenu.Instance != null && CheatMenu.Instance.IsCheatMenuOpen) return;

        TryRejectStandingOnWater();

        // Horisontal rotasjon av spilleren med musa (venstre/høyre)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        transform.Rotate(0f, mouseX, 0f);

        // WASD-bevegelse relativt til spillerens fremoverretning (PG2202-04)
        float h    = Input.GetAxis("Horizontal"); // A/D
        float v    = Input.GetAxis("Vertical");   // W/S
        Vector3 move = transform.right * h + transform.forward * v;
        move *= moveSpeed * Time.deltaTime;
        if (ShouldBlockMoveForWater(move))
            move = Vector3.zero;
        cc.Move(move);

        // Hopp (mellomrom) — bare når spilleren er på bakken (PG2202-04)
        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space))
            verticalVelocity.y = jumpForce;

        // Tyngdekraft — CharacterController har ikke innebygd fysikk (PG2202-04)
        if (cc.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f;
        verticalVelocity.y += gravity * Time.deltaTime;
        cc.Move(verticalVelocity * Time.deltaTime);

        DriveLocomotionAnimator(h, v);
        if (cc.isGrounded && !FootGroundIsWater())
            lastDryGroundPosition = transform.position;

        TryRecoverFromVoid();
    }

    private void DriveLocomotionAnimator(float h, float v)
    {
        if (locomotionAnimator == null) return;

        locomotionAnimator.SetFloat("Hor", h);
        locomotionAnimator.SetFloat("Vert", v);

        Vector3 planar = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
        locomotionAnimator.SetFloat("State", Mathf.Clamp01(planar.magnitude / Mathf.Max(0.01f, moveSpeed)));
        locomotionAnimator.SetBool("IsJump", !cc.isGrounded && verticalVelocity.y > 0.35f);
    }

    private void TryRejectStandingOnWater()
    {
        if (!IsWaterBlockScene()) return;
        if (!cc.isGrounded) return;
        if (!FootGroundIsWater()) return;

        cc.enabled = false;
        transform.position = lastDryGroundPosition + Vector3.up * 0.12f;
        verticalVelocity.y = -2f;
        Physics.SyncTransforms();
        cc.enabled = true;
    }

    private bool IsWaterBlockScene()
    {
        string sn = SceneManager.GetActiveScene().name;
        return sn == "Level01_By" || sn == "Level02_StrandSkog";
    }

    private bool FootGroundIsWater()
    {
        Vector3 o = transform.position + Vector3.up * 0.35f;
        if (!Physics.Raycast(o, Vector3.down, out RaycastHit hit, 6f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;
        return WaterDetection.IsWaterCollider(hit.collider);
    }

    /// <summary>
    /// Under the level, a downward ray from the sky often hits roofs first — use the safety box top instead.
    /// Throttled + CC disable to avoid jitter inside CharacterController.
    /// </summary>
    private bool ShouldBlockMoveForWater(Vector3 horizontalDelta)
    {
        if (!IsWaterBlockScene()) return false;
        if (horizontalDelta.sqrMagnitude < 1e-8f) return false;

        Vector3 next = transform.position + horizontalDelta;
        return GroundUnderPointIsWater(new Vector3(next.x, transform.position.y, next.z));
    }

    private bool GroundUnderPointIsWater(Vector3 worldPoint)
    {
        Vector3 from = new Vector3(worldPoint.x, transform.position.y + 8f, worldPoint.z);
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 80f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return WaterDetection.IsWaterCollider(hit.collider);
        return false;
    }

    private void TryRecoverFromVoid()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != "Level01_By" && scene != "Level02_StrandSkog") return;
        if (transform.position.y >= voidRecoverBelowYCached) return;
        if (Time.time < nextVoidRecoverTime) return;

        nextVoidRecoverTime = Time.time + voidRecoverCooldown;

        cc.enabled = false;
        if (!TryPlaceOnSafetyGroundTop())
            AlignSpawnToGroundInLevelScenes();
        verticalVelocity.y = -2f;
        Physics.SyncTransforms();
        cc.enabled = true;
    }

    private bool TryPlaceOnSafetyGroundTop()
    {
        GameObject sg = GameObject.Find("_SafetyGround");
        if (sg == null || !sg.TryGetComponent(out BoxCollider box))
            return false;

        float topY  = box.bounds.max.y;
        float stand = cc.height * 0.5f + cc.skinWidth + 0.12f;
        Vector3 p   = transform.position;
        transform.position = new Vector3(p.x, topY + stand, p.z);
        return true;
    }

    // Kalles av PauseMenu og CheatMenu for å låse/låse opp musen
    public void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}

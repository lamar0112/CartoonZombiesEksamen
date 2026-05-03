using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Etter MainMenu → Level01/02: SpawnPoints ved origo mens by-miljø er flyttet gir «tom himmel».
// Flytter spiller til synlig kart og kobler hovedkamera på nytt.
public static class GameplaySceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameSceneNames.Level01By && scene.name != GameSceneNames.Level02StrandSkog)
            return;

        var go = new GameObject("_SceneLoadFix");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<SceneLoadFixRunner>();
    }

    private sealed class SceneLoadFixRunner : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Én frame: MeshColliders / spiller-scripts har initialisert
            yield return null;
            Apply();
            Destroy(gameObject);
        }

        private static void Apply()
        {
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null) return;

            CharacterController cc = playerGo.GetComponent<CharacterController>();
            bool wasCcOn = cc != null && cc.enabled;
            if (cc != null) cc.enabled = false;

            if (LevelWorldBoundsUtil.TryGetPlayableWorldBounds(out Bounds wb))
            {
                Vector3 p = playerGo.transform.position;
                float xzExtent = Mathf.Max(wb.extents.x, wb.extents.z);
                float maxOkXZ    = Mathf.Max(80f, xzExtent * 0.65f);
                float horizontal = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(wb.center.x, wb.center.z));

                bool underMap  = p.y < wb.min.y - 12f;
                bool overMap   = p.y > wb.max.y + 120f;
                bool lostInXZ  = horizontal > maxOkXZ;

                if (underMap || overMap || lostInXZ)
                {
                    Vector3 probe = new Vector3(wb.center.x, wb.max.y + 120f, wb.center.z);
                    if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 600f,
                            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        float stand = cc != null
                            ? cc.height * 0.5f + cc.skinWidth + 0.1f
                            : 1.1f;
                        playerGo.transform.position = hit.point + Vector3.up * stand;
                        Physics.SyncTransforms();
                    }
                }
            }

            EnsureMainCameraFollows(playerGo.transform);

            if (cc != null) cc.enabled = wasCcOn;
        }

        private static void EnsureMainCameraFollows(Transform player)
        {
            Scene active = SceneManager.GetActiveScene();

            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (c == null || !c.CompareTag("MainCamera")) continue;
                if (c.gameObject.scene != active)
                    c.gameObject.SetActive(false);
            }

            Camera[] inLevel = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c != null && c.gameObject.scene == active).ToArray();

            Camera main = inLevel.FirstOrDefault(c =>
                c.CompareTag("MainCamera") && c.isActiveAndEnabled);

            if (main == null)
                main = inLevel.FirstOrDefault(c => c.isActiveAndEnabled);

            if (main == null) return;

            foreach (Camera c in inLevel)
            {
                if (c == main) continue;
                if (c.CompareTag("MainCamera"))
                    c.tag = "Untagged";
            }

            if (!main.CompareTag("MainCamera"))
                main.tag = "MainCamera";

            CameraFollow cf = main.GetComponent<CameraFollow>();
            if (cf == null)
                cf = main.gameObject.AddComponent<CameraFollow>();

            cf.SetTarget(player);
            cf.SnapToTargetNow();
        }
    }
}

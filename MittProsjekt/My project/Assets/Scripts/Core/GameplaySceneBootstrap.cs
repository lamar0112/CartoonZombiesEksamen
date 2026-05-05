using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// GameplaySceneBootstrap — kjører automatisk etter at Level01/Level02 er lastet (RuntimeInitializeOnLoad + sceneLoaded).
// Pensum: SceneManager API, UI Image som enkel overlay (PG2202-08).
// Ekstra: pipeline for store asset-pack kart der spawner/spiller står i «tom himmel» før miljøet er plassert — ikke pensum-eksempel,
// men vanlig i eksamensprosjekter med ferdig by; vi skjuler korte hakk med svart lerret og retter kamera/spiller.
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
            // Svart overlay skjuler glitch-frames umiddelbart (PG2202-08 polish)
            Image overlayImg = CreateBlackOverlay();

            // Extra frames: MeshColliders, CharacterController og NavMesh initialiseres (store levels / disk load)
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Apply();

            // 2 ekstra frames for fysikk-settling
            yield return null;
            yield return null;

            // Fade overlay fra svart til gjennomsiktig over 0.7 sekunder
            if (overlayImg != null)
                yield return StartCoroutine(FadeOverlay(overlayImg, 0.7f));

            Destroy(gameObject);
        }

        private Image CreateBlackOverlay()
        {
            var root = new GameObject("_FadeOverlay");
            SceneManager.MoveGameObjectToScene(root, gameObject.scene);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            root.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("Fill");
            imgGo.transform.SetParent(root.transform, false);

            var img = imgGo.AddComponent<Image>();
            img.color = Color.black;

            var rt = imgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return img;
        }

        private static IEnumerator FadeOverlay(Image img, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && img != null)
            {
                elapsed += Time.unscaledDeltaTime;
                Color c = img.color;
                c.a       = 1f - Mathf.Clamp01(elapsed / duration);
                img.color = c;
                yield return null;
            }
            if (img != null)
                Destroy(img.transform.root.gameObject);
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
                float xzExtent  = Mathf.Max(wb.extents.x, wb.extents.z);
                float maxOkXZ   = Mathf.Max(80f, xzExtent * 0.65f);
                float horizontal = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(wb.center.x, wb.center.z));

                bool underMap = p.y < wb.min.y - 12f;
                bool overMap  = p.y > wb.max.y + 120f;
                bool lostInXZ = horizontal > maxOkXZ;

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

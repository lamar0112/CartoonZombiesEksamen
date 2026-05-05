using UnityEngine;
using UnityEngine.AI;

/// <summary>Raycast for fothøyde — hopper over egen kollisjon og vann.</summary>
public static class ZombieSnapPositionUtility
{
    private const float RayHeight   = 140f;
    private const float RayLength = 450f;

    public static bool TryFeetOnSolidGround(Vector3 xzPivot, GameObject skipOwnHierarchy, out Vector3 worldPos)
    {
        worldPos = xzPivot;
        Vector3 rayStart = xzPivot + Vector3.up * RayHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, RayLength,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit gh in hits)
        {
            if (skipOwnHierarchy != null &&
                gh.collider != null &&
                gh.collider.transform.IsChildOf(skipOwnHierarchy.transform))
                continue;

            if (WaterDetection.IsWaterCollider(gh.collider)) continue;

            float y = gh.point.y + 0.42f;
            if (skipOwnHierarchy != null)
            {
                var rend = skipOwnHierarchy.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    float pivotToBottom = skipOwnHierarchy.transform.position.y - rend.bounds.min.y;
                    if (pivotToBottom > 0.02f)
                        y = Mathf.Max(y, gh.point.y + pivotToBottom + 0.14f);
                }
                else
                    y = gh.point.y + 0.62f;
            }

            worldPos = new Vector3(xzPivot.x, y, xzPivot.z);
            return true;
        }

        return false;
    }

    public static void SnapAgentToGround(GameObject z)
    {
        if (z == null) return;

        Vector3 p = z.transform.position;
        Vector3 placed;

        if (!TryFeetOnSolidGround(p, z, out placed) &&
            NavMesh.SamplePosition(p, out NavMeshHit nmHit, 22f, NavMesh.AllAreas) &&
            !TryFeetOnSolidGround(nmHit.position, z, out placed))
        {
            placed = new Vector3(nmHit.position.x, nmHit.position.y + 0.28f, nmHit.position.z);
        }

        z.transform.position = placed;
        Physics.SyncTransforms();

        if (z.TryGetComponent(out NavMeshAgent agent) &&
            NavMesh.SamplePosition(placed, out NavMeshHit warpHit, 22f, NavMesh.AllAreas))
        {
            Vector3 w = warpHit.position;
            w.y = Mathf.Max(w.y + 0.12f, placed.y);
            agent.Warp(w);
            z.transform.position = new Vector3(w.x, w.y, w.z);
            Physics.SyncTransforms();
        }
        else if (z.TryGetComponent(out NavMeshAgent badAgent))
        {
            // Keep feet out of the ground even when the navmesh sample fails (common right after bake).
            badAgent.enabled = false;
            TryFeetOnSolidGround(z.transform.position, z, out Vector3 grounded);
            z.transform.position = grounded;
            Physics.SyncTransforms();
            badAgent.enabled = true;
        }
    }

    public static bool IsStandingOnWater(Transform t, float up = 1.2f, float down = 4f)
    {
        if (t == null) return false;
        Vector3 o = t.position + Vector3.up * up;
        if (!Physics.Raycast(o, Vector3.down, out RaycastHit hit, down,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;
        if (hit.collider != null && hit.collider.transform.IsChildOf(t)) return false;
        return WaterDetection.IsWaterCollider(hit.collider);
    }
}

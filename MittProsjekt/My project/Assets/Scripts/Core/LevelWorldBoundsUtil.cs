using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime AABB for level geometry — samme rot-navn som LevelSceneRepair / SceneSetupTool.</summary>
public static class LevelWorldBoundsUtil
{
    public static bool TryGetPlayableWorldBounds(out Bounds worldBounds)
    {
        worldBounds = default;
        bool any = false;

        var names = new List<string>();
        names.AddRange(LevelMapRootResolver.CityMapRootCandidates);
        names.AddRange(LevelMapRootResolver.BeachMapRootCandidates);
        names.Add("EnvironmentArt");
        names.Add("Environment");

        var processed = new HashSet<GameObject>();
        foreach (string n in names)
        {
            if (string.IsNullOrEmpty(n)) continue;
            GameObject go = GameObject.Find(n);
            if (go == null || !processed.Add(go)) continue;

            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null || !r.enabled) continue;
                if (!any)
                {
                    worldBounds = r.bounds;
                    any         = true;
                }
                else
                {
                    worldBounds.Encapsulate(r.bounds);
                }
            }
        }

        return any;
    }
}

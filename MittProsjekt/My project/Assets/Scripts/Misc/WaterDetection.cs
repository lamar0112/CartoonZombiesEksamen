using UnityEngine;

// WaterDetection — statisk sjekk: tag Water, layer Water, navn inneholder water/vann/hav (PG2202-04 layers/tags).
// Pensum: LayerMask.NameToLayer; CompareTag.
// Ekstra: navne-heuristikk for tredjeparts-mesher (Kenney m.fl.) der prefabs ikke alltid har riktig tag — dokumenter i rapport.
public static class WaterDetection
{
    public static bool IsWaterCollider(Collider col)
    {
        if (col == null) return false;
        if (col.CompareTag("Water")) return true;

        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0 && col.gameObject.layer == waterLayer)
            return true;

        return IsWaterObjectName(col.gameObject.name);
    }

    public static bool IsWaterObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return false;
        string n = objectName.ToLowerInvariant();
        return n.Contains("water") || n.Contains("vann") || n.Contains("flat water")
            || n.Contains("ocean") || n.Contains("sea") || n.Contains("lake")
            || n.Contains("river") || n.Contains("waves");
    }

    // Raycast ned fra litt over punktet — brukes for spawn-sjekk (unngå NavMesh på vann-plan).
    public static bool GroundUnderPointIsLikelyWater(Vector3 worldPoint, float rayStartYOffset = 120f)
    {
        Vector3 from = worldPoint + Vector3.up * rayStartYOffset;
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, rayStartYOffset + 200f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return IsWaterCollider(hit.collider);
        return false;
    }
}

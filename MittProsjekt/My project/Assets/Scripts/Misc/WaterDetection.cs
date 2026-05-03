using UnityEngine;

/// <summary>Delt logikk for vann — tag, layer og vanlige prefab-navn.</summary>
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
}

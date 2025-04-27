using UnityEngine;
using System.Collections.Generic;

public class HexUtils : MonoBehaviour
{
    
    public static HexTile GetRandomHexTile(Dictionary<Vector2Int, HexTile> hexMap)
    {
        if (hexMap == null || hexMap.Count == 0)
            return null;

        int randomIndex = Random.Range(0, hexMap.Count);
        int i = 0;

        foreach (var kvp in hexMap)
        {
            if (i == randomIndex)
                return kvp.Value;
            i++;
        }

        return null; // fallback (shouldn't hit)
    }
}

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.TerrainTools;

public class HexUtils : MonoBehaviour
{
    private static Vector2Int[] neighborOffsets = {new Vector2Int(+1, 0), new Vector2Int(+1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, +1), new Vector2Int(0, +1)};
        
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

    // call this with radius to get additional rings.   If called often, may want to keep radius < 5 for performance. 
    public static List<HexTile> GetHexTileNeighbours(HexTile centerTile, Dictionary<Vector2Int, HexTile> tileMap, int radius = 1)
    {
        List<HexTile> neighbors = new List<HexTile>();
        Vector2Int centerCoords = centerTile.gridCoords;
        
        if (radius <= 0) return neighbors; // an empty List

        HashSet<Vector2Int> visitedCoords = new HashSet<Vector2Int>();

        for (int r = 1; r <= radius; r++)
        {
            foreach (Vector2Int direction in neighborOffsets)
            {
                // Start from a tile r steps in this direction
                Vector2Int current = centerCoords + direction * r;

                // Now walk around the ring in 6 directions
                for (int i = 0; i < 6; i++)
                {
                    Vector2Int stepDir = neighborOffsets[i];
                    for (int j = 0; j < r; j++)
                    {
                        if (tileMap.TryGetValue(current, out HexTile tile) && visitedCoords.Add(current))
                        {
                            neighbors.Add(tile);
                        }
                        current += stepDir;
                    }
                }
            }
        }

        return neighbors;
    }

    public static List<HexTile> GetHexRing(Vector2Int centerCoords, Dictionary<Vector2Int, HexTile> tileMap, int ring)
    {
        List<HexTile> ringTiles = new List<HexTile>();

        if (ring <= 0) return ringTiles;

        Vector2Int current = centerCoords + neighborOffsets[4] * ring;  // start at one hex in a consistent direction
        
        for (int i = 0; i < 6; i++)  // 6 sides
        {
            for (int j = 0; j < ring; j++)  // each side length is `ring`
            {
                if (tileMap.TryGetValue(current, out HexTile tile))
                {
                    ringTiles.Add(tile);
                }
                current += neighborOffsets[i];
            }
        }

        return ringTiles;
    }

    public static void UpdateHexTileNeighbours(HexTile thisTileScript, Dictionary<Vector2Int, HexTile> tileMap)
    {
        thisTileScript.neighbors.Clear();

        foreach (Vector2Int offset in neighborOffsets)
        {
            Vector2Int neighborCoords = thisTileScript.gridCoords + offset;
            if (tileMap.TryGetValue(neighborCoords, out HexTile neighborTile))
            {
                thisTileScript.neighbors.Add(neighborTile);
            }
        }
    }

    public static void UpdateHexMapNeighbours(Dictionary<Vector2Int, HexTile> tileMap)
    {        
        foreach (HexTile tile in tileMap.Values)
        {
            tile.neighbors.Clear();

            foreach (Vector2Int offset in neighborOffsets)
            {
                Vector2Int neighborCoords = tile.gridCoords + offset;
                if (tileMap.TryGetValue(neighborCoords, out HexTile neighborTile))
                {
                    tile.neighbors.Add(neighborTile);
                }
            }
        }
    }

}

using UnityEngine;
using System.Collections.Generic;

public class HexUtils : MonoBehaviour
{
    private static Vector2Int[] neighborOffsets = { new Vector2Int(+1, 0), new Vector2Int(+1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, +1), new Vector2Int(0, +1) };

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
    
    /////////////////////////////
    //HEXMAP GENERATION HELPERS
    /////////////////////////////
    static public (List<Vector3>, List<Vector2Int>) GenerateGridPositions(int numRows, int numCols, float hexSize, bool isFlatTop)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector2Int> gridCoords = new List<Vector2Int>();

        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                Vector3 pos = isFlatTop ? GetFlatTopHexPosition(row, col, hexSize)
                                        : GetPointedTopHexPosition(row, col, hexSize);
                positions.Add(pos);
                gridCoords.Add(new Vector2Int(row, col));
            }
        }
        return (positions, gridCoords);
    }

    static public Vector3 GetFlatTopHexPosition(int row, int col, float size)
    {
        float x = col * (1.5f * size);
        float z = row * (Mathf.Sqrt(3) * size) + (col % 2) * (Mathf.Sqrt(3) / 2 * size);
        return new Vector3(x, 0, z);
    }

    static public Vector3 GetPointedTopHexPosition(int row, int col, float size)
    {
        float x = col * (Mathf.Sqrt(3) * size) + (row % 2) * (Mathf.Sqrt(3) / 2 * size);
        float z = row * (1.5f * size);
        return new Vector3(x, 0, z);
    }

    static public (List<Vector3>, List<Vector2Int>) GeneratePinwheelPositions(float radius, float hexSize)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector2Int> gridCoords = new List<Vector2Int>();

        int maxRings = Mathf.CeilToInt(radius / (1.5f * hexSize)); // Approximate ring count
        positions.Add(Vector3.zero); // Center hex

        for (int ring = 1; ring <= maxRings; ring++)
        {
            for (int i = 0; i < 6; i++) // 6 sides of a hexagon
            {
                for (int j = 0; j < ring; j++)
                {
                    float angle = (i * 60) * Mathf.Deg2Rad; // Convert degrees to radians
                    float x = ring * hexSize * Mathf.Cos(angle) + j * hexSize * Mathf.Cos(angle + Mathf.PI / 3);
                    float z = ring * hexSize * Mathf.Sin(angle) + j * hexSize * Mathf.Sin(angle + Mathf.PI / 3);
                    positions.Add(new Vector3(x, 0, z));
                    gridCoords.Add(new Vector2Int(i, j));
                }
            }
        }
        return (positions, gridCoords);
    }

    static public (List<Vector3>, List<Vector2Int>) GenerateCirclePositions(float radius, float roughness, float hexSize, bool isFlatTop)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector2Int> gridCoords = new List<Vector2Int>();
        int maxRings = Mathf.CeilToInt(radius / hexSize) + 1;

        System.Random rand = new System.Random(); // Seeded RNG for consistency

        for (int q = -maxRings; q <= maxRings; q++)
        {
            for (int r = -maxRings; r <= maxRings; r++)
            {
                Vector3 worldPos = AxialToWorld(q, r, hexSize, isFlatTop);
                float distance = Vector3.Distance(Vector3.zero, worldPos);

                // Check if the hex is within the intended radius
                if (distance <= radius)
                {
                    // Apply randomness to remove hexes at the edges
                    if (distance > radius * 0.75f) // Only affect outer region
                    {
                        float noise = (float)rand.NextDouble(); // Random number between 0 and 1
                        if (noise < roughness) continue; // Skip this hex to create holes
                    }
                    
                    positions.Add(worldPos);
                    gridCoords.Add(new Vector2Int(q, r));
                }
            }
        }
        return (positions, gridCoords);
    }
    
    static public (List<Vector3>, List<Vector2Int>) GenerateHexagonPositions(float radius, float hexSize, bool isFlatTop)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector2Int> gridCoords = new List<Vector2Int>();

        // Center hex
        positions.Add(Vector3.zero);

        int maxRings = Mathf.FloorToInt(radius / (1.5f * hexSize));

        for (int q = -maxRings; q <= maxRings; q++)
        {
            for (int r = Mathf.Max(-maxRings, -q - maxRings); r <= Mathf.Min(maxRings, -q + maxRings); r++)
            {
                int s = -q - r; // The third axial coordinate (q + r + s = 0)
                Vector3 pos = AxialToWorld(q, r, hexSize, isFlatTop);
                positions.Add(pos);
                gridCoords.Add(new Vector2Int(q, r));
            }
        }
        return (positions, gridCoords);
    }

    // Convert axial coordinates (q, r) to world-space positions
    static private Vector3 AxialToWorld(int q, int r, float hexSize, bool isFlatTop)
    {
        float x, z;
        if (isFlatTop)
        {
            x = hexSize * 1.5f * q;
            z = hexSize * Mathf.Sqrt(3) * (r + q / 2f);
        }
        else
        {
            x = hexSize * Mathf.Sqrt(3) * (q + r / 2f);
            z = hexSize * 1.5f * r;
        }
        return new Vector3(x, 0, z);
    }
}

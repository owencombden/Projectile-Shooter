using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance; 
    public enum SpawnMode { Grid, Circle, Pinwheel, Hexagon }
    public int numRows = 5;
    public int numCols = 6;
    public float hexSize = 1f;  // Assumes hex ratio is 2 x Sqrt(3).  2f = twice as big.  0.5f = 1/2 as big.
    public bool isFlatTop = true; // Toggle between flat-top and pointed-top
    public SpawnMode spawnMode = SpawnMode.Grid;
    public float circleRadius = 5f; // Used only for circle mode
    public float edgeRaggedness = 0;  // 0 = perfect cirle, 0.5f pretty ragged, 0.8f very ragged.

    private System.Random random = new System.Random();
    
    Vector2Int[] neighborOffsets = {new Vector2Int(+1, 0), new Vector2Int(+1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, +1), new Vector2Int(0, +1)};

    // store all platforms in a dictionary of dictionaries
    // outer dictionary stores all the platforms, indexable by platform ID
    // inner dictionary stores all of the hexTiles for that platform, indexable by grid-coord    
    // platforms[platformID]                 -> get all tiles for that platform
    // platforms[platformID][gridPos]        -> get any tile in O(1)
    // platforms[platformID].Remove(gridPos) -> remove tile from dictionary
    private int platformCounter = 0;
    private Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms = new();  // 2D hexmaps
    private Dictionary<int, GameObject> platformGameObjects = new();             // gameobjs  
    
    // could also implement this HashSet if things get slow
    // an inner hashset is faster, could be good if wanted to apply something across all tiles (ie: collision detection?)
    // would have to keep the Dict(Dict) and maintain two collections when adding/removing tiles and platforms
    // private Dictionary<int, HashSet<HexTile>> activeTiles = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        int numPlatforms = GameManager.Instance.numberOfAIBots + 1;  // one platform for each AI, and an extra one for player
        
        Vector3[] platformPositions = new Vector3[]
        {
            new Vector3(3, 0, 20),
            new Vector3(40, 0, 55),
            new Vector3(-30, 0, 57),
            new Vector3(6, 0, 90)
        };

        if(numPlatforms > platformPositions.Count())
        {
            Debug.Log("Not enough platforms!!!");
            return;
        }

        // spawn all platforms
        for (int i = 0; i < numPlatforms; i++)
        {
            BuildPlatform(platformPositions[i], i);
        }
        
        // spawn all the characters
        GameManager.Instance.SpawnAllCharacters(platforms);
        
    }

    private void BuildPlatform(Vector3 startPos, int id)
    {
        List<Vector3>     hexPositions = new List<Vector3>();
        List<Vector2Int> hexGridCoords = new List<Vector2Int>();

        if (spawnMode == SpawnMode.Grid)
        {
            (hexPositions, hexGridCoords) = GenerateGridPositions();
        }
        else if (spawnMode == SpawnMode.Circle)
        {
            (hexPositions, hexGridCoords) = GenerateCirclePositions(circleRadius, edgeRaggedness);
        }
        else if (spawnMode == SpawnMode.Pinwheel)
        {
            (hexPositions, hexGridCoords) = GeneratePinwheelPositions(circleRadius);
        }
        else if (spawnMode == SpawnMode.Hexagon)
        {
            (hexPositions, hexGridCoords) = GenerateHexagonPositions(circleRadius);
        }

        // move the positions to the startPos param
        for (int i=0; i < hexPositions.Count; i++) { hexPositions[i] += startPos; }
        
        if (hexPositions.Count != hexGridCoords.Count)
        {
            Debug.Log("BuildHexMap error!  Mismatched positions and gridcoords.");
            return;
        }

        // get an empty parent object (platform) that will hold all of the tiles we're about to spawn
        GameObject thisPlatform = AssetManager.Instance.GetPlatform(startPos, Quaternion.identity);     // PASS HEXTILEPOSITIONS & GRID COORDS HERE, AND BUILD A FULL PLATFORM IN AM.
        
        // build one hex platform (a dictionary of grid-coords to hexTileScripts) and add it to the platforms (outer) dictionary with an ID.
        Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();

        // give the platform script to handle pickups (ammo, health, etc)
        PickupPlatformData pickupScript = thisPlatform.AddComponent<PickupPlatformData>();
        pickupScript.platformId = id;
        pickupScript.nextSpawnTime = 5f;
        pickupScript.hexMap = hexMap;

        //spawn the hex tiles at each position, store coords, and add to dictionary
        for (int i=0; i <hexPositions.Count; i++)
        {   
            GameObject thisHexTile = AssetManager.Instance.GetHexTile(hexPositions[i], Quaternion.identity);
            thisHexTile.transform.parent = thisPlatform.transform;
            HexTile hexScript = thisHexTile.GetComponent<HexTile>();
            hexScript.gridCoords = hexGridCoords[i];
            hexMap[hexGridCoords[i]] = hexScript;
        }

        //update the neighbours for each tile
        UpdateHexMapNeighbours(hexMap);

        // add the new hexMap to the collection of platforms.
        platforms[platformCounter] = hexMap;
        platformGameObjects[platformCounter] = thisPlatform;
        platformCounter += 1;
    }

    private void UpdateHexMapNeighbours(Dictionary<Vector2Int, HexTile> tileMap)
    {
        foreach (HexTile tile in tileMap.Values)
        {
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

    public Dictionary<Vector2Int, HexTile> GetHexMap(int platformId)
    {        
        if(platformId >= platforms.Count || platformId < 0)
        {
            Debug.Log("GetHexMap is trying to access a platform ID that does not exist!");
            return null;
        } 

        return platforms[platformId];
    }

    public Dictionary<int, Dictionary<Vector2Int, HexTile>> GetAllPlatformHexMaps()
    {
        return platforms;  
    }

    public Dictionary<int, GameObject> GetAllPlatformObjects()
    {
        return platformGameObjects;
    }

    public void RemovePlatform()
    {
        // to do
        // remove from both hexmap dict and gameobj dict
    }
    

    private (List<Vector3>, List<Vector2Int>) GenerateGridPositions()
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

    private Vector3 GetFlatTopHexPosition(int row, int col, float size)
    {
        float x = col * (1.5f * size);
        float z = row * (Mathf.Sqrt(3) * size) + (col % 2) * (Mathf.Sqrt(3) / 2 * size);
        return new Vector3(x, 0, z);
    }

    private Vector3 GetPointedTopHexPosition(int row, int col, float size)
    {
        float x = col * (Mathf.Sqrt(3) * size) + (row % 2) * (Mathf.Sqrt(3) / 2 * size);
        float z = row * (1.5f * size);
        return new Vector3(x, 0, z);
    }

    private (List<Vector3>, List<Vector2Int>) GeneratePinwheelPositions(float radius)
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

    private (List<Vector3>, List<Vector2Int>) GenerateCirclePositions(float radius, float roughness)
    {
        List<Vector3> positions = new List<Vector3>();
        List<Vector2Int> gridCoords = new List<Vector2Int>();
        int maxRings = Mathf.CeilToInt(radius / hexSize) + 1;

        System.Random rand = new System.Random(); // Seeded RNG for consistency

        for (int q = -maxRings; q <= maxRings; q++)
        {
            for (int r = -maxRings; r <= maxRings; r++)
            {
                Vector3 worldPos = AxialToWorld(q, r);
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
    
    private (List<Vector3>, List<Vector2Int>) GenerateHexagonPositions(float radius)
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
                Vector3 pos = AxialToWorld(q, r);
                positions.Add(pos);
                gridCoords.Add(new Vector2Int(q, r));
            }
        }
        return (positions, gridCoords);
    }

    // Convert axial coordinates (q, r) to world-space positions
    private Vector3 AxialToWorld(int q, int r)
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

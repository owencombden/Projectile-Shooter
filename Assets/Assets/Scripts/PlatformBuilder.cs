using UnityEngine;
using System.Collections.Generic;

public class PlatformBuilder : MonoBehaviour
{
    public static PlatformBuilder Instance;

    public enum SpawnMode { Grid, Circle, Pinwheel, Hexagon }
    public int numRows = 5;
    public int numCols = 6;
    public float hexSize = 1f;  // Assumes hex ratio is 2 x Sqrt(3).  2f = twice as big.  0.5f = 1/2 as big.
    public bool isFlatTop = true; // Toggle between flat-top and pointed-top
    public SpawnMode spawnMode = SpawnMode.Grid;
    public float circleRadius = 5f; // Used only for circle mode
    public float edgeRaggedness = 0;  // 0 = perfect cirle, 0.5f pretty ragged, 0.8f very ragged.

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public (List<Vector3>, List<Vector2Int>) GenerateHexTilePositions(Vector3 platformPos)
    {
        List<Vector3> hexPositions = new List<Vector3>();
        List<Vector2Int> hexGridCoords = new List<Vector2Int>();

        if (spawnMode == SpawnMode.Grid)
        {
            (hexPositions, hexGridCoords) = HexUtils.GenerateGridPositions(numRows, numCols, hexSize, isFlatTop);
        }
        else if (spawnMode == SpawnMode.Circle)
        {
            (hexPositions, hexGridCoords) = HexUtils.GenerateCirclePositions(circleRadius, edgeRaggedness, hexSize, isFlatTop);
        }
        else if (spawnMode == SpawnMode.Pinwheel)
        {
            (hexPositions, hexGridCoords) = HexUtils.GeneratePinwheelPositions(circleRadius, hexSize);
        }
        else if (spawnMode == SpawnMode.Hexagon)
        {
            (hexPositions, hexGridCoords) = HexUtils.GenerateHexagonPositions(circleRadius, hexSize, isFlatTop);
        }

        if (hexPositions.Count != hexGridCoords.Count)
        {
            Debug.Log("BuildHexMap error!  Mismatched positions and gridcoords.");
            return (null, null);
        }

        // update the generated world positions relative to this platform position
        for (int i = 0; i < hexPositions.Count; i++) { hexPositions[i] += platformPos; }

        return (hexPositions, hexGridCoords);
    }

    /*
    public (GameObject, Dictionary<Vector2Int, HexTile>) BuildPlatform(Vector3 startPos)
    {
        // get an empty parent object (platform) that will hold all of the floor tiles we're about to spawn
        GameObject thisPlatform = AssetManager.Instance.GetPlatform(startPos, Quaternion.identity);
        thisPlatform.GetComponent<Platform>().platformId = platformIdCounter;

        //build a hexMap for this platform, add it to the collection of platform hexMaps
        Dictionary<Vector2Int, HexTile> hexMap = BuildPlatformHexMap(thisPlatform);

        platformIdCounter++;

        return (thisPlatform, hexMap);
    }

    public Dictionary<Vector2Int, HexTile> BuildPlatformHexMap(GameObject thisPlatform)
    {
        // generate world positions and grid-coords required for the hextile floor
        // spawn hex tiles at position, store the grid-coords, and attach them as children of thisPlatform
        // store the tiles in a hexMap container (a dictionary of grid-coords to hexTileScripts). return the hexMap.
        Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();
        List<Vector3> hexPositions = new List<Vector3>();
        List<Vector2Int> hexGridCoords = new List<Vector2Int>();

        if (spawnMode == SpawnMode.Grid)
        {
            (hexPositions, hexGridCoords) = HexUtils.GenerateGridPositions(numRows, numCols, hexSize, isFlatTop);
        }
        else if (spawnMode == SpawnMode.Circle)
        {
            (hexPositions, hexGridCoords) = HexUtils.GenerateCirclePositions(circleRadius, edgeRaggedness, hexSize, isFlatTop);
        }
        else if (spawnMode == SpawnMode.Pinwheel)
        {
            (hexPositions, hexGridCoords) = HexUtils.GeneratePinwheelPositions(circleRadius, hexSize);
        }
        else if (spawnMode == SpawnMode.Hexagon)
        {
            (hexPositions, hexGridCoords) = HexUtils.GenerateHexagonPositions(circleRadius, hexSize, isFlatTop);
        }

        if (hexPositions.Count != hexGridCoords.Count)
        {
            Debug.Log("BuildHexMap error!  Mismatched positions and gridcoords.");
            return null;
        }

        // update the generated world positions relative to this platform position
        for (int i = 0; i < hexPositions.Count; i++) { hexPositions[i] += thisPlatform.transform.position; }

        //spawn the hex tiles at each position, store coords, and add to hexmap
        for (int i = 0; i < hexPositions.Count; i++)
        {
            // get a hexTile, set the platform as the parent   
            GameObject thisHexTile = AssetManager.Instance.GetHexTile(hexPositions[i], Quaternion.identity);
            thisHexTile.transform.parent = thisPlatform.transform;
            thisHexTile.name = "Hex Tile " + hexGridCoords[i];

            // initialize the hexScript
            // should fix this with an Initialize and callback, similar to PickupManager/Pickup.
            // when tile is destroyed, do a callback here for LevelManager to remove the tile from Dict collections.
            HexTile hexScript = thisHexTile.GetComponent<HexTile>();
            hexScript.ownerId = thisPlatform.GetComponent<Platform>().platformId;
            hexScript.gridCoords = hexGridCoords[i];

            // add this tile (script) to the hexMap
            hexMap[hexGridCoords[i]] = hexScript;
        }

        //update the neighbours for each tile
        HexUtils.UpdateHexMapNeighbours(hexMap);

        return hexMap;
    }
    */

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }    
}

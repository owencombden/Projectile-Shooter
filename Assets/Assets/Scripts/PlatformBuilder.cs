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
            Debug.LogError("PlatformBuilder -> GenerateHexTilePositions Error!  Mismatched positions and gridcoords.");
            return (null, null);
        }

        // update the generated world positions relative to this platform position
        for (int i = 0; i < hexPositions.Count; i++) { hexPositions[i] += platformPos; }

        return (hexPositions, hexGridCoords);
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }    
}

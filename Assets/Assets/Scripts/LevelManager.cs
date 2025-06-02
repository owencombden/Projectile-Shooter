using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("Game Setup")]
    [SerializeField] private int maxTotalCharacters = 4; // total including players + AI
    [SerializeField] private int extraAICount = 2;

    [Header("Platform Settings")]
    [SerializeField] private float xSpacing = 60f;
    [SerializeField] private float zSpacing = 60f;

    // store all platforms in a dictionary of nested dictionaries
    // outer dictionary stores all the platforms, indexable by platform ID
    // inner dictionary stores all of the hexTiles for that platform, indexable by grid-coord    
    // platforms[platformID]                 -> get all tiles for that platform
    // platforms[platformID][gridPos]        -> get any tile in O(1)
    // platforms[platformID].Remove(gridPos) -> remove tile from dictionary    
    private Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms = new();  // 2D hexmaps
    private Dictionary<int, GameObject> platformGameObjects = new();             // gameobjs  

    // could also implement this HashSet if things get slow
    // an inner hashset is faster, could be good if wanted to apply something across all tiles (ie: collision detection?)
    // would have to keep the Dict(Dict) and maintain two collections when adding/removing tiles and platforms
    // private Dictionary<int, HashSet<HexTile>> activeTiles = new();

    private Dictionary<int, PlayerController> playerControllers = new();
    private Dictionary<int, AIController> aiControllers = new();
    public bool AreAllAIsDefeated() => aiControllers.Count == 0;
    public bool IsPlayerDefeated() => playerControllers.Count == 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    public void InitializeLevel()
    {
        Debug.Log("Level Manager is Initalializing the level");
        SpawnAllPlatforms();
        SpawnAllCharacters(platforms);
    }

    private void SpawnAllPlatforms()
    {

        int humanCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        int totalCharacters = Mathf.Max(humanCount + extraAICount, maxTotalCharacters);

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCharacters));
        int spawned = 0;

        for (int row = 0; row < gridSize && spawned < totalCharacters; row++)
        {
            for (int col = 0; col < gridSize && spawned < totalCharacters; col++)
            {
                Vector3 spawnPos = new Vector3(col * xSpacing, 0, row * zSpacing);

                // get a platform with a hexMap
                GameObject thisPlatform;
                Dictionary<Vector2Int, HexTile> hexMap;
                (thisPlatform, hexMap) = PlatformBuilder.Instance.BuildPlatform(spawnPos);

                // cache the platform gameobject, as well as the hexmap of tiles
                int platformId = thisPlatform.GetComponent<Platform>().platformId;
                platformGameObjects[platformId] = thisPlatform;
                platforms[platformId] = hexMap;

                spawned++;
            }
        }

    }

    public void SpawnAllCharacters(Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms)
    {
        // Compute the center of all platforms (characters will face this center when spawned)
        Vector3 centerPoint = Vector3.zero;
        foreach (var platformGO in platformGameObjects.Values)
        {
            centerPoint += platformGO.transform.position;
        }
        centerPoint /= platformGameObjects.Count;

        int aiNeeded = extraAICount;

        // Assign characters to platforms
        List<int> platformIndices = new List<int>(platforms.Keys);
        int currentPlatformIndex = 0;

        // HUMAN PLAYERS (Network-spawned already, just reposition them)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            GameObject playerObj = client.PlayerObject.gameObject;

            //var spawnTile = HexUtils.GetRandomHexTile(platforms[platformIndices[currentPlatformIndex]]);
            //Vector3 spawnPos = spawnTile.transform.position;

            Vector3 spawnPos = GetPlatformCenter(platforms[platformIndices[currentPlatformIndex]]);
            spawnPos.y = 2.33f;

            playerObj.transform.position = spawnPos;

            // Face center of the grid
            Vector3 directionToCenter = (centerPoint - spawnPos).normalized;
            directionToCenter.y = 0; // flatten on Y axis
            if (directionToCenter != Vector3.zero)
            {
                playerObj.transform.rotation = Quaternion.LookRotation(directionToCenter);
            }

            CharacterMotor playerMotorScript = playerObj.GetComponent<CharacterMotor>();
            playerMotorScript.isPlayer = true;

            var playerController = playerObj.GetComponent<PlayerController>();
            playerController.Set_ID(platformIndices[currentPlatformIndex]);
            playerController.SetPlayerHexMap(platforms[platformIndices[currentPlatformIndex]]);

            // this registration is local (in LevelManager), NOT Network client/player registration
            RegisterPlayer(platformIndices[currentPlatformIndex], playerController);

            // Set up the camera for the local player
            if (playerController.IsOwner)
            {
                Camera.main.GetComponent<CameraLook>().SetTarget(playerObj.transform, playerObj.transform.Find("CameraLookHere"));
            }

            currentPlatformIndex++;
        }

        // AI PLAYERS
        for (int i = 0; i < aiNeeded; i++)
        {
            // only the Server should spawn/handle AI
            if (!NetworkManager.Singleton.IsServer) return;

            int platformIndex = platformIndices[currentPlatformIndex];
            //Vector3 aiSpawnPos = HexUtils.GetRandomHexTile(platforms[platformIndex]).transform.position;

            Vector3 aiSpawnPos = GetPlatformCenter(platforms[platformIndex]);
            aiSpawnPos.y = 2.77f;

            // Face center of the grid
            Vector3 directionToCenter = (centerPoint - aiSpawnPos).normalized;
            directionToCenter.y = 0; // flatten on Y axis
            Quaternion aiRotation = Quaternion.identity;
            if (directionToCenter != Vector3.zero)
            {
                aiRotation = Quaternion.LookRotation(directionToCenter);
            }

            GameObject ai = AssetManager.Instance.GetAI(aiSpawnPos, aiRotation);

            // Ensure the AI prefab has a NetworkObject component
            NetworkObject networkObject = ai.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }

            var aiController = ai.GetComponent<AIController>();
            aiController.SetAIHexMap(platforms[platformIndex]);
            int aiID = 1000 + platformIndex;
            aiController.Set_ID(aiID);

            // this registration is local (in LevelManager), NOT Network client/player registration
            RegisterAI(aiID, aiController);

            currentPlatformIndex++;
        }

        GameManager.Instance.TransitionToGameplay();
    }

    public void RegisterPlayer(int player_ID, PlayerController controllerScript)
    {
        playerControllers[player_ID] = controllerScript;
    }

    public void RegisterAI(int ai_ID, AIController controllerScript)
    {
        aiControllers[ai_ID] = controllerScript;
    }

    public Dictionary<Vector2Int, HexTile> GetHexMap(int platformId)
    {
        if (platformId >= platforms.Count || platformId < 0) { return null; }

        return platforms[platformId];
    }

    public Dictionary<int, Dictionary<Vector2Int, HexTile>> GetAllHexMaps()
    {
        return platforms;
    }

    public Dictionary<int, GameObject> GetAllPlatformObjects()
    {
        return platformGameObjects;
    }

    public Vector3 GetPlatformCenter(Dictionary<Vector2Int, HexTile> hexMap)
    {
        Vector2Int centerCoords = new Vector2Int(0, 0);
        foreach (var tile in hexMap)
        {
            if (tile.Key == centerCoords)
                return tile.Value.transform.position;
        }

        Debug.LogWarning($"Center tile (0,0) was not found. Falling back to average.");

        // Optional fallback to average center
        var platformTiles = hexMap.Values.ToList();
        if (platformTiles.Count == 0) return Vector3.zero;

        Vector3 avg = Vector3.zero;
        foreach (var tile in platformTiles)
            avg += tile.transform.position;

        return avg / platformTiles.Count;
    }

    public void RemoveCharacter(int id, bool isPlayer)
    {
        if (isPlayer)
        {
            playerControllers.Remove(id);
        }
        else
        {
            aiControllers.Remove(id);
        }
    }

    public void RemoveHexTile(int platformID, Vector2Int gridPos)
    {
        // get the script on this tile
        HexTile thisHexTile = platforms[platformID][gridPos];

        // get the neighbours of the tile that will be removed (they will need their 'neighbours' updated after removal)
        List<HexTile> neighbours = HexUtils.GetHexTileNeighbours(thisHexTile, platforms[platformID]);

        // reset the tile being destroyed
        thisHexTile.startPos = Vector3.zero;
        thisHexTile.gridCoords = Vector2Int.zero;
        thisHexTile.neighbors.Clear();

        // remove it from the LevelManager collection
        platforms[platformID].Remove(gridPos);

        // update the neighbours of the removed tile
        foreach (HexTile neighbour in neighbours)
        {
            HexUtils.UpdateHexTileNeighbours(neighbour, platforms[platformID]);
        }
    }

    public void CleanUpBeforeRestart()
    {
        // player cleanup
        foreach (var player in playerControllers.Values)
        {
            if (player != null) Destroy(player.gameObject);
        }
        playerControllers.Clear();

        // AI cleanup
        foreach (var ai in aiControllers.Values)
        {
            if (ai != null) Destroy(ai.gameObject);
        }
        aiControllers.Clear();

        // Platform and tile cleanup
        foreach (var platformID in new List<int>(platforms.Keys))
        {
            RemovePlatform(platformID);
        }

        platforms.Clear();
        platformGameObjects.Clear();
    }

    private void RemovePlatform(int platformID)
    {
        // remove platforms from both hexmap dict and gameobj dict.  make sure they are empty.
        if (!platforms.ContainsKey(platformID)) return;
        if (!platformGameObjects.ContainsKey(platformID)) return;

        // Destroy all tiles under this platform
        foreach (var tile in platforms[platformID].Values)
        {
            if (tile != null) Destroy(tile.gameObject);
        }

        // Destroy the platform object
        if (platformGameObjects.TryGetValue(platformID, out GameObject platformObj))
        {
            Destroy(platformObj);
        }

        // Remove from dictionaries
        platforms.Remove(platformID);
        platformGameObjects.Remove(platformID);
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

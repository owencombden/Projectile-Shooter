using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;

public class LevelManager : NetworkBehaviour
{
    public static LevelManager Instance;

    [Header("Game Setup")]
    [SerializeField] private int maxTotalCharacters = 0; // SET IN INSPECTOR.  Total includes players + AI
    [SerializeField] private int extraAICount = 0;       // SET IN INSPECTOR.

    [Header("Platform Settings")]
    [SerializeField] private float xSpacing = 60f;
    [SerializeField] private float zSpacing = 60f;

    // store all platforms in a dictionary of nested dictionaries
    // outer dictionary stores all the platforms, indexable by platform ID
    // inner dictionary stores all of the hexTiles for that platform, indexable by grid-coord    
    // platforms[platformID]                 -> get all tiles for that platform
    // platforms[platformID][gridPos]        -> get any tile in O(1)
    // platforms[platformID].Remove(gridPos) -> remove tile from dictionary    
    private Dictionary<ulong, Dictionary<Vector2Int, HexTile>> platforms = new();  // 2D hexmaps
    private Dictionary<ulong, GameObject> platformGameObjects = new();             // gameobjs  

    // could also implement this HashSet if things get slow
    // an inner hashset is faster, could be good if wanted to apply something across all tiles (ie: collision detection?)
    // would have to keep the Dict(Dict) and maintain two collections when adding/removing tiles and platforms
    // private Dictionary<int, HashSet<HexTile>> activeTiles = new();    

    private Dictionary<ulong, PlayerController> playerControllers = new();
    private Dictionary<ulong, AIController> aiControllers = new();

    // tracking the initial synch of hextiles data across the network.
    [SerializeField] private List<HexTileData> allHexTileData = new();

    // track the clients    
    private int expectedClientCount = -1;
    ulong[] characterIds = Enumerable.Repeat(ulong.MaxValue, 15).ToArray();  // careful, max 15 ids! initialize each position with a MaxValue placeholder

    // ClientRPC callback tracking
    private HashSet<ulong> clientsConfirmedHexData = new();
    private HashSet<ulong> clientsConfirmedPlatformsSpawned = new();

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
        // server/host only here!

        //Debug.Log("Level Manager is Initalializing the level");

        // get the number of expected clients (excluding the host) that we have to communicate with
        expectedClientCount = NetworkManager.Singleton.ConnectedClientsList.Count - 1;

        int idCounter = 0;
        // get the human ids
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
        {
            characterIds[i] = NetworkManager.Singleton.ConnectedClientsList[i].ClientId;
            //Debug.Log($"Created human player with id: {characterIds[i]}");
            idCounter++;
        }
        // generate and append any AI ids
        for (int i = 0; i < extraAICount; i++)
        {
            ulong aiID = 100 + (ulong)idCounter;
            //Debug.Log($"Created AI player with id: {aiID}");
            characterIds[idCounter] = aiID;
            idCounter++;
        }

        

        // generate the datastructure that stores all the information needed to spawn all the floor tiles
        GenerateAllHexTileData();

        // share the hexTile data with all clients.  The clients report back when they all have synched.
        // convert the argument to an array (as its natively serialized by Unity) for transport to clients
        if (NetworkManager.Singleton.ConnectedClientsList.Count > 1)
        {
            SendHexTileDataClientRpc(allHexTileData.ToArray());
        }
        else
        {
            // only one human player
            SpawnAllPlatforms();
            SpawnAllCharacters();
            GameManager.Instance.TransitionToGameplay();
        }
        
    }

    private void GenerateAllHexTileData()
    {
        int humanCount = NetworkManager.Singleton.ConnectedClientsList.Count;
        int totalCharacters = Mathf.Max(humanCount + extraAICount, maxTotalCharacters);

        // put this back for num platforms based on num total characters
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCharacters));

        int platformsSpawned = 0;

        for (int row = 0; row < gridSize && platformsSpawned < totalCharacters; row++)
        {
            for (int col = 0; col < gridSize && platformsSpawned < totalCharacters; col++)
            {
                // get the platform position
                Vector3 platformPos = new Vector3(col * xSpacing, 0, row * zSpacing);

                // get the worldpos and grid coords for each hextile attached to this platform
                List<Vector3> hexPositions;
                List<Vector2Int> hexGridCoords;
                (hexPositions, hexGridCoords) = PlatformBuilder.Instance.GenerateHexTilePositions(platformPos);

                // create the lightweight datastructure for sharing across the networks
                // this will be used client-side to create/update the hextiles
                List<HexTileData> hexTileDataList = new();
                for (int i = 0; i < hexPositions.Count; i++)
                {
                    HexTileData data = new HexTileData
                    {
                        platformId = characterIds[platformsSpawned],  // platform gets ID from character
                        worldPos = hexPositions[i],
                        gridCoords = hexGridCoords[i]
                    };

                    hexTileDataList.Add(data);
                }
                allHexTileData.AddRange(hexTileDataList);

                platformsSpawned++;
            }
        }

        //Debug.Log($"Finished generating hextiles.  There are {platformsSpawned} platforms available");
    }    
    
    [ClientRpc]
    private void SendHexTileDataClientRpc(HexTileData[] tileDataList)
    {
        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is calling SendHexTileDataClientRpc");        
        // clients only. the host already has this data.
        if (NetworkManager.Singleton.IsHost) return; 
        
        allHexTileData = tileDataList.ToList();  // convert the serializable array back to a list, which is updated on client side 

        // Tell host we received it
        ConfirmHexTileDataReceivedServerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmHexTileDataReceivedServerRpc(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        clientsConfirmedHexData.Add(clientId);
        //Debug.Log($"[HexSync] Client {clientId} confirmed tile data receipt ({clientsConfirmedHexData.Count}/{expectedClientCount})");

        if (clientsConfirmedHexData.Count >= expectedClientCount)
        {
            //Debug.Log("[HexSync] All clients are ready to spawn platforms!");
            ReadyToSpawnPlatformsClientRpc();
        }
    }

    [ClientRpc]
    private void ReadyToSpawnPlatformsClientRpc()
    {
        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is calling ReadyToSpawnPlatformsClientRpc");

        // everybody spawns, host too.
        SpawnAllPlatforms();        

        // each client should now report that they have finished spawning the floor hextiles.  
        // when all are done, host can proceed with spawning characters.
        ConfirmDoneSpawningPlatformsServerRpc(NetworkManager.Singleton.LocalClientId);
    }       

    private void SpawnAllPlatforms()
    {
        // the host has shared allHexTileData among all the clients
        // clients will now use this list to instantiate platforms and platformGameObjects on the client-side

        // Step 1: Group the hex tile data by platformId
        var groupedByPlatform = allHexTileData
            .GroupBy(data => data.platformId);

        foreach (var platformGroup in groupedByPlatform)
        {
            ulong platformId = platformGroup.Key;

            // make the position of the platform equal to the position of the first child hextile
            Vector3 platformStartPos = platformGroup.First().worldPos;

            //Debug.Log($"Creating platform {platformId} at position {platformStartPos}");

            // Step 2: Instantiate the platform root object, set it's Id, and cache it
            GameObject thisPlatform = AssetManager.Instance.GetPlatform(platformStartPos, Quaternion.identity);
            thisPlatform.name = $"Platform_{platformId}";
            thisPlatform.GetComponent<Platform>().platformId = platformId;
            platformGameObjects[platformId] = thisPlatform;

            // build a hexMap for this platform (gridcoords to HexTile scripts)
            Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();

            // Step 3: For each HexTileData in this group, spawn a hex tile
            foreach (var tileData in platformGroup)
            {
                GameObject thisHexTile = AssetManager.Instance.GetHexTile(tileData.worldPos, Quaternion.identity);
                thisHexTile.transform.SetParent(thisPlatform.transform, worldPositionStays: true);
                thisHexTile.name = $"HexTile_{tileData.gridCoords}";

                // initialize the hexScript
                // should fix this with an Initialize and callback, similar to PickupManager/Pickup.
                // when tile is destroyed, do a callback here for LevelManager to remove the tile from Dict collections.
                HexTile hexScript = thisHexTile.GetComponent<HexTile>();
                hexScript.ownerId = thisPlatform.GetComponent<Platform>().platformId;
                hexScript.gridCoords = tileData.gridCoords;

                // add this tile (script) to the hexMap
                hexMap[tileData.gridCoords] = hexScript;
            }

            //update the neighbours for each tile
            HexUtils.UpdateHexMapNeighbours(hexMap);

            // add the configured hexMap to the collection
            platforms[platformId] = hexMap;            
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmDoneSpawningPlatformsServerRpc(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        // track the clientId of the client that called this on the server
        clientsConfirmedPlatformsSpawned.Add(clientId);

        // the host/server has also spawned platforms, track its id
        if (!clientsConfirmedPlatformsSpawned.Contains(NetworkManager.Singleton.LocalClientId))
        {
            clientsConfirmedPlatformsSpawned.Add(NetworkManager.Singleton.LocalClientId);
        }

        //Debug.Log($"[PlatformSync] Client {clientId} confirmed finished spawning platforms.  receipt ({clientsConfirmedPlatformsSpawned.Count}/{expectedClientCount + 1})");  //include host

        if (clientsConfirmedPlatformsSpawned.Count >= expectedClientCount + 1) // include host
        {
            //Debug.Log($"[PlatformSync] Client {NetworkManager.Singleton.LocalClientId} reporting that the server is ready to spawn characters!");
            ReadyToSpawnCharactersServerRpc();
        }
    }

    [ServerRpc]
    private void ReadyToSpawnCharactersServerRpc()
    {
        // server will spawn and own characters
        if (!NetworkManager.Singleton.IsServer) return;

        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is calling ReadyToSpawnCharactersClientRpc");

        // spawn human and ai characters (position, rotation, config)
        SpawnAllCharacters();
        
        // finished with setup, start the game
        GameManager.Instance.TransitionToGameplay();
    }

    void SpawnAllCharacters()
    {
        // only server can spawn
        if (!IsServer) { return; }

        //Debug.Log($"Spawning characters.  There are {platformGameObjects.Count} platforms available.");

        // compute the center of all platforms (characters will face this center when spawned)
        Vector3 centerPoint = Vector3.zero;
        foreach (var platformGO in platformGameObjects.Values)
        {
            centerPoint += platformGO.transform.position;
        }
        centerPoint /= platformGameObjects.Count;

        // assign characters to platforms
        List<ulong> platformIndices = new List<ulong>(platforms.Keys);
        ulong currentPlatformIndex = 0;

        // HUMAN PLAYERS (spawn on the network and reposition/configure)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            // position (center of platform)        
            Vector3 spawnPos = GetPlatformCenter(platforms[platformIndices[(int)currentPlatformIndex]]);
            spawnPos.y = 2.33f;

            // rotation (face center of the platform grid)
            Vector3 directionToCenter = (centerPoint - spawnPos).normalized;
            directionToCenter.y = 0; // flatten on Y axis
            Quaternion spawnRot = Quaternion.LookRotation(directionToCenter);

            // spawn player
            GameObject playerObj = AssetManager.Instance.GetPlayer(spawnPos, spawnRot);
            playerObj.GetComponent<NetworkObject>().SpawnAsPlayerObject(client.ClientId);

            // configure player
            CharacterMotor playerMotorScript = playerObj.GetComponent<CharacterMotor>();
            playerMotorScript.isPlayer = true;
            var playerController = playerObj.GetComponent<PlayerController>();
            playerController.id.Value = client.ClientId;
            //Debug.Log($"Setting human player controller as ID: {playerController.id}");
            playerController.SetPlayerHexMap(platforms[platformIndices[(int)currentPlatformIndex]]);
            // set starting ammo
            CharacterShooter playerShooterScript = playerObj.GetComponent<CharacterShooter>();
            playerShooterScript.currentAmmo.Value = playerShooterScript.maxAmmo;
            //Debug.Log($"Player {playerController.id.Value} is starting with {playerShooterScript.currentAmmo.Value} ammo.");

            // register player in LevelManager
            RegisterPlayer(platformIndices[(int)currentPlatformIndex], playerController);

            currentPlatformIndex++;
        }

        // AI PLAYERS
        for (int i = 0; i < extraAICount; i++)
        {
            
            // position
            ulong platformIndex = platformIndices[(int)currentPlatformIndex];
            Vector3 aiSpawnPos = GetPlatformCenter(platforms[platformIndex]);
            aiSpawnPos.y = 2.77f;

            // rotation (face center of the platform grid)
            Vector3 directionToCenter = (centerPoint - aiSpawnPos).normalized;
            directionToCenter.y = 0; // flatten on Y axis
            Quaternion aiRotation = Quaternion.LookRotation(directionToCenter);

            // spawn the ai on the network
            GameObject ai = AssetManager.Instance.GetAI(aiSpawnPos, aiRotation);
            ai.GetComponent<NetworkObject>().Spawn(true); // Server owns it            

            // configure
            var aiController = ai.GetComponent<AIController>();
            aiController.SetAIHexMap(platforms[platformIndex]);
            ulong aiID = characterIds[(int)currentPlatformIndex];
            aiController.id.Value = aiID;
            // set starting ammo
            CharacterShooter aiShooterScript = ai.GetComponent<CharacterShooter>();
            aiShooterScript.currentAmmo.Value = aiShooterScript.maxAmmo;
            //Debug.Log($"AI {aiController.id.Value} is starting with {aiShooterScript.currentAmmo.Value} ammo.");

            // register with LevelManager
            RegisterAI(aiID, aiController);

            currentPlatformIndex++;
            
        }

        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is finished spawning the characters");
    }  
    
    public void RegisterPlayer(ulong player_ID, PlayerController controllerScript)
    {
        playerControllers[player_ID] = controllerScript;
    }

    public void RegisterAI(ulong ai_ID, AIController controllerScript)
    {
        aiControllers[ai_ID] = controllerScript;
    }
   
    public Dictionary<Vector2Int, HexTile> GetHexMap(ulong platformId)
    {
        if (platformId < 0) { return null; }

        return platforms[platformId];
    }

    public Dictionary<ulong, Dictionary<Vector2Int, HexTile>> GetAllHexMaps()
    {
        return platforms;
    }

    public Dictionary<ulong, GameObject> GetAllPlatformObjects()
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

    public void RemoveCharacter(ulong removingId, bool isPlayer)
    {
        // server only
        if (!IsServer) return;

        if (isPlayer)
        {
            playerControllers.Remove(removingId);
        }
        else
        {
            aiControllers.Remove(removingId);
        }

        CheckForGameOver();
    }

    public void RemoveHexTile(ulong platformID, Vector2Int gridPos)
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

    [ClientRpc]
    public void ApplyBlastDamageClientRpc(ulong platformID, Vector2Int originGridPos, float baseDamage, int blastRadius)
    {
        // Skip for host — already applied on server side
        if (IsHost) return;

        // This assumes you can find the tile and your map is in sync
        HexTile originTile = platforms[platformID][originGridPos];
        if (originTile != null)
        {
            originTile.ApplyBlastDamage(baseDamage, blastRadius);
        }
    }

    private void CheckForGameOver()
    {
        // server only
        if (!IsServer) return;

        if (playerControllers.Count() == 1 && aiControllers.Count() <= 0)
        {            
            var remainingPlayerController = playerControllers.First().Value;
            ulong remainingPlayerId = remainingPlayerController.id.Value;
            Debug.Log($"Game Over!  Player {remainingPlayerId} Won !!");
            GameManager.Instance.TransitionToGameOver(remainingPlayerId);
        }
        else if (aiControllers.Count() == 1 && playerControllers.Count() <= 0)
        {
            var remainingAIController = aiControllers.First().Value;
            ulong remainingAIId = remainingAIController.id.Value;
            Debug.Log($"Game Over! AI {remainingAIId} Won !!");
            GameManager.Instance.TransitionToGameOver(remainingAIId);
        }
    }

    public void CleanUpBeforeRestart()
    {
        // player cleanup
        foreach (var player in playerControllers.Values)
        {
            Debug.Log($"Destroying Player {player.id.Value}");
            if (player != null) Destroy(player.gameObject);
        }
        playerControllers.Clear();

        // AI cleanup
        foreach (var ai in aiControllers.Values)
        {
            if (ai != null) Destroy(ai.gameObject);
        }
        aiControllers.Clear();

        // Pickups cleanup
        var allPlatformObjects = LevelManager.Instance.GetAllPlatformObjects();
        foreach (var kvp in allPlatformObjects)
        {
            GameObject platformGO = kvp.Value;
            ulong platformId = platformGO.GetComponent<Platform>().platformId;
            PlatformPickupData platformData = platformGO.GetComponent<PlatformPickupData>();
            foreach (var pickup in platformData.activePickups)
            {
                var netObj = pickup.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
            }
            platformData.activePickups.Clear();
        }

        // Platform and tile cleanup (client-owned, so clean up locally)
        RemovePlatformClientRpc();        
    }

    [ClientRpc]
    private void RemovePlatformClientRpc()
    {
        foreach (var platformID in new List<ulong>(platforms.Keys))
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
        platforms.Clear();
        platformGameObjects.Clear();        
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private new void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

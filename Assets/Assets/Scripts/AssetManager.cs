using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class AssetManager : MonoBehaviour
{
    [SerializeField]private Camera     mainCamera;
    [SerializeField]private GameObject player;
    [SerializeField]private GameObject enemy;
    [SerializeField]private GameObject iceSheet;
    [SerializeField]private GameObject squareTile;
    [SerializeField]private GameObject platform;
    [SerializeField]private GameObject hexTile;
    [SerializeField]private GameObject playerBullet;
    [SerializeField]private GameObject enemyBullet;
    [SerializeField]private GameObject rangeMarker;
    [SerializeField]private GameObject treasure;
    [SerializeField]private GameObject ammoSpawnPrefab;

    private Transform playerGround; 
        
    private LevelManager     levelManager;
    private List<GameObject> allIceSheets     = new List<GameObject>();
    private List<GameObject> allEnemies       = new List<GameObject>();
    private List<GameObject> allPlayerBullets = new List<GameObject>();
    private List<GameObject> allEnemyBullets  = new List<GameObject>();
    private List<GameObject> allTreasures     = new List<GameObject>();
    private List<GameObject> allPlayerAmmos   = new List<GameObject>();

    private System.Random random = new System.Random();
    
    Vector2Int[] neighborOffsets = {new Vector2Int(+1, 0), new Vector2Int(+1, -1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, +1), new Vector2Int(0, +1)};

    // store all platforms in a dictionary of dictionaries
    // outer dictionary stores all the platforms, indexable by platform ID
    // inner dictionary stores all of the hexTiles for that platform, indexable by grid-coord    
    // platforms[platformID]                 -> get all tiles for that platform
    // platforms[platformID][gridPos]        -> get any tile in O(1)
    // platforms[platformID].Remove(gridPos) -> remove tile from dictionary
    private int platformCounter = 0;
    private Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms = new();
    
    // could also implement this HashSet if things get slow
    // an inner hashset is faster, could be good if wanted to apply something across all tiles (ie: collision detection?)
    // would have to keep the Dict(Dict) and maintain two collections when adding/removing tiles and platforms
    // private Dictionary<int, HashSet<HexTile>> activeTiles = new();
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();                
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject GetAmmoSpawnPrefab(Vector3 spawnPos)
    {
        Quaternion rot = ammoSpawnPrefab.transform.rotation;
        GameObject thisAmmo = GameObject.Instantiate(ammoSpawnPrefab, spawnPos, rot);

        AmmoSpawnPrefab ammoScript = thisAmmo.GetComponent<AmmoSpawnPrefab>();
        
        //set the types of ammo available in this spawn package (this should be dictated by the Game/Level/Pickup manager when ready)
        List<string> types = new List<string> { "normal", "normal", "normal", "normal" };
        ammoScript.SetAmmoTypes(types);

        allPlayerAmmos.Add(thisAmmo);
        return thisAmmo;
    }    
    
    public GameObject GetEnemy()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = enemy.transform.rotation;
        //GameObject ice = GameObject.Instantiate(iceSheet, pos, rot, parent);
        GameObject e = GameObject.Instantiate(enemy, pos, rot);
        return e;
    }

    public GameObject GetEnemyBullet(Vector3 spawnPos)
    {
        Vector3 pos = spawnPos;
        Quaternion rot = enemyBullet.transform.rotation;
        GameObject thisBullet = GameObject.Instantiate(enemyBullet, pos, rot);
        thisBullet.GetComponent<Bullet>().SetStartPos(pos);
        allEnemyBullets.Add(thisBullet);
        return thisBullet;
    }

    public void BuildHexMap(List<Vector3> positions, List<Vector2Int> gridCoords)
    {
        if (positions.Count != gridCoords.Count)
        {
            Debug.Log("BuildHexMap error!  Mismatched positions and gridcoords.");
            return;
        }

        // get a parent object (platform) that will hold all of the tiles we're about to spawn
        GameObject thisPlatform = Instantiate(platform, Vector3.zero, Quaternion.identity, transform);

        // build one hex platform (a dictionary of grid-coords to hexTileScripts) and add it to the platforms (outer) dictionary with an ID.
        Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();

        //spawn the hex tiles at each position, store coords, and add to dictionary
        for (int i=0; i <positions.Count; i++)
        {
            GameObject thisHexTile = Instantiate(hexTile, positions[i], Quaternion.identity, thisPlatform.transform);
            HexTile hexScript = thisHexTile.GetComponent<HexTile>();
            hexScript.gridCoords = gridCoords[i];
            hexMap[gridCoords[i]] = hexScript;
        }

        //update the neighbours for each tile
        UpdateHexMapNeighbours(hexMap);

        // add the new hexMap to the collection of platforms.
        platforms[platformCounter] = hexMap;
        platformCounter += 1;        

    }

    public void UpdateHexMapNeighbours(Dictionary<Vector2Int, HexTile> tileMap)
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

    public HexTile GetRandomPlayerHexScript()
    {
        if (platforms.Count == 0) return null; // Prevent errors if the dictionary is empty

        int randomPlatformIndex = random.Next(platforms.Count); 
        int randomTileIndex = random.Next(platforms[randomPlatformIndex].Count);
        return platforms[randomPlatformIndex].Values.ElementAt(randomTileIndex); // Fetch the random script        
    }

    public GameObject GetIceSheet()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = iceSheet.transform.rotation;
        //GameObject ice = GameObject.Instantiate(iceSheet, pos, rot, parent);
        GameObject ice = GameObject.Instantiate(iceSheet, pos, rot);
        allIceSheets.Add(ice);
        return ice;
    }

    public Camera GetMainCamera()
    {
        return mainCamera;
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public GameObject GetPlayerBullet(Vector3 spawnPos, string bulletType)
    {
        if (bulletType == "normal")
        {
            Vector3 pos = spawnPos;
            Quaternion rot = playerBullet.transform.rotation;
            GameObject thisBullet = GameObject.Instantiate(playerBullet, pos, rot);
            allPlayerBullets.Add(thisBullet);
            return thisBullet;
        }
        else
        {
            Debug.Log("Bullet Type -- " + bulletType + " -- not found!!");
            return null;
        }
        
    }

    public GameObject GetRangeMarker()
    {
        transform.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
        GameObject thisRangeMarker = GameObject.Instantiate(rangeMarker, pos, rot);
        return thisRangeMarker;
    }

    public GameObject GetSquareTile()
    {
        return squareTile;
    }
    
    public GameObject GetTreasure(Vector3 spawnPos)
    {
        Vector3 pos = spawnPos;
        Quaternion rot = treasure.transform.rotation;
        GameObject thisTreasure = GameObject.Instantiate(treasure, pos, rot);
        allTreasures.Add(thisTreasure);
        return thisTreasure;
    }

    public void RemoveGameObject(GameObject taggedObject, float delay)
    {
        // set this up to use a pooling system

        if (taggedObject.tag == "Ammo")
        {
            //remove current ammo prefab
            GameObject.Destroy(taggedObject, delay);
        }
    }

    

}

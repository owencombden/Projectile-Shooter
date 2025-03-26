using System.Collections.Generic;
using UnityEngine;

public class AssetManager : MonoBehaviour
{
    [SerializeField]private Camera mainCamera;
    [SerializeField]private GameObject player;
    [SerializeField]private GameObject enemy;
    [SerializeField]private GameObject iceSheet;
    [SerializeField]private GameObject squareTile;
    [SerializeField]private GameObject hexTile;
    [SerializeField]private GameObject playerBullet;
    [SerializeField]private GameObject enemyBullet;
    [SerializeField]private GameObject rangeMarker;
    [SerializeField]private GameObject treasure;
    [SerializeField]private GameObject ammoSpawnPrefab;

    private Transform playerGround; 
        
    private LevelManager     levelManager;
    private List<GameObject> allIceSheets     = new List<GameObject>();
    private List<GameObject> allHexTiles      = new List<GameObject>();
    private List<GameObject> allEnemies       = new List<GameObject>();
    private List<GameObject> allPlayerBullets = new List<GameObject>();
    private List<GameObject> allEnemyBullets  = new List<GameObject>();
    private List<GameObject> allTreasures     = new List<GameObject>();
    private List<GameObject> allPlayerAmmos   = new List<GameObject>();

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
        Vector3 pos = spawnPos;
        Quaternion rot = ammoSpawnPrefab.transform.rotation;
        GameObject thisAmmo = GameObject.Instantiate(ammoSpawnPrefab, pos, rot);
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

    public void GetHexTiles(List<Vector3> positions)
    {
        //spawn the hex tiles at each position
        foreach (Vector3 pos in positions)
        {
            GameObject thisHexTile = Instantiate(hexTile, pos, Quaternion.identity, transform);
            allHexTiles.Add(thisHexTile);
        }
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

    public void RemoveGameObject(GameObject taggedObject)
    {
        // set this up to use a pooling system

        if (taggedObject.tag == "Ammo")
        {
            //remove current ammo prefab, call for a new one to be spawned in a random short time interval
            GameObject.Destroy(taggedObject, 0.1f);
            SpawnNewPlayerAmmo();
        }
    }

    public void SpawnNewPlayerAmmo()
    {
        //fix this!   set the playerGround someother way.
        if (playerGround == null)
        {
            // using this to help spawn ammo.  
            Vector3 rayStart = player.transform.position;
            Vector3 rayDir = transform.up * -1;
            Ray ray = new Ray(rayStart, rayDir);        
            float rayLength = 10f;
            RaycastHit hitData;
            if (Physics.Raycast(ray, out hitData, rayLength))
            {            
                string tag = hitData.collider.tag;
                if(tag == "Ground")
                {
                    //we hit a tile, get the parent container
                    playerGround = hitData.transform.parent;
                    Debug.Log("Level found " + playerGround.name + " as player ground container.");
                }
            }
            else { Debug.Log(gameObject.name + " could not find the player ground!"); }

        }

        Vector3 spawnPos = playerGround.GetChild(Random.Range(0, playerGround.childCount)).position;
        spawnPos.y += 1;
        GetAmmoSpawnPrefab(spawnPos);
    }

}

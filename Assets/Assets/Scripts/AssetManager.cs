using UnityEngine;
using System.Collections.Generic;

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
        return enemy;
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

    public GameObject GetHexTile()
    {
        return hexTile;
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

    public GameObject GetPlatform()
    {
        return platform;
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

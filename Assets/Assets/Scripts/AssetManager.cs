using System.Collections.Generic;
using UnityEngine;

public class AssetManager : MonoBehaviour
{
    [SerializeField]private Camera mainCamera;
    [SerializeField]private GameObject levelPrefab;
    [SerializeField]private GameObject player;
    [SerializeField]private GameObject enemy;
    [SerializeField]private GameObject iceSheet;
    [SerializeField]private GameObject tile;
    [SerializeField]private GameObject playerBullet;
    [SerializeField]private GameObject enemyBullet;
    [SerializeField]private GameObject rangeMarker;
    [SerializeField]private GameObject treasure;
    [SerializeField]private GameObject ammoSpawnPrefab;

    
    private GameObject currentLevel;
    private Level currentLevelScript;
    private List<GameObject> allIceSheets = new List<GameObject>();
    private List<GameObject> allEnemies   = new List<GameObject>();
    private List<GameObject> allPlayerBullets = new List<GameObject>();
    private List<GameObject> allEnemyBullets = new List<GameObject>();
    private List<GameObject> allTreasures = new List<GameObject>();
    private List<GameObject> allPlayerAmmos = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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

    public GameObject GetTile()
    {
        return tile;
    }
    
    public GameObject GetTreasure(Vector3 spawnPos)
    {
        Vector3 pos = spawnPos;
        Quaternion rot = treasure.transform.rotation;
        GameObject thisTreasure = GameObject.Instantiate(treasure, pos, rot);
        allTreasures.Add(thisTreasure);
        return thisTreasure;
    }    

    public Level LoadLevel()
    {
        Vector3 pos = Vector3.zero;
        Quaternion rot = levelPrefab.transform.rotation;
        //GameObject level = GameObject.Instantiate(level, pos, rot, parent);
        currentLevel = GameObject.Instantiate(levelPrefab, pos, rot);
        currentLevelScript = currentLevel.GetComponent<Level>();
        return currentLevelScript;
    }

    public void RemoveGameObject(GameObject taggedObject)
    {
        // set this up to use a pooling system

        if (taggedObject.tag == "Ammo")
        {
            //remove current ammo prefab, call for a new one to be spawned in a random short time interval
            GameObject.Destroy(taggedObject, 0.1f);
            currentLevelScript.SpawnNewPlayerAmmo();
        }
    }

}

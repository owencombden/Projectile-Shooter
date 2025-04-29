using UnityEngine;
using System.Collections.Generic;

public class AssetManager : MonoBehaviour
{
    public static AssetManager Instance;

    [Header("Prefabs")]
    [SerializeField]private GameObject playerPrefab;
    [SerializeField]private GameObject aiPrefab;
    [SerializeField]private GameObject platformPrefab;
    [SerializeField]private GameObject hexTilePrefab;
    [SerializeField]private GameObject bulletPrefab;
    [SerializeField]private GameObject ammoSpawnPrefab;
    
    private Dictionary<string, Queue<GameObject>> poolDict = new();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
        
        Instance.PrewarmPool("player", playerPrefab, 4);
        Instance.PrewarmPool("ai", aiPrefab, 4);
        Instance.PrewarmPool("platform", platformPrefab, 10);
        Instance.PrewarmPool("hextile", hexTilePrefab, 1500);
        Instance.PrewarmPool("bullet", bulletPrefab, 5);
        Instance.PrewarmPool("ammospawn", ammoSpawnPrefab, 10);
    }

    // Convenience wrappers
    public GameObject GetAI(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("ai", aiPrefab, position, rotation);
    }

    public void ReturnAI(GameObject ai)
    {
        ReturnToPool("ai", ai);
    }

    public GameObject GetAmmoSpawn(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("ammospawn", ammoSpawnPrefab, position, rotation);
    }

    public void ReturnAmmoSpawn(GameObject ammoSpawn)
    {
        ReturnToPool("ammospawn", ammoSpawn);
    }

    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("bullet", bulletPrefab, position, rotation);
    }

    public void ReturnBullet(GameObject bullet)
    {
        ReturnToPool("bullet", bullet);
    }

    public GameObject GetHexTile(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("hextile", platformPrefab, position, rotation);
    }

    public void ReturnHexTile(GameObject hextile)
    {
        ReturnToPool("hextile", hextile);
    }

    public GameObject GetPlatform(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("platform", platformPrefab, position, rotation);
    }

    public void ReturnPlatform(GameObject platform)
    {
        ReturnToPool("platform", platform);
    }

    public GameObject GetPlayer(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("player", playerPrefab, position, rotation);
    }

    public void ReturnPlayer(GameObject player)
    {
        ReturnToPool("player", player);
    }

    public void PrewarmPool(string key, GameObject prefab, int count)
    {
        if (!poolDict.ContainsKey(key))
            poolDict[key] = new Queue<GameObject>();

        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab);
            obj.SetActive(false);
            poolDict[key].Enqueue(obj);
            obj.transform.name = obj.transform.name + "_" + i;
            obj.transform.parent = transform;
        }
    }

    private GameObject GetFromPool(string key, GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDict.ContainsKey(key))
        {
            poolDict[key] = new Queue<GameObject>();
        }

        GameObject obj;
        if (poolDict[key].Count > 0)
        {
            obj = poolDict[key].Dequeue();
            obj.transform.parent = null;
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
        }

        return obj;
    }

    private void ReturnToPool(string key, GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.transform.parent = transform;        
        poolDict[key].Enqueue(obj);        
    }    
}

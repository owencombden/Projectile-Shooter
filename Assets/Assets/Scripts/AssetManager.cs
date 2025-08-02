using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class AssetManager : MonoBehaviour
{
    public static AssetManager Instance;

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject aiPrefab;
    [SerializeField] private GameObject platformPrefab;
    [SerializeField] private GameObject hexTilePrefab;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject ammoSpawnPrefab;
    [SerializeField] private GameObject groundHitParticlesPrefab;
    [SerializeField] private GameObject bulletHitParticlesPrefab;

    private Dictionary<string, Queue<GameObject>> poolDict = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        // Pool static props immediately:
        PrewarmPool("platform", platformPrefab, 10);
        PrewarmPool("hextile", hexTilePrefab, 1100);
        PrewarmPool("groundHitParticles", groundHitParticlesPrefab, 50);
        PrewarmPool("bulletHitParticles", bulletHitParticlesPrefab, 10);

        // Delay all networked pools until host/server is up:
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (clientId == 0 && NetworkManager.Singleton.IsServer)
            {
                PrewarmPool("bullet", bulletPrefab, 5);
                PrewarmPool("ammospawn", ammoSpawnPrefab, 0);
            }
        };
    }
    
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

    public GameObject GetBulletHitParticles(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("bulletHitParticles", bulletHitParticlesPrefab, position, rotation);
    }

    public float GetBulletHitParticlesDuration()
    {
        return bulletHitParticlesPrefab.GetComponent<ParticleSystem>().main.duration;
    }

    public void ReturnBulletHitParticles(GameObject particles)
    {
        ReturnToPool("bulletHitParticles", particles);
    }

    public GameObject GetGroundHitParticles(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("groundHitParticles", groundHitParticlesPrefab, position, rotation);
    }

    public float GetGroundHitParticlesDuration()
    {
        return groundHitParticlesPrefab.GetComponent<ParticleSystem>().main.duration;
    }

    public void ReturnGroundHitParticles(GameObject particles)
    {
        ReturnToPool("groundHitParticles", particles);
    }

    public GameObject GetHexTile(Vector3 position, Quaternion rotation)
    {
        return GetFromPool("hextile", hexTilePrefab, position, rotation);
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
        //Debug.Log("Getting a player from the pool.");
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
            // ONLY parent if it is NOT a NetworkObject
            if (!obj.TryGetComponent<NetworkObject>(out var _))
            {
                obj.transform.SetParent(transform);
            }
        }
    }

    private GameObject GetFromPool(string key, GameObject prefab, Vector3 position, Quaternion rotation)
    {

        //Debug.Log($"Getting a {prefab.name} from the pool...");

        if (!poolDict.ContainsKey(key))
        {
            poolDict[key] = new Queue<GameObject>();
        }

        GameObject obj;
        if (poolDict[key].Count > 0)
        {
            //Debug.Log($"...pool has {poolDict[key].Count} available.");
            obj = poolDict[key].Dequeue();
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
        //Debug.Log($"Deactivating {obj.name} and returning it to the pool.");
        obj.SetActive(false);
        obj.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        poolDict[key].Enqueue(obj);
        // ONLY parent if it is NOT a NetworkObject
        if (!obj.TryGetComponent<NetworkObject>(out var _))
        {
            obj.transform.SetParent(transform);
        }
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }    
}

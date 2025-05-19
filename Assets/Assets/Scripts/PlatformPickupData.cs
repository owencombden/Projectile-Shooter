using UnityEngine;
using System.Collections.Generic;

public class PlatformPickupData : MonoBehaviour
{
    public float nextSpawnTime = 5f;

    public List<GameObject> activePickups = new List<GameObject>();

    private Platform platformScript;

    void Start()
    {
        platformScript = GetComponent<Platform>();
    }

    public Vector3 GetRandomSpawnPoint()
    {
        Dictionary<Vector2Int, HexTile> thisHexMap = LevelManager.Instance.GetHexMap(GetComponent<Platform>().platformId);
        return HexUtils.GetRandomHexTile(thisHexMap).transform.position;
    }

    public void RegisterPickup(GameObject pickup)
    {
        activePickups.Add(pickup);
    }

    public void UnregisterPickup(GameObject pickup)
    {
        activePickups.Remove(pickup);
    }
}

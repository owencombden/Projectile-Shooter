using UnityEngine;
using System.Collections.Generic;

public class PickupPlatformData : MonoBehaviour
{
    public int platformId;
    public float nextSpawnTime;
    public List<GameObject> activePickups = new List<GameObject>();
    public Dictionary<Vector2Int, HexTile> hexMap;    

    public Vector3 GetRandomSpawnPoint()
    {
        return HexUtils.GetRandomHexTile(hexMap).transform.position;
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

using System.Collections.Generic;
using UnityEngine;

public class PickupManager : MonoBehaviour
{
    public int maxAmmoDrops = 1;
    private AssetManager assetManager;
    
    private Transform[] playerTiles;
    private int currentAmmoDrops = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetManager = GameObject.Find("AssetManager").GetComponent<AssetManager>();
            
    }

    // Update is called once per frame
    void Update()
    {
        if(currentAmmoDrops < maxAmmoDrops)
        {
            currentAmmoDrops += 1;
            Invoke("DropAmmo", 1f);
        }        
    }

    private void DropAmmo()
    {
        Vector3 spawnPos = assetManager.GetRandomPlayerHexScript().transform.position;
        spawnPos.y += 0.86f;
        assetManager.GetAmmoSpawnPrefab(spawnPos);
    }

    public void RemoveAmmoDrop()
    {
        currentAmmoDrops -= 1;
    }
}

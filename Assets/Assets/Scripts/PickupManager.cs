using UnityEngine;
using System.Collections.Generic;

public class PickupManager : MonoBehaviour
{
    public float globalSpawnCooldown = 5f;    // Time between spawn checks
    public int maxPickupsPerPlatform = 2;     // Max active pickups allowed per platform
    public int pickupAmmoAmount = 3;          // How much ammo a pickup gives

    private float spawnTimer = 0f;

    void Update()
    {
        /* taking this out while working on multiplayer
        
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            TrySpawnPickups();
            
            spawnTimer = globalSpawnCooldown;
        }

        */
        
    }

    private void TrySpawnPickups()
    {
        var allPlatformObjects = LevelManager.Instance.GetAllPlatformObjects();

        foreach (var kvp in allPlatformObjects)
        {
            GameObject platformGO           = kvp.Value;
            int platformId                  = platformGO.GetComponent<Platform>().platformId;
            PlatformPickupData platformData = platformGO.GetComponent<PlatformPickupData>();

            if (platformData == null || LevelManager.Instance.GetHexMap(platformId) == null || LevelManager.Instance.GetHexMap(platformId).Count == 0)
                continue;

            if (platformData.activePickups.Count >= maxPickupsPerPlatform)
                continue;

            Vector3 spawnPos = platformData.GetRandomSpawnPoint();
            spawnPos.y = 0.96f; // Ensure Y height is correct for pickups

            GameObject pickup = AssetManager.Instance.GetAmmoSpawn(spawnPos, Quaternion.identity);

            pickup.GetComponent<Pickup>().Initialize(
                PickupType.Ammo,
                pickupAmmoAmount,
                onCollected: null, // Optional: you could still hook score logic here
                lifetimeSeconds: 10f,
                ownerPlatform: platformData
            );

            platformData.RegisterPickup(pickup);
        }
    }
}

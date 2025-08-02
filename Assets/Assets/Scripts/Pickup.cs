using UnityEngine;
using System;
using Unity.Netcode;

public class Pickup : NetworkBehaviour
{
    public PickupType type;
    public int amount;

    private Action<GameObject> onCollectedCallback;
    private float lifetime;
    private float spawnTime;

    private PlatformPickupData platformData; 

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        // only server has authority to manage pickups
        if (!IsServer) return;

        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
        return;

        if (lifetime > 0f && Time.time - spawnTime > lifetime)
        {
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {        
        // only server has authority to detect pickup collisions
        if (!IsServer) return;

        //debugging
        if (other.CompareTag("Player"))
        {
            //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is handling a pickup onTrigger for Client {other.transform.GetComponent<PlayerController>().id.Value}.");
        }
        else if (other.CompareTag("AI_Player"))
        {
            //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is handling a pickup onTrigger for Client {other.transform.GetComponent<AIController>().id.Value}.");
        }
        

        
        if (other.CompareTag("Player") || other.CompareTag("AI_Player"))
        {
            ApplyPickupEffect(other.gameObject);

            onCollectedCallback?.Invoke(other.gameObject);

            ReturnToPool();
        }
    }

    public void Initialize(
        PickupType pickupType,
        int pickupAmount,
        Action<GameObject> onCollected,
        float lifetimeSeconds,
        PlatformPickupData ownerPlatform
    )
    {
        this.type = pickupType;
        this.amount = pickupAmount;
        this.onCollectedCallback = onCollected;
        this.lifetime = lifetimeSeconds;
        this.spawnTime = Time.time;
        this.platformData = ownerPlatform;
    }

    private void ApplyPickupEffect(GameObject collector)
    {
        switch (type)
        {
            case PickupType.Ammo:
                var shooter = collector.GetComponent<CharacterShooter>();
                if (shooter != null)
                {
                    shooter.AddAmmo(amount);                    
                }
                break;

            // Add more cases like Health, Shield, etc. when needed
        }
    }
    

    public void ReturnToPool()
    {
        // only server has authority to despawn pickups
        if (!IsServer) return;

        // Unregister on expiration
        platformData?.UnregisterPickup(this.gameObject);

        // Tell Netcode to despawn this pickup prefab (but we're pooling so don’t destroy the GameObject!!)
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(destroy: false);
        }

        AssetManager.Instance.ReturnAmmoSpawn(this.gameObject);
    }
}

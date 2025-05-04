using UnityEngine;
using System;

public class Pickup : MonoBehaviour
{
    public PickupType type;
    public int amount;

    private Action<GameObject> onCollectedCallback;
    private float lifetime;
    private float spawnTime;

    private PickupPlatformData platformData; 

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    private void Update()
    {
        if (lifetime > 0f && Time.time - spawnTime > lifetime)
        {
            platformData?.UnregisterPickup(this.gameObject); // Unregister on expiration
            ReturnToPool();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Pickup reported: {transform.name} hit {other.gameObject.name} at {other.transform.position}");

        if (other.CompareTag("Player") || other.CompareTag("AI_Player"))
        {
            ApplyPickupEffect(other.gameObject);

            onCollectedCallback?.Invoke(other.gameObject);

            platformData?.UnregisterPickup(this.gameObject); // Unregister on collection
            ReturnToPool();
        }
    }

    public void Initialize(
        PickupType pickupType,
        int pickupAmount,
        Action<GameObject> onCollected,
        float lifetimeSeconds,
        PickupPlatformData ownerPlatform
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

    private void ReturnToPool()
    {
        AssetManager.Instance.ReturnAmmoSpawn(this.gameObject); // Replace with generic method if available
    }
}

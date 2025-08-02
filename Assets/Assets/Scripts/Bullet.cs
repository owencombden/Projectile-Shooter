using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{    
    public enum BulletType { Normal, Fire, Ice, Rock }
    public BulletType bulletType = BulletType.Normal;
    [Header("Blast Settings")]
    public float blastForce = 100f;  // the AOE force applied, scaled for distance
    public float maxLifetime = 7f;
    public Vector3 startPos { get; private set; }    
    public ulong ownerId;  

    private Rigidbody rb;
    private TrailRenderer trailRenderer;
    private float lifetime = 0f;
    private bool _hasTriggered = false;
    private Vector3 cachedVelocity = Vector3.zero;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        //Debug.Log($"Bullet has spawned on the network.");
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        if (!IsServer) return;
        //Debug.Log($"{transform.name} has been enabled with velocity {rb.linearVelocity}.");
        startPos = transform.position;
        rb.isKinematic = false; 
        rb.linearVelocity = Vector3.zero;        
        _hasTriggered = false; 
    }

    // reset the bullet when it's deactivated (returned to the pool)
    private void OnDisable()
    {
        if (!IsServer) return;
        //Debug.Log($"{transform.name} has been disabled.");
        startPos = Vector3.zero;      
        ownerId = ulong.MaxValue;
        trailRenderer.Clear();
        lifetime = 0f;       
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        // handle paused
        if (PauseManager.Instance.isPaused.Value)
        {
            if (rb.linearVelocity != Vector3.zero)
            {
                cachedVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            return;
        }
        else
        {
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                rb.linearVelocity = cachedVelocity;
            }
        }

        //Debug.Log($"Bullet is airborne with velocity {rb.linearVelocity}");

        // handle bullet lifespan
        lifetime += Time.deltaTime;
        if(!isAlive())
        {                   
            DestroyBullet();
        }        
    }    
    
    private void OnTriggerEnter(Collider other)
    {
        // trigger-related collisions here.  see also OnCollisionEnter below

        // server handles all collisions and should be the source of truth
        if (!IsServer) return;

        if (_hasTriggered) return;  // Already processed
        _hasTriggered = true;       // Set the flag to avoid further triggers

        //Debug.Log($"{transform.name} has triggered {other.name}");
        if (other.CompareTag("Water"))
        {
            return;
        }
        else if (other.CompareTag("Ground"))
        {
            HexTile hexScript = other.GetComponentInParent<HexTile>();
            //Debug.Log($"{transform.name} triggered {hexScript.gridCoords}");
            if (hexScript != null)
            {
                // cache the position info, this tile may get destroyed
                ulong platformId = hexScript.ownerId;
                Vector2Int gridPos = hexScript.gridCoords;

                // calculate the damage, and inform all clients to apply the damage locally 
                float damage = GetBaseDamage();
                int radius = GetBlastRadius();
                LevelManager.Instance.ApplyBlastDamageClientRpc(platformId, gridPos, damage, radius);
            }

            // apply camera shake on the player that owns this platform
            ulong ownerID = hexScript.ownerId;
            var rpcParams = new ClientRpcParams{
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { ownerID }
                }
            };
            Debug.Log($"Shaking camera on character ID: {ownerID}");
            bool hardShake = false;
            LevelManager.Instance.ApplyCameraShakeClientRpc(hardShake, rpcParams);
        }
        
        DestroyBullet();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // collider-related collisions here.  see also OnTriggerEnter above

        // server handles all collisions and should be the source of truth
        if (!IsServer) return;

        Vector3 blastDirection = (collision.transform.position - startPos).normalized;
        blastDirection.y = 0;

        // Debug.Log($"{transform.name} has collided with {collision.transform.name}");
        if (collision.collider.CompareTag("Player"))
        {
            PlayerController player = collision.transform.GetComponent<PlayerController>();
            if (player != null && player.id.Value != ownerId)
            {
                //Debug.Log($"Bullet is applying blast force...");
                player.ReceiveKnockbackBlastClientRpc(blastDirection, blastForce);

                // apply camera shake on the player that was hit
                ulong ownerID = player.id.Value;
                var rpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { ownerID }
                    }
                };
                Debug.Log($"Shaking camera on character ID: {ownerID}");
                bool hardShake = true;
                LevelManager.Instance.ApplyCameraShakeClientRpc(hardShake, rpcParams);

            }

            DestroyBullet();
        }
        else if (collision.collider.CompareTag("AI_Player"))
        {
            AIController ai = collision.transform.GetComponent<AIController>();
            if (ai != null && ai.id.Value != ownerId)
            {
                ai.ApplyBlastForce(blastDirection, blastForce);
            }

            DestroyBullet();
        }
        else if (collision.collider.CompareTag("Bullet"))
        {           
            // play some bullet-on-bullet collision particles
            LevelManager.Instance.ApplyBulletCollisionParticlesClientRPC(transform.position);
        }

    }

    private float GetBaseDamage()
    {
        return bulletType switch
        {
            BulletType.Fire => 60f,
            BulletType.Ice  => 40f,
            BulletType.Rock => 80f,
            _               => 50f, //"Normal" was 50
        };
    }

    private int GetBlastRadius()
    {
        return bulletType switch
        {
            BulletType.Fire => 3,
            BulletType.Ice  => 2,
            BulletType.Rock => 1,
            _               => 5, //"Normal" was 4
        };
    }
    
    public Vector3 GetVelocity() => rb.linearVelocity;    

    private bool isAlive()
    {        
        return transform.position.y > -1f && lifetime < maxLifetime;
    }

    void DestroyBullet()
    {
        if (!IsServer) return;

        //Debug.Log($"Destroying bullet..."); 

        // Tell Netcode to despawn this bullet (but we're pooling so don’t destroy the GameObject!!)
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(destroy: false);
        }

        // return object to the pool, OnDisable will be called to reset the bullet when disabled
        if (AssetManager.Instance != null && gameObject != null)
        {
            AssetManager.Instance.ReturnBullet(gameObject);
        }
        
    }    
}

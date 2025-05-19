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
    public int ownerId;  

    private Rigidbody rb;
    private TrailRenderer trailRenderer;
    private float lifetime = 0f;
    private bool _hasTriggered = false;


    // called only once when instantiated!
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();         
    }

    private void OnEnable()
    {
        //Debug.Log($"{transform.name} has been enabled.");
        startPos = transform.position;        
        _hasTriggered = false; 
    }

    // reset the bullet when it's deactivated (returned to the pool)
    private void OnDisable()
    {
        //Debug.Log($"{transform.name} has been disabled.");
        startPos = Vector3.zero;        
        ownerId = -1;
        rb.linearVelocity = Vector3.zero;
        trailRenderer.Clear();
        lifetime = 0f;       
    }

    // Update is called once per frame
    void Update()
    {
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
            //Debug.Log($"{transform.name} triggered {other.transform.parent.name}");
            HexTile hexScript = other.GetComponentInParent<HexTile>();
            if (hexScript != null)
            {
                float damage = GetBaseDamage();
                int radius = GetBlastRadius();
                hexScript.ApplyBlastDamage(damage, radius);
            }
        }
        
        DestroyBullet();

        // also have a helper below to updates clients when required.
        // server handles all bullet collision/physics, but clients can handle their own visual/sound effects, etc.
        // call function below if wanting to send a message to client about the impact
        // NotifyClientsOfImpact(Vector3 position) 
    }

    private void OnCollisionEnter(Collision collision)
    {
        // collider-related collisions here.  see also OnTriggerEnter above

        // server handles all collisions and should be the source of truth
        if (!IsServer) return;

        // Debug.Log($"{transform.name} has collided with {collision.transform.name}");
        if (collision.collider.CompareTag("Player"))
        {
            Vector3 blastDirection = (collision.transform.position - transform.position).normalized;
            blastDirection.y = 0;

            PlayerController player = collision.transform.GetComponent<PlayerController>();
            if (player != null && player.id != ownerId)
            {   
                //Debug.Log($"Bullet is applying blast force...");
                player.ApplyBlastForce(blastDirection, blastForce);
            }  

            Destroy(gameObject);
        }
        else if (collision.collider.CompareTag("AI_Player"))
        {
            AIController controller = collision.transform.GetComponent<AIController>();
            if (controller != null && controller.id != ownerId)
            {
                //controller.ApplyBlastForce(transform.position, blastForce, blastRadius);
            }
        }

        // also have a helper below to updates clients when required.
        // server handles all bullet collision/physics, but clients can handle their own visual/sound effects, etc.
        // call function below if wanting to send a message to client about the impact
        // NotifyClientsOfImpact(Vector3 position) 
    }

    [ClientRpc]
    void NotifyClientsOfImpactClientRpc(Vector3 position)
    {
        // Trigger visual effects on clients
    }
    

    private float GetBaseDamage()
    {
        return bulletType switch
        {
            BulletType.Fire => 60f,
            BulletType.Ice  => 40f,
            BulletType.Rock => 80f,
            _               => 50f,
        };
    }

    private int GetBlastRadius()
    {
        return bulletType switch
        {
            BulletType.Fire => 3,
            BulletType.Ice  => 2,
            BulletType.Rock => 1,
            _               => 4,
        };
    }
    
    public Vector3 GetVelocity() => rb.linearVelocity;    

    private bool isAlive()
    {        
        return transform.position.y > -1f && lifetime < maxLifetime;
    }

    void DestroyBullet()
    {
        // Tell Netcode to despawn this bullet (but we're pooling so don’t destroy the GameObject!!)
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(destroy: false);
        }

        // return object to the pool, OnDisable will be called to reset the bullet when disabled
        AssetManager.Instance.ReturnBullet(gameObject);
    }
    
}

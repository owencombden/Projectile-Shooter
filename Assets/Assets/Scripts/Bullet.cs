using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float maxLifetime = 7f;
    public enum BulletType { Normal, Fire, Ice, Rock }
    public BulletType bulletType = BulletType.Normal;
    public Vector3 startPos { get; private set; }    
    public string ownerId   { get; set; }  

    private Rigidbody rb;
    private TrailRenderer trailRenderer;
    private float lifetime = 0f;

    // called only once when instantiated!
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trailRenderer = GetComponent<TrailRenderer>();         
    }

    private void OnEnable()
    {
        startPos = transform.position;
    }

    // reset the bullet when it's deactivated (returned to the pool)
    private void OnDisable()
    {
        startPos = Vector3.zero;        
        ownerId = null;
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
    
    public Vector3 GetVelocity() => rb.linearVelocity;    

    private bool isAlive()
    {        
        return transform.position.y > -1f && lifetime < maxLifetime;
    }

    void DestroyBullet()
    {
        // return object to the pool, OnDisable will be called to reset the bullet when disabled
        AssetManager.Instance.ReturnBullet(gameObject);
    }
    
}

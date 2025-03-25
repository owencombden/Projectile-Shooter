using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    public Transform spawnpoint;

    float launchVelocity;
    AssetManager assetManager;
    GameObject bullet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetManager = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        
    }
    
    public void SetLaunchVelocity(float vel)
    {
        launchVelocity = vel;
    }

    public void ShootAtPlayer()
    {
        // 50/50 chance enenmy will shoot
        int prob = Random.Range(0, 100);
        if (prob < 50) { return; }

        // shoot in forward direction, at calculated angle
        bullet = assetManager.GetEnemyBullet(spawnpoint.transform.position);        
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(launchVelocity * spawnpoint.forward, ForceMode.Impulse);
    }
}

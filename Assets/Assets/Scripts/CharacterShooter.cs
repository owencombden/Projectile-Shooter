using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CharacterShooter : MonoBehaviour
{
    public float maxSpeed = 350f;
    public float maxHeight = 10f;  //make this a range, map it to distance
    public float maxAngle = 45f;  
    public float minAngle = 0;
    public float minInterceptTime = 0.1f;
    public float maxInterceptTime = 1.0f;
    public Transform gun;
    public Transform spawnpoint;

    private AssetManager assetManager;
    private float calculatedLaunchVelocity;
    private Rigidbody targetRB;
    private Vector3 shotOrigin;
    private float interceptTime;
    private Vector3 futurePos;
    private Vector3 gravityCompensation;

    private int currentAmmo = 3;
    private int maxAmmo = 3;  
    private float shootCooldown = 0.5f;
    private float lastShootTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetManager = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        // gun and spawnpoint must be children of the character
        gun = transform.Find("Gun");
        spawnpoint = transform.Find("Gun/SpawnPoint");

        //start with a full clip
        //currentAmmo = maxAmmo;
    }

    // Update is called once per frame
    void Update()
    { 
               
    }

    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
        currentAmmo = Mathf.Min(currentAmmo, maxAmmo);
        //Debug.Log($"...added ammo, character now has {GetCurrentAmmo()}");
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }
    
    public void TryShoot(Transform target)
    {   
        // check ammo
        if(currentAmmo <= 0) return;

        if (Time.time - lastShootTime < shootCooldown) return;
        lastShootTime = Time.time;             

        if (target.tag == "Ground" || target.tag == "Player" || target.tag == "AI_Player") 
        { 
            ShootAtPosition(target.position);
        }

        // short tween to rotate/aim, then ShootAtEnemyBullet()
        if (target.tag == "Bullet") { AimAtEnemyBullet(target); }
        
    }
    
    public void ShootAtPosition(Vector3 targetPosition)
    {
        // calculate the required angle and launchVelocity from two inputs: distToTarget and maxHeight
        // maxHeight can be set in the inspector to allow loftier shots
        // use maxHeight and distance to get necessary angle and launch velocity

        // get the dist to target, and the required gun angle.  finetune aim if needed
        float distToTarget = Vector3.Distance(transform.position, targetPosition);
        distToTarget *= 0.97f;
        float targetAngle = Mathf.Atan(4 * maxHeight/distToTarget) * Mathf.Rad2Deg;

        //get the required velocity, at that angle, to reach maxHeight
        // normal velocity is around 30
        calculatedLaunchVelocity = CalculateLaunchVelocity(distToTarget, maxHeight); // float

        // cap velocity at 'maxSpeed'
        if (calculatedLaunchVelocity > maxSpeed) { calculatedLaunchVelocity = maxSpeed; }
        
        // set the required gun angle 
        Vector3 currentGunRot = gun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(targetAngle,currentGunRot.y,currentGunRot.z);
        gun.transform.eulerAngles = gunRot;   
       
        // shoot in forward direction, at calculated angle
        Vector3 shotVelocity = calculatedLaunchVelocity * spawnpoint.forward;
        SpawnBulletServerRpc(shotVelocity);
        
        currentAmmo--;
    }    

    public void AimAtEnemyBullet(Transform target)
    {
        if (target == null) return;

        targetRB   = target.GetComponent<Rigidbody>();
        shotOrigin = target.GetComponent<Bullet>().startPos;

        // Projected horizontal distance to target (ignore height)
        Vector3 flatTargetPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        float currentDistToTarget = Vector3.Distance(transform.position, flatTargetPos);

        float maxPossibleDist = Mathf.Max(1f, Vector3.Distance(transform.position, shotOrigin)); // Prevent divide-by-zero

        // Map the distance to an intercept time, clamped to the desired range
        interceptTime = MapValueToRange(
            currentDistToTarget,
            1f, maxPossibleDist,
            minInterceptTime, maxInterceptTime
        );

        // Clamp the result just to be safe (in case input values or ranges shift)
        interceptTime = Mathf.Clamp(interceptTime, minInterceptTime, maxInterceptTime);

        // Predict future position with gravity compensation
        futurePos = target.position + (targetRB.linearVelocity * interceptTime);
        gravityCompensation = 0.5f * Physics.gravity * interceptTime * interceptTime;
        futurePos -= gravityCompensation;

        if (futurePos.y < 2f || futurePos.y > 30f) return;

        // Rotate to face the futurePos
        Vector3 lookDir = futurePos - transform.position;
        lookDir.y = 0;
        Vector3 lookDirAngle = Quaternion.LookRotation(lookDir).eulerAngles;

        float angleDist = Vector3.Angle(transform.forward, lookDir);
        float rotateTime = angleDist / (2f * 200f);
        
        StartCoroutine(RotateThenShoot(lookDir, rotateTime));       
    }

    private IEnumerator RotateThenShoot(Vector3 lookDir, float duration)
    {
        Quaternion startRotation = transform.rotation;
        Quaternion endRotation = Quaternion.LookRotation(lookDir);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, endRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRotation;
        ShootAtEnemyBullet();
    }


    void ShootAtEnemyBullet()
    {
        // shoot the projectile, attempt to intercept the incoming bullet        
        Vector3 shotDir = (futurePos + gravityCompensation - spawnpoint.position).normalized;
        float distToTarget = Vector3.Distance(futurePos, spawnpoint.position);
        float requiredSpeed = distToTarget / interceptTime;

        // cap velocity at 'maxSpeed'
        if (requiredSpeed > maxSpeed) { requiredSpeed = maxSpeed; }

        Vector3 shotVelocity = requiredSpeed * shotDir;
        SpawnBulletServerRpc(shotVelocity);

        currentAmmo--;
    }

    // NGO uses a server-authoritative model, bullets should be spawned on the server to ensure consistency across clients. 
    [ServerRpc]
    void SpawnBulletServerRpc(Vector3 shotVel)
    {
        GameObject bullet = AssetManager.Instance.GetBullet(spawnpoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = GetMyId();
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(shotVel, ForceMode.Impulse);

        bullet.GetComponent<NetworkObject>().Spawn();
    }


    public static float CalculateLaunchVelocity(float distance, float maxHeight)
    {
        float g = Physics.gravity.y * -1;        
        // Calculate launch angle
        float theta = Mathf.Atan((4 * maxHeight) / distance);        
        // Compute sin(theta)
        float sinTheta = Mathf.Sin(theta);        
        // Calculate initial velocity
        float v0 = Mathf.Sqrt((2 * g * maxHeight) / (sinTheta * sinTheta));        
        return v0;
    }

    private int GetMyId()
    {
        if (transform.tag == "Player")
        {
            return GetComponent<PlayerController>().id;
        }
        else if (transform.tag == "AI_Player")
        {
            return GetComponent<AIController>().id;
        }
        else{ Debug.Log("Could not set ownerID on Bullet!"); }

        return -999;
    }


    public static float MapValueToRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }
    
}

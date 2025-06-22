using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CharacterShooter : NetworkBehaviour
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

    [ServerRpc]
    public void TryShootServerRPC(Vector3 targetPosition, string targetTag)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        //  Who sent this?
        var clientObj = NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject;
        var clientShooter = clientObj.GetComponent<CharacterShooter>();
        
        Debug.Log($"Server (Client {NetworkManager.Singleton.LocalClientId}) is handling a shot request from Client {OwnerClientId}");

        // check ammo
        if (clientShooter.currentAmmo <= 0) return;

        if (Time.time - clientShooter.lastShootTime < clientShooter.shootCooldown) return;
        clientShooter.lastShootTime = Time.time;

        if (targetTag == "Ground" || targetTag == "Player" || targetTag == "AI_Player")
        {
            float fineTune = 0.97f;  //finetune range if needed
            float distToTarget = Vector3.Distance(transform.position, targetPosition) * fineTune;

            // calculate the angle, rotate the gun to position, get the required speed
            // shoot in the direction the gun is pointing
            float shotSpeed = AimAtTarget(distToTarget, clientShooter.gun);

            Debug.Log($"Shot speed: {shotSpeed}");

            Vector3 shotVelocity = shotSpeed * clientShooter.spawnpoint.forward;

            Debug.Log($"Shot velocity: {shotVelocity}");

            SpawnBullet(shotVelocity, clientShooter.spawnpoint.position, clientObj);

            clientShooter.currentAmmo--;
        }

        // removed while focusing on ground hits.
        // when implementing, possibly need to have the server rotate the client...is that doable?  wise?
        // maybe revamp shooting altogether

        // short tween to rotate/aim, then ShootAtEnemyBullet()
        //if (targetTag == "Bullet") { AimAtEnemyBullet(target); }


    }
    
    public float AimAtTarget(float distToTarget, Transform clientGun)
    {
        // calculate the required angle and launchVelocity from two inputs: distToTarget and maxHeight
        // maxHeight can be set in the inspector to allow loftier shots
        // use maxHeight and distance to get necessary angle and launch velocity

        // get and set the required gun angle.  
        float targetAngle = Mathf.Atan(4 * maxHeight/distToTarget) * Mathf.Rad2Deg;
        Vector3 currentGunRot = clientGun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(targetAngle,currentGunRot.y,currentGunRot.z);
        clientGun.transform.eulerAngles = gunRot;   

        // get the required velocity, at that angle, to reach maxHeight. 
        float calculatedLaunchVelocity = CalculateLaunchVelocity(distToTarget, maxHeight); // normal velocity is around 30

        // cap velocity at 'maxSpeed'
        if (calculatedLaunchVelocity > maxSpeed) { calculatedLaunchVelocity = maxSpeed; }
        return calculatedLaunchVelocity;
    }

    /*
    public void AimAtEnemyBullet(Transform target)
    {
        if (target == null) return;

        targetRB = target.GetComponent<Rigidbody>();
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
        SpawnBullet(shotVelocity);

        currentAmmo--;
    }
    */

    void SpawnBullet(Vector3 shotVel, Vector3 spawnPos, NetworkObject clientObj)
    {
        Debug.Log($"Bullet is being spawned by Client {NetworkManager.Singleton.LocalClientId} as requested by Client {clientObj.GetComponent<PlayerController>().id}");
        GameObject bullet = AssetManager.Instance.GetBullet(spawnPos, Quaternion.identity);
        bullet.GetComponent<NetworkObject>().Spawn();
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = GetMyId(clientObj);             // currently the client requesting the shot is being assigned as the bullet 'owner'
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(shotVel, ForceMode.Impulse);
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

    private ulong GetMyId(NetworkObject clientObj)
    {
        if (clientObj.tag == "Player")
        {
            return clientObj.GetComponent<PlayerController>().id.Value;
        }
        else if (clientObj.tag == "AI_Player")
        {
            return clientObj.GetComponent<AIController>().id.Value;
        }
        else{ Debug.Log("Could not set ownerID on Bullet!"); }

        return ulong.MaxValue;
    }


    public static float MapValueToRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }
    
}

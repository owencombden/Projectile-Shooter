using UnityEngine;
using System.Collections;
using Unity.Netcode;

public class CharacterShooter : NetworkBehaviour
{
    public float maxSpeed = 350f;
    public float maxHeight = 10f;  // apoapsis
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

    public NetworkVariable<int> currentAmmo;
    public int maxAmmo = 10;  
    public float shootCooldown = 0.2f;
    public float lastShootTime;

    public bool isHuman = false;


    void Start()
    {
        if (!IsOwner) { return; }

        assetManager = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        // gun and spawnpoint must be children of the character
        gun = transform.Find("Gun");
        spawnpoint = transform.Find("Gun/SpawnPoint");

        isHuman = GetComponent<PlayerController>() == null ? false : true;

        if (isHuman)
        {
            GameplayUI.Instance.SetMaxLimitOnAmmoProgressBar(maxAmmo);
            GameplayUI.Instance.SetAmmoUI(currentAmmo.Value);
        }        
    }

    // Update is called once per frame
    void Update()
    { 
               
    }

    public void AddAmmo(int amount)
    {
        currentAmmo.Value += amount;
        currentAmmo.Value = Mathf.Min(currentAmmo.Value, maxAmmo);

        // set the UI
        if (isHuman)
        {
            GameplayUI.Instance.SetAmmoUI(currentAmmo.Value);
        }
        

        //debugging
        if (transform.tag == "Player")
        {
            //Debug.Log($"Added {amount} ammo for Client {OwnerClientId}.  Current Ammo: {GetCurrentAmmo()}.");
        }
        else if (transform.tag == "AI_Player")
        {
            //Debug.Log($"Added {amount} ammo for AI {transform.GetComponent<AIController>().id.Value}.  Current Ammo: {GetCurrentAmmo()}.");
        }
        
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo.Value;
    }

    [ServerRpc]
    public void SpawnBulletServerRPC(NetworkObjectReference shooterRef, ulong clientID, Vector3 spawnPos, Vector3 shotVel)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        //Debug.Log($"Server is trying to spawn a bullet for Client {clientID}");

        if (!shooterRef.TryGet(out NetworkObject netObj))
        {
            Debug.LogWarning("Invalid shooter reference");
            return;
        }

        CharacterShooter shooter = netObj.GetComponent<CharacterShooter>();
        if (shooter == null)
        {
            Debug.LogWarning("Shooter script not found on NetworkObject");
            return;
        }

        GameObject bullet = AssetManager.Instance.GetBullet(spawnPos, Quaternion.identity);
        bullet.GetComponent<NetworkObject>().Spawn();
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = clientID;             // currently the client requesting the shot is being assigned as the bullet 'owner'
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(shotVel, ForceMode.Impulse);

        //Debug.Log($"Server (Client {NetworkManager.Singleton.LocalClientId}) is spawning a bullet for Client {clientID}");

        // server handles ammo management
        shooter.currentAmmo.Value--;

        // adjust ammo on UI
        if (isHuman)
        {
            GameplayUI.Instance.SetAmmoUI(currentAmmo.Value);
        }
        

        //Debug.Log($"Server (Client {NetworkManager.Singleton.LocalClientId}) is reducing ammo for Client {clientID}.  Current Ammo is now {shooter.currentAmmo.Value}");
    }
    
    public float AimAtTarget(float distToTarget, Transform gun)
    {
        // calculate the required angle and launchVelocity from two inputs: distToTarget and maxHeight
        // maxHeight can be set in the inspector to allow loftier shots
        // use maxHeight and distance to get necessary angle and launch velocity

        // get and set the required gun angle.  
        float targetAngle = Mathf.Atan(4 * maxHeight/distToTarget) * Mathf.Rad2Deg;
        Vector3 currentGunRot = gun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(targetAngle,currentGunRot.y,currentGunRot.z);
        gun.transform.eulerAngles = gunRot;   

        // get the required velocity, at that angle, to reach maxHeight. 
        float calculatedLaunchVelocity = CalculateLaunchVelocity(distToTarget, maxHeight); // normal velocity is around 30

        // cap velocity at 'maxSpeed'
        if (calculatedLaunchVelocity > maxSpeed) { calculatedLaunchVelocity = maxSpeed; }
        return calculatedLaunchVelocity;
    }

    
    public Vector3 AimAtEnemyBullet(Transform target)
    {
        if (target == null) return Vector3.zero;

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

        if (futurePos.y < 2f || futurePos.y > 30f) return Vector3.zero;

        // Rotate to face the futurePos
        Vector3 lookDir = futurePos - transform.position;
        lookDir.y = 0;

        return lookDir;

        
    }

    public IEnumerator RotateToFuturePos(Vector3 lookDir, float duration)
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
    }

    public Vector3 ShootAtEnemyBullet()
    {
        // shoot the projectile, attempt to intercept the incoming bullet        
        Vector3 shotDir = (futurePos + gravityCompensation - spawnpoint.position).normalized;
        float distToTarget = Vector3.Distance(futurePos, spawnpoint.position);
        float requiredSpeed = distToTarget / interceptTime;

        // cap velocity at 'maxSpeed'
        if (requiredSpeed > maxSpeed) { requiredSpeed = maxSpeed; }

        Vector3 shotVelocity = requiredSpeed * shotDir;
        return shotVelocity;
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

    


    public static float MapValueToRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }
    
}

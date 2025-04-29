using UnityEngine;

public class PlayerShoot : MonoBehaviour
{
    /*
    
    public float shootFromTheHipLaunchVelocity = 25f;
    public float maxHeight = 10f;  //make this a range, map it to distance
    public float maxAngle = 45f;  
    public float minAngle = 0;
    public float minInterceptTime;
    public float maxInterceptTime;
    public Transform gun;
    public Transform spawnpoint;
    
    private PlayerMaster playerMasterScript;

    private float calculatedLaunchVelocity;
    private Rigidbody targetRB;
    private Vector3 shotOrigin;
    private float interceptTime;
    private Vector3 futurePos;
    private Vector3 gravityCompensation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMasterScript = transform.GetComponent<PlayerMaster>();
    }

    // Update is called once per frame
    void Update()
    { 
               
    }
    
    public void UpdateGunAngle(float cameraXAngle)
    {
        // used with shooting-from-hip
        // if player looks up, the camera swings up, and gun should rotate up        
        Vector3 currentGunRot = gun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(cameraXAngle,currentGunRot.y,currentGunRot.z);
        gun.transform.eulerAngles = gunRot; 
    }        

    public void ShootFromTheHip()
    {
        GameObject bullet; 
        bool playerHasAmmo = playerMasterScript.GetNextBullet(spawnpoint.transform.position, out bullet);
        if(playerHasAmmo)
        {
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            bulletRb.AddForce(shootFromTheHipLaunchVelocity * spawnpoint.forward, ForceMode.Impulse);
        }
        else { Debug.Log("Shooting from the hip -> No Ammo! "); }
    }
    

    public void ShootAtGround(object hitParam)
    {
        // calculate the required angle and launchVelocity from two inputs: distToTarget and maxHeight
        // maxHeight can be set in the inspector to allow loftier shots
        // use maxHeight and distance to get necessary angle and launch velocity

        // get the required gun angle
        RaycastHit hit = (RaycastHit) hitParam;
        float distToTarget = Vector3.Distance(transform.position, hit.point);
        float targetAngle = Mathf.Atan(4 * maxHeight/distToTarget) * Mathf.Rad2Deg;

        //get the required velocity, at that angle, to reach maxHeight
        calculatedLaunchVelocity = CalculateLaunchVelocity(distToTarget, maxHeight);
        
        // set the required gun angle 
        Vector3 currentGunRot = gun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(targetAngle,currentGunRot.y,currentGunRot.z);
        gun.transform.eulerAngles = gunRot;        
       
        // shoot in forward direction, at calculated angle
        GameObject bullet; 
        bool playerHasAmmo = playerMasterScript.GetNextBullet(spawnpoint.transform.position, out bullet);
        if(playerHasAmmo)
        {
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            bulletRb.AddForce(calculatedLaunchVelocity * spawnpoint.forward, ForceMode.Impulse);
        }
        else { Debug.Log("Shooting at clicked target -> No Ammo! "); }
    }

    public void AimAtEnemyBullet(object hitParam)
    {
        RaycastHit hit = (RaycastHit) hitParam;
        targetRB = hit.transform.GetComponent<Rigidbody>();
        shotOrigin = hit.transform.GetComponent<Bullet>().GetStartPos();

        // choose a time (from now) in which we want to intercept the bullet
        //  far away targets = more time, closer targets = less time (close to immediate)
        // use the time to calculate where the enemey bullet will be, adjust the raw position by subtracting the gravity forces
        // then calculate a shot, to that future position
        // the speed of our projectile will be determined by the intercept time (lower time, faster shot required)
        // we shoot in the direction of the future pos, but also account for gravity resulting in a shot that's a little higher than futurePos

        // map interceptTime to distance-to-target
        float currentDistToTarget = Vector3.Distance(transform.position, hit.transform.position);
        float maxPossibleDist = Vector3.Distance(transform.position, shotOrigin);
        interceptTime = MapValueToRange(currentDistToTarget, 0, maxPossibleDist, minInterceptTime, maxInterceptTime);

        //Debug.Log("Intercept Time is: " + interceptTime);

        // calc where the incoming projectile will be in interceptTime seconds
        futurePos = hit.transform.position + (targetRB.linearVelocity * interceptTime);
        gravityCompensation = new Vector3(0, 0.5f * 9.81f * interceptTime * interceptTime, 0);
        futurePos -= gravityCompensation;  

        // rotate the player body to face the futurePos
        Vector3 lookDir = futurePos - transform.position;
        lookDir.y = 0; // Keep it level on the y-axis
        Vector3 lookDirAngle = Quaternion.LookRotation(lookDir).eulerAngles;
        //calc the time needed to animate the rotation, based on how far we need to turn (t = d/speed)
        float angleDist = Vector3.Angle(transform.forward, lookDir);
        float rotateTime = angleDist / (2 * 200);
        
        LeanTween.rotate(gameObject, lookDirAngle, rotateTime).setOnComplete(ShootAtEnemyBullet);        
    }

    void ShootAtEnemyBullet()
    {
        //rotate the gun?

        // shoot the projectile, attempt to intercept the incoming bullet        
        Vector3 shotDir = (futurePos + gravityCompensation - spawnpoint.position).normalized;
        float distToTarget = Vector3.Distance(futurePos, spawnpoint.position);
        float requiredSpeed = distToTarget / interceptTime;

        GameObject bullet; 
        bool playerHasAmmo = playerMasterScript.GetNextBullet(spawnpoint.transform.position, out bullet);
        if(playerHasAmmo)
        {
            Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
            bulletRb.AddForce(requiredSpeed * shotDir, ForceMode.Impulse);
        }
        else { Debug.Log("Shooting at enemy bullet -> No Ammo! "); }
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

    
    //Old Way, calculating the angle.  It's optimal, but the angle is too low...would like a shot with more height.
    void ShootAtSelected()
    {
        // assuming bullet starts and ends at same elevation (no height diff between origin and hitpoint)

        // get the required gun angle = 0.5 * arcsin(grav*distToTarget/launchVel^2)
        // the default setup seems to shoot just a little too far.  tweak with adjustAutoShootDist if needed.
        float distToTarget = Vector3.Distance(transform.position, hitPos);
        distToTarget += adjustAutoShootDist;
        float targetAngle = 0.5f * Mathf.Asin(((9.8f * distToTarget) / (launchVelocity*launchVelocity))) * Mathf.Rad2Deg;
        
        // set the required gun angle 
        Vector3 currentGunRot = gun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(targetAngle,currentGunRot.y,currentGunRot.z);
        gun.transform.eulerAngles = gunRot;

        // set the camera to align with the gun angle
        float camVertMin = cameraLookScript.lookUpLimit;
        float camVertMax = cameraLookScript.lookDownLimit;
        Vector3 currentCameraRot = mainCamera.transform.rotation.eulerAngles;
        float targetCameraXRot = MapValueToRange(gunRot.x, minAngle, maxAngle, camVertMin, camVertMax);
        Vector3 targetCameraRot = new Vector3(targetCameraXRot,currentCameraRot.y,currentCameraRot.z);
        
        //targetGunRot = Mathf.Clamp(targetGunRot, -32f, 0f);
        //Vector3 gunRot = new Vector3(-targetGunRot,currentGunRot.y,currentGunRot.z);
        mainCamera.transform.eulerAngles = targetCameraRot;

        // shoot in forward direction, at calculated angle
        bullet = assetManager.GetPlayerBullet(spawnpoint.transform.position);        
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(launchVelocity * spawnpoint.forward, ForceMode.Impulse);

        Debug.Log("Distance to target: " + distToTarget +  "        Target/Gun angle: " + gun.transform.eulerAngles.x);
        //Debug.Log("Spawn Point Pos: " + spawnpoint.transform.position +  "        Bullet Position: " + bullet.transform.position);
        Debug.Log("Target Camera Rot: " + mainCamera.transform.eulerAngles.x);
        //Time.timeScale = 0;

    }

    public static float MapValueToRange(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }

    
    */
}

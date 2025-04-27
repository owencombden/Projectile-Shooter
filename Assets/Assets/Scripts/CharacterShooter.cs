using UnityEngine;

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

    private float shootCooldown = 0.5f;
    private float lastShootTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetManager = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        // gun and spawnpoint must be children of the character
        gun = transform.Find("Gun");
        spawnpoint = transform.Find("Gun/SpawnPoint");
    }

    // Update is called once per frame
    void Update()
    { 
               
    }

    // target_obj param should be a Transform
    public void TryShoot(object target_obj)
    {   
        if (Time.time - lastShootTime < shootCooldown) return;
        lastShootTime = Time.time;        

        Transform target = (Transform) target_obj;  //LeanTween requires that this param be passed as an obj.  Cast back to Transform here.

        Debug.Log(transform.name + " is trying to shoot at " + target.name + " with tag: " + target.tag);

        if (target.tag == "Ground" || target.tag == "Player" || target.tag == "AI_Player") { ShootAtPosition(target.position); }

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
        GameObject bullet = AssetManager.Instance.GetBullet(spawnpoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = GetComponent<Targetable>().myId; 
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(calculatedLaunchVelocity * spawnpoint.forward, ForceMode.Impulse);

    }


    public void AimAtEnemyBullet(Transform target)
    {
        targetRB   = target.GetComponent<Rigidbody>();
        shotOrigin = target.GetComponent<Bullet>().startPos;

        // choose a time (from now) in which we want to intercept the bullet
        // far away targets = more time, closer targets = less time (close to immediate)
        // use the time to calculate where the enemey bullet will be, adjust the raw position by subtracting the gravity force
        // then calculate a shot, to that future position
        // the speed of our projectile will be determined by the intercept time (lower time, faster shot required)
        // we shoot in the direction of the future pos, but also account for gravity resulting in a shot that's a little higher than futurePos

        // map interceptTime to distance-to-target
        float currentDistToTarget = Vector3.Distance(transform.position, new Vector3(target.position.x, transform.position.y, target.position.z));
        float maxPossibleDist = Vector3.Distance(transform.position, shotOrigin);        

        interceptTime = MapValueToRange(currentDistToTarget, 1, maxPossibleDist, minInterceptTime, maxInterceptTime);

        Debug.Log("Current Distance to Target: "+currentDistToTarget+ "       Intercept Time is: " + interceptTime);
        
        // calc where the incoming projectile will be in interceptTime seconds
        futurePos = target.position + (targetRB.linearVelocity * interceptTime);
        gravityCompensation = new Vector3(0, 0.5f * 9.81f * interceptTime * interceptTime, 0);
        futurePos -= gravityCompensation;

        Debug.Log("Have intercept.  Target future position will be: " + futurePos);

        // if futurePos is out-of-play, cancel the shot
        if (futurePos.y < 2f || futurePos.y > 30f)
        {
            Debug.Log("Target future position is out of play.  cancelling shot");
            return;
        }  

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
        // shoot the projectile, attempt to intercept the incoming bullet        
        Vector3 shotDir = (futurePos + gravityCompensation - spawnpoint.position).normalized;
        float distToTarget = Vector3.Distance(futurePos, spawnpoint.position);
        float requiredSpeed = distToTarget / interceptTime;

        // cap velocity at 'maxSpeed'
        if (requiredSpeed > maxSpeed) { requiredSpeed = maxSpeed; }

        GameObject bullet = AssetManager.Instance.GetBullet(spawnpoint.position, Quaternion.identity);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        bulletScript.ownerId = GetComponent<Targetable>().myId; 
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(requiredSpeed * shotDir, ForceMode.Impulse);
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

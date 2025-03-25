using UnityEngine;

public class EnemyMove : MonoBehaviour
{
    public float speed;
    public float maxHeight = 10f;
    public float rotateSpeed;
    public Transform gun;

    Transform groundContainer;
    EnemyMaster enemyMasterScript;
    EnemyShoot enemyShootScript;
    AssetManager assetManagerScript;
    Transform player;
    Vector3 targetPosition;
    Vector3 currentVelocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyMasterScript = gameObject.GetComponent<EnemyMaster>();
        enemyShootScript = gameObject.GetComponent<EnemyShoot>();
        assetManagerScript = GameObject.Find("AssetManager").GetComponent<AssetManager>();

        player = assetManagerScript.GetPlayer().transform;

        Vector3 rayStart = transform.position;
        Vector3 rayDir = transform.up * -1;
        Ray ray = new Ray(rayStart, rayDir);        
        float rayLength = 10f;
        RaycastHit hitData;
        if (Physics.Raycast(ray, out hitData, rayLength))
        {            
            string tag = hitData.collider.tag;
            if(tag == "Ground")
            {
                //we hit a tile, get the parent container
                groundContainer = hitData.transform.parent;
            }
        }
        else
        {
            Debug.Log(gameObject.name + " could not find the ground!");
        }          

    }

    // Update is called once per frame
    void Update()
    {
        if (enemyMasterScript.enemyDead) 
        {            
            return;
        }
        
        
        Vector3 rayStart = transform.position;
        Vector3 rayDir = transform.up * -1;
        Ray ray = new Ray(rayStart, rayDir);        
        float rayLength = 10f;
        //Debug.DrawRay(ray.origin, ray.direction.normalized * rayLength, Color.red);
        
        RaycastHit hitData;
        if (Physics.Raycast(ray, out hitData, rayLength))
        {   
            string tag = hitData.collider.tag;
            if(tag == "Water")
            {
                //enemy has fallen in the water
                Debug.Log(gameObject.name + " has fallen in the water");
                enemyMasterScript.KillEnemy(hitData.point, Vector3.Cross(currentVelocity, transform.up));
                return;
            }
            else if(tag == "Ground")
            {
                // if we're already moving/looking somewhere, skip
                if (gameObject.LeanIsTweening()){ return; }

                targetPosition = groundContainer.GetChild(Random.Range(0, groundContainer.childCount)).position;
                targetPosition.y = transform.position.y;
                Vector3 lookDir = targetPosition - transform.position;      
                Vector3 lookRot = Quaternion.LookRotation(lookDir, Vector3.up).eulerAngles;
                float angleDist = Vector3.Angle(transform.forward, lookDir);
                float rotateTime = angleDist / rotateSpeed;
                LeanTween.rotate(gameObject, lookRot, rotateTime).setDelay(2f).setOnComplete(MoveEnemy);
                //Debug.Log("Enemy is looking at next destination...");                
            }
            else
            {
                Debug.Log("Unknown tag!");
            }
        }
    }

    void MoveEnemy()
    {
        currentVelocity = (targetPosition - transform.position).normalized;
        float distToTarget = Vector3.Distance(transform.position, targetPosition);
        float tweenTime = distToTarget / speed;
        LeanTween.move(gameObject, targetPosition, tweenTime).setOnComplete(LookAtPlayer);
        //Debug.Log("Enemy is moving..."); 
    }

    void LookAtPlayer()
    {
        //Debug.Log("Enemy is looking at player..."); 
        Vector3 lookDir = player.position - transform.position;      
        Vector3 lookRot = Quaternion.LookRotation(lookDir, Vector3.up).eulerAngles;
        float angleDist = Vector3.Angle(transform.forward, lookDir);
        float rotateTime = angleDist / rotateSpeed;
        LeanTween.rotate(gameObject, lookRot, rotateTime).setOnComplete(RotateGun);
        //Debug.Log("Rotate time is: " + rotateTime);
    }

    void RotateGun()
    {
        // get the required gun angle
        Vector3 targetPosition = player.transform.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f));
        float distToTarget = Vector3.Distance(transform.position, targetPosition);
        float targetAngle = Mathf.Atan(4 * maxHeight/distToTarget) * Mathf.Rad2Deg;
        
        // set the required gun angle 
        Vector3 currentGunRot = gun.transform.rotation.eulerAngles;
        Vector3 gunRot = new Vector3(targetAngle,currentGunRot.y,currentGunRot.z);
        
        // get the required velocity, at that angle, to reach maxHeight
        // rotate the gun, then shoot        
        enemyShootScript.SetLaunchVelocity(CalculateLaunchVelocity(distToTarget, maxHeight));
        LeanTween.value(gameObject, _SetGunRotation, gun.transform.eulerAngles, gunRot, 1f).setOnComplete(enemyShootScript.ShootAtPlayer);
    }

    private static float CalculateLaunchVelocity(float distance, float maxHeight)
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

    private void _SetGunRotation(Vector3 tweenedRotationVector)
    {
        gun.transform.eulerAngles = tweenedRotationVector;
    }

}

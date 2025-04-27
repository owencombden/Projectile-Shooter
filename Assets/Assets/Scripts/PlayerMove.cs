using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    /*
    public CharacterController controller;
    public Transform cameraTransform;
    public float mouseSensitivity = 200f;
    public float movementSpeed;
    public float movementSmooth = 0.1f;
    public float rotationSpeed;
    public float autorotateSpeed = 200f;
    public float rotationSmooth = 5f;
    public float turnSmoothTime = 0.1f;
    public float edgeAvoidance = 1f;    

    
    private float turnSmoothVelocity;

    PlayerMaster playerMasterScript;
    PlayerShoot playerShootScript;
    Vector3 movementDirection;
    private float currentRotationY;
    private float targetRotationY;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraTransform = GameObject.FindWithTag("MainCamera").transform;
        controller = gameObject.GetComponent<CharacterController>();
        playerMasterScript = gameObject.GetComponent<PlayerMaster>();
        playerShootScript = gameObject.GetComponent<PlayerShoot>();
    }

    // Update is called once per frame
    void Update()
    {        
        
    }
    
    public void MovePlayer(Vector2 input)
    {        
        float horizontal = input.x;
        float vertical = input.y;
        
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        controller.Move(moveDir.normalized * movementSpeed * Time.deltaTime);
    }    

    public void RotatePlayerWithInput(Vector2 input)
    {        
        float lookX = input.x;
        // Accumulate target rotation and smooth between current and target rotation
        targetRotationY += lookX * rotationSpeed;        
        currentRotationY = Mathf.Lerp(currentRotationY, targetRotationY, rotationSmooth * Time.deltaTime);        
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }

    public void RotatePlayerToTarget(RaycastHit hit)
    {
        //rotate the player to look at eye-level above the hitpoint
        Vector3 adjustedHeightHitPoint = new Vector3(hit.point.x, transform.position.y, hit.point.z);
        Vector3 lookDir = Quaternion.LookRotation(adjustedHeightHitPoint-transform.position).eulerAngles;
        
        //calc the time needed to animate the rotation, based on how far we need to turn (t = d/speed)
        Vector3 targetLookDir = adjustedHeightHitPoint - transform.position;
        float angleDist = Vector3.Angle(transform.forward, targetLookDir);
        float rotateTime = angleDist / autorotateSpeed;
        
        if(hit.transform.tag == "EnemyBullet")
        {            
            currentRotationY = lookDir.y; // these will now be used by RotatePlayer() when tween is finished
            targetRotationY = lookDir.y;
            LeanTween.rotate(gameObject, lookDir, rotateTime).setOnComplete(playerShootScript.AimAtEnemyBullet, hit);
        }

        if (hit.transform.tag == "Ground")
        {
            currentRotationY = lookDir.y; // these will now be used by RotatePlayer() when tween is finished
            targetRotationY = lookDir.y;
            LeanTween.rotate(gameObject, lookDir, rotateTime).setOnComplete(playerShootScript.ShootAtGround, hit);
        }
    }

    */
     
}

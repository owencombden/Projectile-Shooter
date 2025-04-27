using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private Transform cameraTransform; // Set this for player only

    private CharacterController controller;
    private CharacterShooter shooter;
    private float turnSmoothVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        shooter = GetComponent<CharacterShooter>();
    }

    // Called for Player input
    public void Move(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f) return;

        Vector3 direction = new Vector3(input.x, 0f, input.y).normalized;
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        if (cameraTransform != null)
        {
            targetAngle += cameraTransform.eulerAngles.y;
        }

        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);

        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        controller.Move(moveDir * moveSpeed * Time.deltaTime);
    }

    // Called for AI movement
    public void MoveTo(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.magnitude < 0.1f) return;

        Vector3 move = direction.normalized * moveSpeed * Time.deltaTime;
        controller.Move(move);

        RotateToward(destination);
    }

    public void RotateToward(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    public void RotateToTargetAndShoot(Transform target)
    {
        //rotate the player to look at eye-level towards the target
        Vector3 lookAt = new Vector3(target.position.x, transform.position.y, target.position.z);
        Vector3 lookDir = Quaternion.LookRotation(lookAt-transform.position).eulerAngles;
        
        //calc the time needed to animate the rotation, based on how far we need to turn (t = d/speed)
        Vector3 targetLookDir = lookAt - transform.position;
        float angleDist = Vector3.Angle(transform.forward, targetLookDir);
        float rotateTime = angleDist / rotateSpeed * 0.5f;

        LeanTween.rotate(gameObject, lookDir, rotateTime).setOnComplete(shooter.TryShoot, target);        
    }

    //called at Start from the PlayerController script (only needed for player)
    public void SetCamera(Transform cam)
    {
        cameraTransform = cam;
    }

    public Vector3 GetVelocity(bool normalized = true)
    {
        if (normalized) { return controller.velocity.normalized;}
        else            { return controller.velocity;}
        
    }
}
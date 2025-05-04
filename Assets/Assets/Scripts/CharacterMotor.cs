using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 360f;
    [SerializeField] private float aimAngleThreshold = 0.5f;
    [SerializeField] private float maxAimTime = 1f;
    [SerializeField] private float facingOverrideDuration = 1f;
    [SerializeField] private Transform cameraTransform; // Set this for player only

    private CharacterController controller;
    private CharacterShooter shooter;
    private float turnSmoothVelocity;
    private Vector3? desiredFacing = null;
    private float facingOverrideTimer = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        shooter = GetComponent<CharacterShooter>();
    }

    private void Update()
    {
        HandleRotation();   
    }

    private void HandleRotation()
    {
        if (desiredFacing.HasValue)
        {
            Vector3 faceDir = desiredFacing.Value;
            faceDir.y = 0f;
            if (faceDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(faceDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }

        if (facingOverrideTimer > 0f)
        {
            facingOverrideTimer -= Time.deltaTime;
        }
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

        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        // Update desired facing direction unless we're overriding it for shooting
        if (facingOverrideTimer <= 0f)
        {
            desiredFacing = moveDir;
        }
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
        
        // Optional: update facing direction to match current rotation
        desiredFacing = direction.normalized;
    }

    public IEnumerator RotateTowardTargetAndShoot(Transform target)
    {
        if (target == null) yield break;

        Vector3 targetDir = target.position - transform.position;
        targetDir.y = 0f;

        if (targetDir == Vector3.zero) yield break;

        Vector3 targetFacing = targetDir.normalized;
        desiredFacing = targetFacing;
        facingOverrideTimer = facingOverrideDuration;

                
        float elapsed = 0f;

        while (elapsed < maxAimTime)
        {
            float angle = Vector3.Angle(transform.forward, targetFacing);

            if (angle < aimAngleThreshold)
            {
                shooter.TryShoot(target);
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        desiredFacing = null;
        facingOverrideTimer  = 0f;
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
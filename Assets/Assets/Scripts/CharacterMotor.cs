using UnityEngine;
using System.Collections;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class CharacterMotor : NetworkBehaviour
{
    public bool isPlayer;
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 720f;
    [SerializeField] private float aimAngleThreshold = 0.5f;
    [SerializeField] private float maxAimTime = 1f;
    [SerializeField] private float facingOverrideDuration = 1f;
    [SerializeField] private Transform cameraTransform; // Set this for player only

    private CharacterController controller;
    private Vector3? desiredFacing = null;
    private float facingOverrideTimer = 0f;
    private Vector3 currentVelocity = Vector3.zero;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
        return;
        
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

   
    // for Player movement    
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
    

    // for AI movement
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

    public IEnumerator RotateTowardTarget(Transform target)
    {
        // this should only be called by owner! See PlayerController

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
                // body has been rotated
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

    public void ApplyBlastForce(Vector3 direction, float force, float duration, bool isHuman)
    {
        StartCoroutine(BlastForceCoroutine(direction, force, duration, isHuman));
    }

    private IEnumerator BlastForceCoroutine(Vector3 direction, float force, float duration, bool isHuman)
    {
        direction.y = 0f;
        Vector3 blastVelocity = direction.normalized * force;
        float time = 0f;

        while (time < duration)
        {
            controller.Move(blastVelocity * Time.deltaTime);
            blastVelocity = Vector3.Lerp(blastVelocity, Vector3.zero, time / duration); // ease out
            time += Time.deltaTime;

            CheckIfWater(isHuman);

            yield return null;
        }

        if (isHuman)
        {
            transform.GetComponent<PlayerController>().isBeingKnockedBack = false;
        }
        else
        {
            transform.GetComponent<AIController>().isBeingKnockedBack = false;
        }        
    }

    private void CheckIfWater(bool isHuman)
    {
        Vector3 rayStart = transform.position;
        float rayRadius = 0.1f;
        Vector3 rayDir = Vector3.down;
        float rayLength = 5f;
        RaycastHit hitData;
        if(Physics.SphereCast(rayStart, rayRadius, rayDir, out hitData, rayLength))
        {
            if(hitData.transform.tag != null && hitData.transform.tag == "Water")
            {
                if (isHuman)
                {
                    transform.GetComponent<PlayerController>().KillPlayer(hitData.point, Vector3.Cross(GetVelocity(false), transform.up));
                }
                else
                {
                    transform.GetComponent<AIController>().KillEnemy(hitData.point, Vector3.Cross(GetVelocity(false), transform.up));
                }

            }
        }
        
    }
}
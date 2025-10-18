using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [Header("Follow Settings")]
    public Vector3 cameraOffset;
    public float cameraMinHeight = 1.5f;
    public float cameraRotateSpeed = .75f;
    public float cameraMoveSmooth = 0.1f;
    public float cameraRotateSmooth = 0.1f;
    public float cameraPitchSmooth = 0.1f;
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("Shake Settings")]
    public float shakeDuration = 0f;
    public float shakeIntensity = 0.1f;

    private Transform player;
    private Transform cameraLookHere;
    private Vector2 lookInput; //this is delivered to the camera by PlayerInputHandler
    private float currentYaw;
    private float currentPitch;
    private float shakeTimer;
    private Vector3 shakeOffset = Vector3.zero;

    public void SetLookInput(Vector2 lookDelta)
    {
        lookInput = lookDelta;  // we are setting look-sensitivity in PlayerInputHandler.Update()
        //Debug.Log($"Camera Look input is: ({lookInput})");
    }

    public void SetTarget(Transform playerTransform, Transform lookPoint)
    {
        player = playerTransform;
        cameraLookHere = lookPoint;

        // Align camera behind the player and facing forward
        Vector3 forward = player.forward;
        currentYaw = Quaternion.LookRotation(forward).eulerAngles.y;
        currentPitch = 10f;
    }

    void LateUpdate()
    {
        if (player == null || cameraLookHere == null) return;

        // Handle orbit input
        if (lookInput.sqrMagnitude > 0.01f)
        {
            currentYaw += lookInput.x * cameraRotateSpeed;
            currentPitch -= lookInput.y * cameraRotateSpeed;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // Calculate rotation and position
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = cameraLookHere.position + rotation * cameraOffset;

        // Smooth movement
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, cameraMoveSmooth);

        // Apply shake if active
        if (shakeTimer > 0f)
        {
            shakeOffset = Random.insideUnitSphere * shakeIntensity;
            shakeTimer -= Time.deltaTime;

            if (shakeTimer <= 0f)
            {
                shakeTimer = 0f;
                shakeOffset = Vector3.zero;
            }
        }
        smoothedPosition += shakeOffset;

        // clamp vertical position so camera never goes below minHeight
        if (smoothedPosition.y < cameraMinHeight)
            smoothedPosition.y = cameraMinHeight;

        transform.position = smoothedPosition;

        // Always look at the target
        transform.LookAt(cameraLookHere);
    }

    
    public void Shake(float intensity, float duration)
    {
        shakeIntensity = intensity;
        shakeDuration = duration;
        shakeTimer = duration;
    }
}

using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [Header("Follow Settings")]
    public Vector3 cameraOffset;
    public float cameraRotateSpeed = 2f;
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
    private float currentYaw;
    private float currentPitch;
    private float shakeTimer;
    private Vector3 shakeOffset = Vector3.zero;

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
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw += mouseX * cameraRotateSpeed;
            currentPitch -= mouseY * cameraRotateSpeed;
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

        transform.position = smoothedPosition + shakeOffset;

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

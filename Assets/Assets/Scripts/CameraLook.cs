using UnityEngine;

public class CameraLook : MonoBehaviour
{
    public Vector3 cameraOffset;
    public float cameraRotateSpeed = 2f;
    public float cameraMoveSmooth = 0.1f;
    public float cameraRotateSmooth = 0.1f;
    public float cameraPitchSmooth = 0.1f;
    public float minPitch = -30f;
    public float maxPitch = 60f;  

    private Transform player;
    private Transform cameraLookHere;
    private float currentYaw;
    private float currentPitch;

    public void SetTarget(Transform playerTransform, Transform lookPoint)
    {
        player = playerTransform;
        cameraLookHere = lookPoint;
    }

    void LateUpdate()
    {
        if (player == null || cameraLookHere == null) return;

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw += mouseX * cameraRotateSpeed;
            currentPitch -= mouseY * cameraRotateSpeed;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = cameraLookHere.position + rotation * cameraOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, cameraMoveSmooth);
        transform.position = smoothedPosition;

        transform.LookAt(cameraLookHere);
    }
}

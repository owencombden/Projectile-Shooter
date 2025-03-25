using UnityEngine;

public class CameraLook : MonoBehaviour
{
    public Transform cameraLookHere; // a child of the player obj
    public Vector3 cameraOffset;
    public float cameraRotateSpeed = 2f;
    public float cameraMoveSmooth = 0.1f;
    public float cameraRotateSmooth = 0.1f;
    public float cameraPitchSmooth = 0.1f;
    public float minPitch = -30f;
    public float maxPitch = 60f;  
    
    private AssetManager assetManagerScript;
    PlayerShoot playerShootScript;
    private Transform player;
    private float currentYaw;
    private float currentPitch;
    
    

    void Start()
    {
        assetManagerScript = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        
        player = assetManagerScript.GetPlayer().transform;
        playerShootScript = player.GetComponent<PlayerShoot>();        
    }

    void LateUpdate()
    {
       if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            
            currentYaw += mouseX * cameraRotateSpeed;
            currentPitch -= mouseY * cameraRotateSpeed;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

            float currentCameraX = transform.rotation.eulerAngles.x;
            playerShootScript.UpdateGunAngle(currentCameraX);
        }

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 desiredPosition = cameraLookHere.position + rotation * cameraOffset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, cameraMoveSmooth);
        transform.position = smoothedPosition;

        transform.LookAt(cameraLookHere);        
    }

}

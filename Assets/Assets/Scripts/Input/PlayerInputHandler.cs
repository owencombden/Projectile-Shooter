using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.InputSystem.EnhancedTouch;


public class PlayerInputHandler : NetworkBehaviour, ICharacterInputProvider
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    private bool lookEnabled; // <- Tracks RMB state


    public event Action OnShootClicked; // <- new clean event

    private PlayerInput playerInput;
    private CameraLook cameraLookScript;



    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        cameraLookScript = Camera.main.GetComponent<CameraLook>();

        EnhancedTouchSupport.Enable();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            if (playerInput != null)
                playerInput.enabled = false;
        }
        else
        {
            if (playerInput != null)
            {
                playerInput.ActivateInput();

                // FORCE TOUCH CONTROLS
                //playerInput.SwitchCurrentControlScheme("Touch", Touchscreen.current, Mouse.current);
            }
        }
    }

    void Update()
    {
        foreach (var t in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
                Debug.Log($"[TOUCH DETECTED] id={t.touchId} pos={t.screenPosition}");
        }
    }


    // ---- Input Action Callbacks ----
    public void OnMove(InputAction.CallbackContext context)
    {
        Debug.Log($"OnMove is updating MoveInput.");
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {

        if (lookEnabled)  // if RMB is held (need to add 'or right joystick is active' here for mobile)
        {
            LookInput = context.ReadValue<Vector2>();
            cameraLookScript.SetLookInput(LookInput);
        }
        else
            LookInput = Vector2.zero;
    }

    public void OnLookEnable(InputAction.CallbackContext context)
    {
        if (context.performed) lookEnabled = true;
        else if (context.canceled) lookEnabled = false;
    }

    public void OnShootTarget(InputAction.CallbackContext context)
    {
        Debug.Log($"OnShootTarget is attempting a shot.");
        if (context.performed)
            OnShootClicked?.Invoke();
    }


    public Transform GetPointerTarget()
    {
        Debug.Log($"Getting pointer target.");

        // currently tracking mouse and touch
        Vector2 screenPos;
        if (Touchscreen.current != null)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else
        {
            screenPos = Mouse.current.position.ReadValue();
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform;
        }

        Debug.LogWarning($"Pointer target not found!!");
        return null;
    }
}

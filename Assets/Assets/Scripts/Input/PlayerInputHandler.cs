using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.InputSystem.EnhancedTouch;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : NetworkBehaviour, ICharacterInputProvider
{
    private PlayerInput playerInput;
    private Camera playerCamera;
    private CameraLook cameraLookScript;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool shootAtTarget;

    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    public bool ShootAtTarget => shootAtTarget;

    [SerializeField] private RectTransform movementJoystickRect;
    [SerializeField] private RectTransform lookJoystickRect;

    float aimAssistRadius = 70f; // screen-space pixels
    public event Action<Transform> OnShootClicked;
    Vector3 lastShootScreenPos = Vector3.zero;



    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerCamera = Camera.main;
        cameraLookScript = playerCamera.GetComponent<CameraLook>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        playerInput = GetComponent<PlayerInput>();

        if (!IsOwner)
        {
            if (playerInput != null)
                playerInput.enabled = false;
            return;
        }

        if (playerInput != null)
        {
            // Find actions by name (must match your InputActionAsset)
            var moveAction = playerInput.actions["Movement"];
            //var lookAction = playerInput.actions["Look"];

            // --- Move ---
            moveAction.performed += ctx =>
            {
                moveInput = ctx.ReadValue<Vector2>();
            };
            moveAction.canceled += ctx =>
            {
                moveInput = Vector2.zero;
            };

            // --- Look ---
            //lookAction.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
            //lookAction.canceled += ctx => lookInput = Vector2.zero;

            // --- Shoot ---
            // moved this to Update for polling taps.

            EnhancedTouchSupport.Enable();
            playerInput.ActivateInput();
        }
    }

    void Start()
    {
        //get the joystick rects (for filtering shot attempts)
        movementJoystickRect = GameObject.FindGameObjectWithTag("MoveStick").GetComponent<RectTransform>();
        lookJoystickRect = GameObject.FindGameObjectWithTag("LookStick").GetComponent<RectTransform>();
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseShoot();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchShoot();
#endif

    }

    void HandleMouseShoot()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();

            // if the click is over UI, ignore it.
            // can add other UI elements here (pause, settings, etc)
            if (IsOverJoystick(screenPos)) return;
            
            // get the clicked target
            Transform clickedTarget = null;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                clickedTarget = hit.transform;
            }

            if (clickedTarget && clickedTarget.tag == "Ground")
            {
                OnShootClicked?.Invoke(clickedTarget);
            }
            else
            {
                // shooting at something other than 'ground'.  use aim assist
                // get the closest targetPos (within radius) to the click/tap position
                Transform closestTarget = GetClosestTarget(screenPos, aimAssistRadius);
                if (closestTarget != null)
                {
                    OnShootClicked?.Invoke(closestTarget);
                }
                
            }
        }
    }

    void HandleTouchShoot()
    {
        foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 screenPos = touch.screenPosition;

                // filter out touches we don't care about (can add other UI buttons here)
                if (IsOverJoystick(screenPos)) return;

                // get the tapped target
                Transform tappedTarget = null;
                Ray ray = Camera.main.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    tappedTarget = hit.transform;
                }

                if (tappedTarget && tappedTarget.tag == "Ground")
                {
                    OnShootClicked?.Invoke(tappedTarget);
                }
                else
                {
                    // shooting at something other than 'ground'.  use aim assist
                    // get the closest targetPos (within radius) to the click/tap position
                    Transform closestTarget = GetClosestTarget(screenPos, aimAssistRadius);
                    if (closestTarget != null)
                    {
                        OnShootClicked?.Invoke(closestTarget);
                        break;
                    }                    
                }
            }
        }
    }

    // helper: check if screen position overlaps a joystick rect
    bool IsOverJoystick(Vector2 screenPos)
    {
        // Check if point is inside either joystick
        if (RectTransformUtility.RectangleContainsScreenPoint(movementJoystickRect, screenPos, null))
        {
            return true;
        }
        if (RectTransformUtility.RectangleContainsScreenPoint(lookJoystickRect, screenPos, null))
        {
            return true;
        }

        return false;
    }
    
    public Transform GetClosestTarget(Vector2 screenPos, float radius)
    {
        List<Targetable> targets = TargetManager.Instance.GetTargets();
        Transform bestTarget = null;
        float bestDist = Mathf.Infinity;

        foreach (var t in targets)
        {
            // World -> Screen
            Vector3 screenPoint = playerCamera.WorldToScreenPoint(t.transform.position);
            //Debug.Log($"------------------------------------------");
            //Debug.Log($"Calculating for {t.transform.name} with WorldToScreenPoint {screenPoint}");
            //Debug.Log($"Distance from tap is {Vector2.Distance(screenPos, screenPoint)}");


            // Ignore things behind the camera
            if (screenPoint.z < 0) continue;

            float dist = Vector2.Distance(screenPos, screenPoint);

            if (dist < radius && dist < bestDist)
            {
                bestDist = dist;
                bestTarget = t.transform;
            }
        }

        return bestTarget;
    }
}





/*
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : NetworkBehaviour, ICharacterInputProvider
{
    // Backing fields for interface
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool shootAtTarget;

    // ICharacterInputProvider properties
    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    public bool ShootAtTarget => shootAtTarget;

    private PlayerInput playerInput;
    private CameraLook cameraLookScript;

    public event Action OnShootClicked; // <- new clean event

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        cameraLookScript = Camera.main.GetComponent<CameraLook>();
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
            }
        }
    }

    public Transform GetPointerTarget()
    {
        Debug.Log("Getting pointer target...FIX THIS!!");
        return null;
    }

    // --- Input System Callbacks ---
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        // get this working, see code below.   Action and callbacks

        Debug.Log("Shoot detected.");

        if (context.performed)
        {
            shootAtTarget = true;
            OnShootClicked?.Invoke();
        }
        else if (context.canceled)
            shootAtTarget = false;
    }
}

*/




/*  OLD INPUT SYSTEM BELOW

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.InputSystem.EnhancedTouch;


public class PlayerInputHandler : NetworkBehaviour, ICharacterInputProvider
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool ShootAtTarget { get; private set; }

    private bool lookEnabled; // <- Tracks RMB state

    private Vector2 lastShootScreenPos;

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
            }
        }
    }

    void Update()
    {
        // Debugging touch on mobile.
        //foreach (var t in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
        //{
        //    if (t.phase == UnityEngine.InputSystem.TouchPhase.Began)
        //        Debug.Log($"[TOUCH DETECTED] id={t.touchId} pos={t.screenPosition}");
        //}
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
        Debug.Log($"OnShootTarget is attempting a shot. Context is {context.ToString()}");
        // Use mouse if available, otherwise use current touch position
        if (Mouse.current != null && Touchscreen.current == null)
        {
            Debug.Log($"Found a mouse.  Setting lastShootScreenPos to {Mouse.current.position.ReadValue()}");
            lastShootScreenPos = Mouse.current.position.ReadValue();
        }
        else if (Touchscreen.current != null)
        {
            Debug.Log($"Touchscreen.current is detecting {Touchscreen.current.touches.Count} touches.");
            // Take the FIRST active touch (not necessarily primaryTouch)
            foreach (var ctrl in Touchscreen.current.touches)
            {
                if (ctrl.press.isPressed)
                {
                    Debug.Log($"{ctrl.press.name} is pressed.  Storing lastShootScreenPos as {ctrl.position.ReadValue()}");
                    // store the position of the tap which requested the shot
                    lastShootScreenPos = ctrl.position.ReadValue();
                    break;
                }
            }
        }

        OnShootClicked?.Invoke();
    }


    public Transform GetPointerTarget()
    {
        Debug.Log($"Getting pointer target.");

        Ray ray = Camera.main.ScreenPointToRay(lastShootScreenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.transform;
        }

        Debug.LogWarning("Pointer target not found!!");
        return null;
    }
}

*/

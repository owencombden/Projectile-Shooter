using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.Netcode;
using UnityEngine.InputSystem.EnhancedTouch;
using Empress.UITK;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : NetworkBehaviour, ICharacterInputProvider
{
    private PlayerInput playerInput;
    private Camera playerCamera;
    private CameraLook cameraLookScript;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float lookSensitivity = 1f;
    private bool mouseLookActive = false;
    private bool shootAtTarget;

    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    public bool ShootAtTarget => shootAtTarget;

    [SerializeField] private VirtuaStickProcedural movementJoystick;
    [SerializeField] private VirtuaStickProcedural lookJoystick;

    float aimAssistRadius = 120f; // screen-space pixels
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
            // turn on/off the mobile UI controls depending on the build 
            GameObject mobileControls = GameObject.FindGameObjectWithTag("MobileControls");
#if UNITY_EDITOR || UNITY_STANDALONE
            mobileControls.SetActive(false);
#elif UNITY_ANDROID || UNITY_IOS
            mobileControls.SetActive(true);
            movementJoystick = GameObject.FindGameObjectWithTag("MoveStick").GetComponent<VirtuaStickProcedural>();
            lookJoystick     = GameObject.FindGameObjectWithTag("LookStick").GetComponent<VirtuaStickProcedural>();
#endif

            // Find actions by name (must match the InputAction project settings)
            var moveAction = playerInput.actions["Movement"];
            var lookAction = playerInput.actions["Look"];
            var activateMouseLookAction = playerInput.actions["ActivateMouseLook"];

            // setup callbacks, see helpers below.  Triggered by the actions above
            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
            lookAction.performed += OnLookPerformed;
            lookAction.canceled += OnLookCanceled;
            activateMouseLookAction.performed += OnMouseLookPerformed;
            activateMouseLookAction.canceled += OnMouseLookCanceled;

            // Shooting has been moved to Update (polling taps)

            // finish setting up
            SetLookSensitivity();
            EnhancedTouchSupport.Enable();
            playerInput.ActivateInput();
        }
    }

    // callback helpers
    void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;
    void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        if (mouseLookActive || (movementJoystick && lookJoystick))
            lookInput = ctx.ReadValue<Vector2>();
        else
            lookInput = Vector2.zero;
    }
    void OnLookCanceled(InputAction.CallbackContext ctx) => lookInput = Vector2.zero;
    void OnMouseLookPerformed(InputAction.CallbackContext ctx) => mouseLookActive = true;
    void OnMouseLookCanceled(InputAction.CallbackContext ctx) => mouseLookActive = false;


    void Update()
    {
        if (!IsOwner) return;

        //LOOK (mouse or right-joystick)    
        if (cameraLookScript != null)
        {            
            Vector2 look = lookInput * lookSensitivity;
            cameraLookScript.SetLookInput(look);
        }

        // SHOOT (mouse or tap-screen)
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseShoot();
#elif UNITY_ANDROID || UNITY_IOS
        HandleTouchShoot();
#endif
    }

    void SetLookSensitivity()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        // Mouse delta is usually high-frequency, so scale it down
        lookSensitivity = 0.1f;
#elif UNITY_ANDROID || UNITY_IOS
        // Joystick gives small values (0–1), scale them up
        lookSensitivity = 1.5f;
#endif        
    }

    void HandleMouseShoot()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 screenPos = Mouse.current.position.ReadValue();

            // if the click is over UI, ignore it.
            // can add other UI elements here (pause, settings, etc)
            //if (IsOverJoystick(screenPos)) return;

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
        // Calculate screen size
        float screenW = Screen.width;
        float screenH = Screen.height;

        // --- Move Joystick Area (bottom-left corner) ---
        float moveWidth = screenW * (movementJoystick.stickArea.x / 100f);
        float moveHeight = screenH * (movementJoystick.stickArea.y / 100f);
        Rect moveRect = new Rect(0f, 0f, moveWidth, moveHeight); // origin = bottom-left

        // --- Look Joystick Area (bottom-right corner) ---
        float lookWidth = screenW * (lookJoystick.stickArea.x / 100f);
        float lookHeight = screenH * (lookJoystick.stickArea.y / 100f);
        Rect lookRect = new Rect(screenW - lookWidth, 0f, lookWidth, lookHeight); // origin = bottom-right

        // --- Check if point lies within either rect ---
        return moveRect.Contains(screenPos) || lookRect.Contains(screenPos);
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

    private new void OnDestroy()
    {
        if (!IsOwner) return;

        if (playerInput != null)
        {
            var moveAction = playerInput.actions["Movement"];
            var lookAction = playerInput.actions["Look"];
            var activateMouseLookAction = playerInput.actions["ActivateMouseLook"];

            // Unsubscribe to prevent memory leaks or duplicate callbacks
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            lookAction.performed -= OnLookPerformed;
            lookAction.canceled -= OnLookCanceled;
            activateMouseLookAction.performed -= OnMouseLookPerformed;
            activateMouseLookAction.canceled -= OnMouseLookCanceled;
        }

        // Disable touch system
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();

        // Clear event subscribers (optional but safe)
        OnShootClicked = null;
    }

}


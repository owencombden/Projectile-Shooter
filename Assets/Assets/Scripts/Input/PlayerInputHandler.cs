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

    float aimAssistRadius = 100f; // screen-space pixels
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


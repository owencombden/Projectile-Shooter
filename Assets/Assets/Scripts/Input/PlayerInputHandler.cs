using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using Unity.Netcode;

public class PlayerInputHandler : NetworkBehaviour, ICharacterInputProvider
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool ShootAtTarget { get; private set; }

    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
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
                var user = playerInput.user;

                // Unpair previous devices, just in case
                InputUser.PerformPairingWithDevice(Keyboard.current, user);
                InputUser.PerformPairingWithDevice(Mouse.current, user);

                // Activate the input
                playerInput.ActivateInput();
            }
        }
    }
    public void OnMove(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => LookInput = context.ReadValue<Vector2>();

    public void OnShootTarget(InputAction.CallbackContext context)
    {
        if (context.started)
            ShootAtTarget = true;
        else if (context.canceled)
            ShootAtTarget = false;
    }
}

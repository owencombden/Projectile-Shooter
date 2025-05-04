using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour, ICharacterInputProvider
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool ShootAtTarget { get; private set; }

    public void OnMove(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => LookInput = context.ReadValue<Vector2>();
    public void OnShootTarget(InputAction.CallbackContext context)
    {
        if (context.started)  // only when button is first pressed down
            ShootAtTarget = true;
        else if (context.canceled)
            ShootAtTarget = false;
    }    
}
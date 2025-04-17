using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool ShootFromTheHip { get; private set; }
    public bool ShootAtTarget { get; private set; }

    public void OnMove(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();
    public void OnLook(InputAction.CallbackContext context) => LookInput = context.ReadValue<Vector2>();    
    
    public void OnShootHip(InputAction.CallbackContext context)
    {
        if (context.started)  // only when button is first pressed down
            ShootFromTheHip = true;
        else if (context.canceled)
            ShootFromTheHip = false;
    }
    public void ResetShootHip() => ShootFromTheHip = false;

    public void OnShootTarget(InputAction.CallbackContext context)
    {
        if (context.started)  // only when button is first pressed down
            ShootAtTarget = true;
        else if (context.canceled)
            ShootAtTarget = false;
    }
    public void ResetShootTarget() => ShootAtTarget = false;

    
}



/*
public class PlayerInputHandler : MonoBehaviour
{
    private IPlayerInput inputMethod;

    private void Awake()
    {
        // Swap between input methods dynamically
        if (IsGamepadConnected())
            inputMethod = gameObject.AddComponent<GamepadInputHandler>();
        else
            inputMethod = gameObject.AddComponent<KeyboardInputHandler>();
    }

    public Vector2 GetMovementInput()     => inputMethod.GetMovementInput();
    public Vector2 GetLookInput()         => inputMethod.GetLookInput();
    public bool GetShootFromTheHipInput() => inputMethod.GetShootFromTheHipInput();
    public bool GetShootAtTargetInput()   => inputMethod.GetShootAtTargetInput();

    private bool IsGamepadConnected()
    {
        return Input.GetJoystickNames().Length > 0 && !string.IsNullOrEmpty(Input.GetJoystickNames()[0]);
    }
}

public interface IPlayerInput
{
    Vector2 GetMovementInput();   // Returns movement input (X = strafe, Y = forward)
    Vector2 GetLookInput();       // Returns camera look input
    bool GetShootFromTheHipInput();          // Returns jump button state
    bool GetShootAtTargetInput();         // Returns shooting state
}

public class KeyboardInputHandler : MonoBehaviour, IPlayerInput
{
    public Vector2 GetMovementInput()
    {
        float x = Input.GetAxis("Horizontal");  // A/D or Left/Right Arrow
        float y = Input.GetAxis("Vertical");    // W/S or Up/Down Arrow
        return new Vector2(x, y);
    }

    public Vector2 GetLookInput()
    {
        float x = Input.GetAxis("Mouse X");   // Mouse movement X
        float y = Input.GetAxis("Mouse Y");   // Mouse movement Y
        return new Vector2(x, y);
    }

    public bool GetShootFromTheHipInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool GetShootAtTargetInput()
    {
        return Input.GetMouseButtonDown(0); // Left-click
    }
}

public class GamepadInputHandler : MonoBehaviour, IPlayerInput
{
    public Vector2 GetMovementInput()
    {
        float x = Input.GetAxis("Horizontal"); // Left stick X
        float y = Input.GetAxis("Vertical");   // Left stick Y
        return new Vector2(x, y);
    }

    public Vector2 GetLookInput()
    {
        float x = Input.GetAxis("RightStickX");  // Right stick X
        float y = Input.GetAxis("RightStickY");  // Right stick Y
        return new Vector2(x, y);
    }

    public bool GetShootFromTheHipInput()
    {
        return Input.GetButtonDown("Jump"); // Typically "A" button
    }

    public bool GetShootAtTargetInput()
    {
        return Input.GetButtonDown("Fire1"); // Right trigger or X button
    }
}

*/
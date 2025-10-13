using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

public class PauseManager : NetworkBehaviour
{
    public static PauseManager Instance;    

    public NetworkVariable<bool> isPaused = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (IsServer)
        {
            isPaused.Value = false;
        }
        
        isPaused.OnValueChanged += HandlePauseChanged;
    }

    private new void OnDestroy()
    {
        isPaused.OnValueChanged -= HandlePauseChanged;
    }

    private void Update()
    {
        // anyone can pause (could restrict that here)

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePauseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TogglePauseServerRpc()  // public for debugging....make this private 
    {
        isPaused.Value = !isPaused.Value;
        //Debug.Log($"Pause state set to: {isPaused.Value} by client {OwnerClientId}");
    }

    private void HandlePauseChanged(bool oldValue, bool newValue)
    {
        if (isPaused.Value)
        {
            GameplayUI.Instance.DisplayPausedMessage();
        }
        else
        {
            GameplayUI.Instance.ClearPausedMessage();
        }        
    }
}

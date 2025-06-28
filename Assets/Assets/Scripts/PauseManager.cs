using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEditor.Build;

public class PauseManager : NetworkBehaviour
{
    public static PauseManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject pauseUI;

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
        // Hide pause UI on start
        if (pauseUI != null)
            pauseUI.SetActive(false);

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
        Debug.Log($"Pause state set to: {isPaused.Value} by {OwnerClientId}");
    }

    private void HandlePauseChanged(bool oldValue, bool newValue)
    {
        if (pauseUI != null)
        {
            pauseUI.SetActive(newValue);
        }
    }
}

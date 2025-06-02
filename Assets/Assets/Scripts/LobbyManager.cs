using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private NetworkList<FixedString32Bytes> playerNames = new NetworkList<FixedString32Bytes>();

    [SerializeField] private LobbyUI lobbyUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"LobbyManager OnNetworkSpawn (IsServer: {IsServer}) on client {NetworkManager.LocalClientId}");

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

            playerNames.Clear();
            foreach (var clientId in NetworkManager.ConnectedClientsIds)
                playerNames.Add($"Player {clientId}");
        }

        // All clients update UI when player list changes
        playerNames.OnListChanged += OnPlayerListChanged;

        UpdatePlayerListUI(); // Show initial state
        
    }

    private void OnPlayerListChanged(NetworkListEvent<FixedString32Bytes> change)
    {
        UpdatePlayerListUI();
    }

    private void UpdatePlayerListUI()
    {
        // Build a list of strings from FixedString32Bytes
        List<string> names = new List<string>();
        foreach (var name in playerNames)
        {
            names.Add(name.ToString());
        }

        lobbyUI?.UpdatePlayerList(names);

        // Server pushes to clients explicitly
        if (IsServer)
        {
            FixedString32Bytes[] copy = new FixedString32Bytes[playerNames.Count];
            for (int i = 0; i < playerNames.Count; i++)
            {
                copy[i] = playerNames[i];
            }
            SendPlayerListToClientsClientRpc(copy);
        }
    }

    [ClientRpc]
    private void SendPlayerListToClientsClientRpc(FixedString32Bytes[] names)
    {
        Debug.Log($"[ClientRpc] Received player list with {names.Length} entries");
        if (lobbyUI == null)
        {
            Debug.LogWarning("LobbyUI is null on client!");
        }

        
        if (IsServer) return; // Server already has correct UI

        List<string> displayNames = new List<string>();
        foreach (var name in names)
        {
            displayNames.Add(name.ToString());
        }

        lobbyUI?.UpdatePlayerList(displayNames);
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        playerNames.Add($"Player {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        string targetName = $"Player {clientId}";
        for (int i = 0; i < playerNames.Count; i++)
        {
            if (playerNames[i].ToString() == targetName)
            {
                playerNames.RemoveAt(i);
                break;
            }
        }
    }

    public void StartGame()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only the host can start the game.");
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    public void LeaveLobby()
    {
        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene");
    }

    private new void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;

        playerNames.OnListChanged -= OnPlayerListChanged;

        if (Instance == this) Instance = null;
    }
}

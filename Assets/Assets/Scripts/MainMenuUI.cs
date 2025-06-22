using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField ipInputField;
    public Button hostButton;
    public Button joinButton;
    public TMP_Text statusText;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        statusText.text = "Host a game, or enter an IP and join a game.";
    }

    private void OnHostClicked()
    {
        //Debug.Log("OnHostClicked was detected");

        statusText.text = "Starting Host...";

        // Always clear previous callback before setting it again
        NetworkManager.Singleton.ConnectionApprovalCallback = null;
        //NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection; // optional, see helper below.  resets incoming prefab.
        NetworkManager.Singleton.StartHost();
        LoadLobbyScene();
    }

    private void OnJoinClicked()
    {
        //Debug.Log("OnJoinClicked was detected");

        string ip = ipInputField.text;
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

        if (!IsValidAddress(ip))
        {
            statusText.text = $"Invalid IP or hostname: {ip}";
            Debug.LogError($"Rejected invalid address: {ip}");
            return;
        }

        statusText.text = $"Connecting to {ip}...";
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        NetworkManager.Singleton.StartClient();
    }

    private void OnClientConnected(ulong clientId)
    {
        //Debug.Log($"OnClientConnected was detected for clientID {clientId}");

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            //Debug.Log($"Connected! Client {clientId} is entering Lobby... ");    

            statusText.text = "Connected! Entering Lobby...";
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;

            //SceneManager.LoadScene("LobbyScene");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        //Debug.Log($"OnClientDisconnected was detected for clientID {clientId}");

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"Client {clientId} has been disconnected... ");  

            statusText.text = "Failed to connect to host.";
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void LoadLobbyScene()
    {
        // For multiplayer scene switching use Netcode's SceneManager:
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }
    }

    // Optional approval for connection handling    
    /*
    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = true;
        response.PlayerPrefabHash = null;
        response.Position = Vector3.zero;
        response.Rotation = Quaternion.identity;
    }
    */    

    private bool IsValidAddress(string address)
    {
        // Try if it's a valid IP address
        if (IPAddress.TryParse(address, out _))
            return true;

        // Try DNS resolution (hostname like 'localhost' or 'somehost.com')
        try
        {
            Dns.GetHostEntry(address);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

using Unity.Netcode;
using UnityEngine;
using System.Net;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    private UIDocument uiDocument;
    public TextField ipInputField;

    // individual buttons
    public Button hostButton;
    public Button joinButton;

    // all buttons
    private List<Button> allMenuButtons = new List<Button>();

    private AudioSource audioSource;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        audioSource = GetComponent<AudioSource>();
        hostButton = uiDocument.rootVisualElement.Q("HostButton") as Button;
        joinButton = uiDocument.rootVisualElement.Q("JoinButton") as Button;
        

        // register callbacks for each button, as well as 'any' button
        hostButton.RegisterCallback<ClickEvent>(OnHostClicked);
        joinButton.RegisterCallback<ClickEvent>(OnJoinClicked);
        allMenuButtons = uiDocument.rootVisualElement.Query<Button>().ToList();
        foreach (Button menuButton in allMenuButtons)
        {
            menuButton.RegisterCallback<ClickEvent>(OnAnyButtonClicked);
        }
    }

    private void OnHostClicked(ClickEvent evt)
    {
        //Debug.Log("OnHostClicked was detected");

        // Always clear previous callback before setting it again
        NetworkManager.Singleton.ConnectionApprovalCallback = null;
        //NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection; // optional, see helper below.  resets incoming prefab.
        NetworkManager.Singleton.StartHost();
        LoadLobbyScene();
    }

    private void OnJoinClicked(ClickEvent evt)
    {
        //Debug.Log("OnJoinClicked was detected");

        string ip = ipInputField.text;
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

        if (!IsValidAddress(ip))
        {
            Debug.LogError($"Rejected invalid address: {ip}");
            return;
        }

        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, 7777);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        NetworkManager.Singleton.StartClient();
    }

    private void OnAnyButtonClicked(ClickEvent evt)
    {
        // play button-click sound
        audioSource.Play();
    }

    private void OnClientConnected(ulong clientId)
    {
        //Debug.Log($"OnClientConnected was detected for clientID {clientId}");

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            //Debug.Log($"Connected! Client {clientId} is entering Lobby... ");
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        //Debug.Log($"OnClientDisconnected was detected for clientID {clientId}");

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"Client {clientId} has been disconnected... ");
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

    private void OnDisable()
    {
        hostButton.UnregisterCallback<ClickEvent>(OnHostClicked);
        joinButton.UnregisterCallback<ClickEvent>(OnJoinClicked);
        foreach (Button menuButton in allMenuButtons)
        {
            menuButton.UnregisterCallback<ClickEvent>(OnAnyButtonClicked);
        }
    }
}

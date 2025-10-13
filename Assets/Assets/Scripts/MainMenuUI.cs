using Unity.Netcode;
using UnityEngine;
using System.Net;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;


public class MainMenuUI : NetworkBehaviour
{
    [Header("UI Elements")]
    private UIDocument uiDocument;

    // main menu inputs
    private VisualElement mainMenuContainer;
    private Button singlePlayerButton;
    private Button multiPlayerButton;
    private Button settingsButton;
    private Button quitGameButton;

    // settings menu inputs
    private VisualElement settingsMenuContainer;
    private DropdownField numOfEnemiesDropdown;
    private Toggle enemiesCanShootToggle;
    private Button settingsBackButton;

    // multiplayer menu inputs
    private VisualElement multiPlayerMenuContainer;
    private Button multiPlayerHostButton;
    private Button multiPlayerJoinButton;
    private TextField multiPlayeripInputField;
    private Button multiPlayerBackButton;

    // all buttons
    private List<Button> allMenuButtons = new List<Button>();

    private AudioSource audioSource;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        audioSource = GetComponent<AudioSource>();

        // menu containers
        mainMenuContainer = uiDocument.rootVisualElement.Q("MainMenuContainer") as VisualElement;
        settingsMenuContainer = uiDocument.rootVisualElement.Q("SettingsContainer") as VisualElement;
        multiPlayerMenuContainer = uiDocument.rootVisualElement.Q("MultiPlayerContainer") as VisualElement;

        // main menu inputs
        singlePlayerButton = uiDocument.rootVisualElement.Q("SinglePlayerGameButton") as Button;
        multiPlayerButton = uiDocument.rootVisualElement.Q("MultiPlayerGameButton") as Button;
        settingsButton = uiDocument.rootVisualElement.Q("SettingsButton") as Button;
        quitGameButton = uiDocument.rootVisualElement.Q("QuitGameButton") as Button;
        singlePlayerButton.RegisterCallback<ClickEvent>(OnSinglePlayerClicked);
        multiPlayerButton.RegisterCallback<ClickEvent>(OnMultiPlayerMenuClicked);
        settingsButton.RegisterCallback<ClickEvent>(OnSettingsClicked);
        quitGameButton.RegisterCallback<ClickEvent>(OnQuitGameClicked);

        // multi player menu inputs
        multiPlayerHostButton = uiDocument.rootVisualElement.Q("HostButton") as Button;
        multiPlayerJoinButton = uiDocument.rootVisualElement.Q("JoinButton") as Button;
        multiPlayeripInputField = uiDocument.rootVisualElement.Q("IpTextField") as TextField;
        multiPlayerBackButton = uiDocument.rootVisualElement.Q("MultiPlayerBackButton") as Button;
        multiPlayerHostButton.RegisterCallback<ClickEvent>(OnHostClicked);
        multiPlayerJoinButton.RegisterCallback<ClickEvent>(OnJoinClicked);
        multiPlayerBackButton.RegisterCallback<ClickEvent>(OnMultiPlayerBackClicked);

        // settings menu inputs
        numOfEnemiesDropdown = uiDocument.rootVisualElement.Q("NumEnemiesDropdown") as DropdownField;
        enemiesCanShootToggle = uiDocument.rootVisualElement.Q("EnemiesCanShootToggle") as Toggle;
        settingsBackButton = uiDocument.rootVisualElement.Q("SettingsBackButton") as Button;
        settingsBackButton.RegisterCallback<ClickEvent>(OnSettingsBackClicked);

        // 'all' button callbacks
        allMenuButtons = uiDocument.rootVisualElement.Query<Button>().ToList();
        foreach (Button menuButton in allMenuButtons)
        {
            menuButton.RegisterCallback<ClickEvent>(OnAnyButtonClicked);
        }

        // initial setup
        multiPlayerMenuContainer.style.display = DisplayStyle.None;
        settingsMenuContainer.style.display = DisplayStyle.None;
        mainMenuContainer.style.display = DisplayStyle.Flex;
        numOfEnemiesDropdown.index = 2;
        enemiesCanShootToggle.value = true;        
    }    

    //----------------------
    // SINGLE PLAYER
    //----------------------

    private void OnSinglePlayerClicked(ClickEvent evt)
    {
        // start single player game
        StoreGameSettings(false);
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
    }

    //----------------------
    // SETTINGS
    //----------------------

    private void OnSettingsClicked(ClickEvent evt)
    {
        //Debug.Log("switching to multi player menu container");
        mainMenuContainer.style.display     = DisplayStyle.None;
        settingsMenuContainer.style.display = DisplayStyle.Flex;
    }

    private void OnSettingsBackClicked(ClickEvent evt)
    {
        //Debug.Log("switching to multi player menu container");
        settingsMenuContainer.style.display = DisplayStyle.None;
        mainMenuContainer.style.display     = DisplayStyle.Flex;
    }

    private void StoreGameSettings(bool isMultiplayer = false)
    {
        // store menu setting in GameSettings static class
        GameSettings.EnemyCount = Int32.Parse(numOfEnemiesDropdown.value);
        GameSettings.EnemiesCanShoot = enemiesCanShootToggle.value;
        GameSettings.GameIsMultiplayer = isMultiplayer;
    }

    //----------------------
    // MULTIPLAYER
    //----------------------

    private void OnMultiPlayerMenuClicked(ClickEvent evt)
    {
        //Debug.Log("switching to multi player menu container");
        mainMenuContainer.style.display = DisplayStyle.None;
        multiPlayerMenuContainer.style.display = DisplayStyle.Flex;
    } 

    private void OnMultiPlayerBackClicked(ClickEvent evt)
    {
        // swap to main menu
        multiPlayerMenuContainer.style.display = DisplayStyle.None;
        mainMenuContainer.style.display = DisplayStyle.Flex;
    }

    private void OnHostClicked(ClickEvent evt)
    {
        //Debug.Log("Starting multiplayer game as host.");
        StoreGameSettings(true);
        // Always clear previous callback before setting it again
        NetworkManager.Singleton.ConnectionApprovalCallback = null;
        // optional, see helper below.  resets incoming prefab.
        //NetworkManager.Singleton.ConnectionApprovalCallback += ApproveConnection; 
        NetworkManager.Singleton.StartHost();        
        LoadLobbyScene();
    }

    private void OnJoinClicked(ClickEvent evt)
    {
        //Debug.Log("OnJoinClicked was detected");

        string ip = multiPlayeripInputField.value;
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
            //Debug.Log($"Client {clientId} has been disconnected... ");
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

    //----------------------
    // OTHER
    //----------------------

    private void OnAnyButtonClicked(ClickEvent evt)
    {
        // play button-click sound
        audioSource.Play();
    }

    private void OnQuitGameClicked(ClickEvent evt)
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    private void OnDisable()
    {
        if (multiPlayerHostButton != null && multiPlayerJoinButton != null)
        {
            multiPlayerHostButton.UnregisterCallback<ClickEvent>(OnHostClicked);
            multiPlayerJoinButton.UnregisterCallback<ClickEvent>(OnJoinClicked);
            
            foreach (Button menuButton in allMenuButtons)
            {
                menuButton.UnregisterCallback<ClickEvent>(OnAnyButtonClicked);
            }
        }
    }
}

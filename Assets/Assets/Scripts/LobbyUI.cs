using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyUI : MonoBehaviour
{
    [Header("UI Elements")]
    private UIDocument uiDocument;
    private List<Label> playerNameTexts;

    // individual buttons
    private Button startGameButton;
    private Button backButton;

    // all buttons
    private List<Button> allMenuButtons = new List<Button>();

    private AudioSource audioSource;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        audioSource = GetComponent<AudioSource>();

        startGameButton = uiDocument.rootVisualElement.Q("StartGameButton") as Button;
        backButton = uiDocument.rootVisualElement.Q("BackButton") as Button;

        // Hide Start Game button if not Host
        if (!Unity.Netcode.NetworkManager.Singleton.IsHost)
        {
            startGameButton.style.display = DisplayStyle.None;
        }        

        // register callbacks for each button, as well as 'any' button
        startGameButton.RegisterCallback<ClickEvent>(OnStartGameClicked);
        backButton.RegisterCallback<ClickEvent>(OnBackClicked);
        allMenuButtons = uiDocument.rootVisualElement.Query<Button>().ToList();
        foreach (Button lobbyButton in allMenuButtons)
        {
            lobbyButton.RegisterCallback<ClickEvent>(OnAnyButtonClicked);
        }

        // get the list of labels that will display connected players
        VisualElement connectedPlayersContainer = uiDocument.rootVisualElement.Q("ConnectedPlayersContainer");
        playerNameTexts = connectedPlayersContainer.Query<Label>().ToList();

        //Debug.Log($"PlayerNameTexts has {playerNameTexts.Count} players.");
        
        // Clear all player labels on startup
        ClearAllPlayerNames();
    }

    // called by LobbyManager when clients join/leave
    public void UpdatePlayerList(List<string> playerNames)
    {
        ClearAllPlayerNames();

        for (int i = 0; i < playerNames.Count && i < playerNameTexts.Count; i++)
        {
            playerNameTexts[i].text = playerNames[i];
        }
    }

    private void ClearAllPlayerNames()
    {
        foreach (var text in playerNameTexts)
        {
            text.text = "";
        }
    }

    private void OnAnyButtonClicked(ClickEvent evt)
    {
        // play button-click sound
        audioSource.Play();
    }

    private void OnStartGameClicked(ClickEvent evt)
    {
        //Debug.Log("Start Game clicked");
        LobbyManager.Instance?.StartGame();
    }

    private void OnBackClicked(ClickEvent evt)
    {
        //Debug.Log("Leave Lobby clicked");
        LobbyManager.Instance?.LeaveLobby();
    }
    
    private void OnDisable()
    {
        startGameButton.UnregisterCallback<ClickEvent>(OnStartGameClicked);
        backButton.UnregisterCallback<ClickEvent>(OnBackClicked);
        foreach (Button menuButton in allMenuButtons)
        {
            menuButton.UnregisterCallback<ClickEvent>(OnAnyButtonClicked);
        }
    }
}

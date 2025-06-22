using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Player Name Slots")]
    [SerializeField] private List<TextMeshProUGUI> playerNameTexts; // Drag & drop 5 TMP Texts in Inspector

    [Header("Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveLobbyButton;

    private void Awake()
    {
        startGameButton.onClick.AddListener(OnStartGameClicked);
        leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);

        // Hide Start Game button if not Host
        startGameButton.gameObject.SetActive(Unity.Netcode.NetworkManager.Singleton.IsHost);

        // Clear all player labels on startup
        ClearAllPlayerNames();
    }

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

    private void OnStartGameClicked()
    {
        //Debug.Log("Start Game clicked");
        LobbyManager.Instance?.StartGame();
    }

    private void OnLeaveLobbyClicked()
    {
        //Debug.Log("Leave Lobby clicked");
        LobbyManager.Instance?.LeaveLobby();
    }
}

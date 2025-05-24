using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MultiplayerUI : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public Button restartButton;
    public TMP_Text statusText;
    public TMP_InputField ipInputField;


    void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
    }

    void OnHostClicked()
    {
        statusText.text = "Starting Host...";
        MultiplayerManager.Instance.StartHost();
    }

    void OnJoinClicked()
    {
        string ip = string.IsNullOrWhiteSpace(ipInputField.text)
                    ? "127.0.0.1"
                    : ipInputField.text.Trim();

        statusText.text = $"Joining {ip}…";
        MultiplayerManager.Instance.StartClient(ip);

        // Deselect input field so it no longer receives input
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnRestartClicked()
    {
        // Only host has authority to reset the scene
        if (NetworkManager.Singleton.IsHost)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;

            // This method reloads the scene across all connected clients
            NetworkManager.Singleton.SceneManager.LoadScene(
                currentSceneName, 
                LoadSceneMode.Single
            );

            Debug.Log("Host triggered scene reset.");
        }
        else
        {
            Debug.Log("Only host can reset the game.");
        }
    }

    void Update()
    {
        if (MultiplayerManager.Instance == null || !Application.isPlaying) return;

        if (MultiplayerManager.Instance.IsHost)
            statusText.text = "Status: Host";
        else if (MultiplayerManager.Instance.IsClient)
            statusText.text = "Status: Client";

        // hide the buttons when connected
        var manager = MultiplayerManager.Instance;
        if (manager.IsHost || manager.IsClient)
        {
            if (ipInputField.gameObject.activeSelf)
                ipInputField.gameObject.SetActive(false);

            if (hostButton.gameObject.activeSelf)
                hostButton.gameObject.SetActive(false);

            if (joinButton.gameObject.activeSelf)
                joinButton.gameObject.SetActive(false);
        }

        // Quit on Q or Escape
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }
    
    void QuitGame()
    {
        Debug.Log("Quitting game...");

        if (MultiplayerManager.Instance != null)
        {
            var networkManager = NetworkManager.Singleton;

            if (networkManager != null)
            {
                if (networkManager.IsHost)
                    networkManager.Shutdown(); // Disconnects all clients too
                else if (networkManager.IsClient)
                    networkManager.Shutdown(); // Clean disconnect
            }
        }

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}

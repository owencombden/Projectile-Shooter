using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        None,
        Setup,
        Gameplay,
        Win,
        Loss,
        Restarting
    }

    public static GameManager Instance;
    public AssetManager assetManager;
    public bool allowEnemies = true;

    private GameState currentState = GameState.None;

    private void OnEnable()
    {
        Debug.Log("GameManager OnEnable has run");

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnNetworkSceneLoaded;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        else
        {
            Debug.LogWarning("NetworkManager or SceneManager is not initialized yet.");
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnNetworkSceneLoaded;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }        
    }

    private void OnNetworkSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Debug.Log("GameManager OnNetworkSceneLoaded has run");  //THIS IS NOT GETTING CALLED!

        // Host initializes the level *after* everyone finishes loading
        if (NetworkManager.Singleton.IsHost && clientId == NetworkManager.Singleton.LocalClientId)
        {
            SetGameState(GameState.Setup); // Triggers LevelManager.InitializeLevel()
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected.");

        // Only the host should clean up and check win/loss
        if (!NetworkManager.Singleton.IsHost) return;

        var playerObj = NetworkManager.Singleton.ConnectedClients
            .Where(kvp => kvp.Key == clientId)
            .Select(kvp => kvp.Value.PlayerObject)
            .FirstOrDefault();

        if (playerObj == null)
        {
            Debug.LogWarning($"No PlayerObject found for disconnected client {clientId}");
            return;
        }

        // Optional: Show visual effect, log, etc.
        Debug.Log($"Destroying PlayerObject for client {clientId}");

        // Try to clean up character from LevelManager
        var playerController = playerObj.GetComponent<PlayerController>();
        if (playerController != null)
        {
            LevelManager.Instance.RemoveCharacter(playerController.id, isPlayer: true);
        }

        // Despawn network object (host authority)
        if (playerObj != null && playerObj.IsSpawned)
            playerObj.Despawn();

        // Check for game end conditions
        if (LevelManager.Instance.AreAllAIsDefeated())
            SetGameState(GameState.Win);
        else if (LevelManager.Instance.IsPlayerDefeated())
            SetGameState(GameState.Loss);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState != GameState.Gameplay) return;

        // Check for win/loss based on current counts
        if (LevelManager.Instance.AreAllAIsDefeated())
        {
            SetGameState(GameState.Win);
        }
        else if (LevelManager.Instance.IsPlayerDefeated())
        {
            SetGameState(GameState.Loss);
        }
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"Game State changed to: {currentState}");

        switch (currentState)
        {
            case GameState.Setup:
                StartCoroutine(HandleSetup());
                break;
            case GameState.Gameplay:
                // Begin gameplay loop
                break;
            case GameState.Win:
                HandleWin();
                break;
            case GameState.Loss:
                HandleLoss();
                break;
            case GameState.Restarting:
                StartCoroutine(RestartLevelAfterDelay(2f));
                break;
        }
    }

    private IEnumerator HandleSetup()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening);

        LevelManager.Instance.InitializeLevel(); // Create method to call platform/character spawn
    }

    public void TransitionToGameplay()
    {
        SetGameState(GameState.Gameplay);
        Debug.Log("Game has started!");
    }

    public void OnPlayerDefeated()
    {
        if (currentState != GameState.Gameplay) return;

        Debug.Log("Player defeated!");
        currentState = GameState.Loss;
        HandleLoss();
    }

    public void OnAllAIsDefeated()
    {
        if (currentState != GameState.Gameplay) return;

        Debug.Log("All AI defeated!");
        currentState = GameState.Win;
        HandleWin();
    }

    private void HandleWin()
    {
        Debug.Log("Player wins! Restarting...");
        StartCoroutine(RestartLevelAfterDelay(2f));
    }

    private void HandleLoss()
    {
        Debug.Log("Player loses! Restarting...");
        StartCoroutine(RestartLevelAfterDelay(2f));
    }

    private IEnumerator RestartLevelAfterDelay(float delay)
    {
        currentState = GameState.Restarting;
        yield return new WaitForSeconds(delay);
        ReloadScene();
    }

    public void ReloadScene()
    {
        // Only host has authority to reset the scene
        if (NetworkManager.Singleton.IsHost)
        {
            // Clear all pooled/spawned objects before scene reload
            LevelManager.Instance?.CleanUpBeforeRestart();

            string currentSceneName = SceneManager.GetActiveScene().name;

            // This method reloads the scene across all connected clients
            NetworkManager.Singleton.SceneManager.LoadScene(
                currentSceneName,
                LoadSceneMode.Single
            );

            Debug.Log("Host started a new game.");
        }
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }      
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    public enum GameState
    {
        None,
        Setup,
        Gameplay,
        GameOver
    }

    public static GameManager Instance;
    public AssetManager assetManager;
    public bool allowEnemies = true;

    private GameState currentState = GameState.None;

    private HashSet<ulong> clientsLoadedScene = new(); // counter of clients that have connected


    /*
    POTENTIAL GAME FLOW
    --------------------
    [Scene Load Phase]
    → Host + Clients enter GameScene via network scene loading
    → Each client (including host) fires `OnLoadComplete`
    → Each client sends "SceneLoaded" to host

    [Tile Sync Phase]
    → Host waits until all clients have sent "SceneLoaded"
    → Host generates platform and tile data
    → Host sends platform and tile data to all clients via RPC
    → Each client (and host) uses that data to spawn tiles
    → Each client sends "TilesSpawned" to host

    [Game Start Phase]
    → Host waits until all clients have sent "TilesSpawned"
    → Host sends `StartGameClientRpc()` to begin gameplay
    */



    private void OnEnable()
    {
        //Debug.Log("GameManager OnEnable has run");

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
        //Debug.Log($"Local client ({clientId}) finished loading scene.");

        //ClientSceneLoadedServerRpc(clientId);
        if (NetworkManager.Singleton.LocalClientId == clientId) // ✅ Only call from the local client
        {
            ClientSceneLoadedServerRpc(clientId);
        }
    }

    
    [ServerRpc(RequireOwnership = false)]
    public void ClientSceneLoadedServerRpc(ulong clientId)
    {
        // count the incoming clients.  when all have connected, setup the game
        //if (!NetworkManager.Singleton.IsHost) return;

        //Debug.Log($"ClientSceneLoadedServerRpc is firing from Client {clientId}");

        if (!clientsLoadedScene.Contains(clientId))
        {
            clientsLoadedScene.Add(clientId);
            //Debug.Log($"Client {clientId} reported scene loaded. Total: {clientsLoadedScene.Count} of {NetworkManager.Singleton.ConnectedClientsIds.Count}");

            // Check if all connected clients are ready
            int totalClients = NetworkManager.Singleton.ConnectedClientsIds.Count;

            if (clientsLoadedScene.Count == totalClients)
            {
                //Debug.Log("All clients have loaded the scene.");
                SetGameState(GameState.Setup);
            }
        }
        else
        {
            //Debug.Log($"Client {clientId} has already been included in the clientsLoadedScene list!");
        }
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
        if (!IsServer) return;

        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
        return;

        if (currentState != GameState.Gameplay) return;
        
    }

    public void TransitionToGameplay()
    {
        SetGameState(GameState.Gameplay);
    }

    public void TransitionToGameOver()
    {
        SetGameState(GameState.GameOver);
    }

    private void SetGameState(GameState newState)
    {
        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is attempting to change GameState to: {newState}.  It is currently {currentState}");

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
                //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} has started the game!");
                break;
            case GameState.GameOver:
                HandleGameOver();
                break;
        }
    }

    public GameState GetGameState()
    {
        return currentState;
    }

    private IEnumerator HandleSetup()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer && NetworkManager.Singleton.IsListening);

        LevelManager.Instance.InitializeLevel(); // Create method to call platform/character spawn
    }


    private void HandleGameOver()
    {
        if (!IsServer) return;

        Debug.Log("GameManager has ended the game!");

        StartCoroutine(LoadLobbySceneAfterDelay(5f));
    }

    private IEnumerator LoadLobbySceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Clear all pooled/spawned objects before scene reload
        LevelManager.Instance?.CleanUpBeforeRestart();

        Debug.Log("Loading Lobby Scene...");
        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
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
            LevelManager.Instance.RemoveCharacter(playerController.id.Value, isPlayer: true);
        }

        // Despawn network object (host authority)
        if (playerObj != null && playerObj.IsSpawned)
            playerObj.Despawn();        
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private new void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }      
}

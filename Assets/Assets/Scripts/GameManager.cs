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

    private GameSettingsData gameSettings;
    private GameState currentState = GameState.None;

    private HashSet<ulong> clientsLoadedScene = new(); // counter of clients that have connected

    public NetworkVariable<bool> isGameOver = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


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
                Debug.Log("All clients have loaded the scene.");

                //Debug.Log($"Reading Game Settings.  AI can shoot? {GameSettings.EnemiesCanShoot}");

                // apply the game settings to host and all clients
                gameSettings = GameSettings.ToData();
                ApplySettingsClientRpc(gameSettings);

                SetGameState(GameState.Setup);
            }
        }
        else
        {
            //Debug.Log($"Client {clientId} has already been included in the clientsLoadedScene list!");
        }
    }

    [ClientRpc]
    private void ApplySettingsClientRpc(GameSettingsData settings)
    {
        gameSettings = settings;

        Debug.Log($"[Client] Received settings: {gameSettings.enemyCount} enemies, shoot={gameSettings.enemiesCanShoot}, isMultiplayer={gameSettings.gameIsMultiplayer}");

        // next step, apply the game settings to gameplay here 
        // ex:  LevelManager.Instance.numEnemies = gameSettings.enemyCount;
    }
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
        return;

        if (currentState != GameState.Gameplay) return;
        
    }

    public bool IsMultiplayerGame()
    {
        return gameSettings.gameIsMultiplayer;
    }

    public void TransitionToGameplay()
    {
        SetGameState(GameState.Gameplay);
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

    public void SubmitAIDeath(NetworkObject losingAI, ulong losingAI_Id)
    {
        if (!IsServer) return;
        
        LevelManager.Instance.RemoveCharacter(losingAI_Id, false);
        TargetManager.Instance.UnregisterTarget(losingAI.GetComponent<Targetable>());

        // despawn the loser, return him to the pool.
        if (losingAI && losingAI.IsSpawned)
        {
            Debug.Log($"Despawining a losing AI with id {losingAI_Id}");
            losingAI.Despawn(false);
            AssetManager.Instance.ReturnAI(losingAI.gameObject);
        }

        // check for game over and handle process
        StartCoroutine(HandleGameOverSequence()); 
    }

    [ClientRpc]
    public void PlayPlayerDeathClientRpc(ulong deadPlayerId, Vector3 tippingAxis)
    {
        var player = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
            .FirstOrDefault(p => p.id.Value == deadPlayerId);

        if (player == null)
        {
            Debug.LogWarning($"[DeathAnim] Player with id {deadPlayerId} not found on client {NetworkManager.Singleton.LocalClientId}");
            return;
        }

        Debug.Log($"[DeathAnim] Playing death animation on client {NetworkManager.Singleton.LocalClientId} for player {deadPlayerId}");

        player.StartCoroutine(player.FallOver(tippingAxis));
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitDeathServerRpc(Vector3 tippingAxis, ServerRpcParams rpcParams = default)
    {
        // which client called this?
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        var deadPlayer = NetworkManager.Singleton.ConnectedClients[senderClientId].PlayerObject;
        ulong deadPlayerId = deadPlayer.GetComponent<PlayerController>().id.Value;
        Debug.Log($"Server (Client {NetworkManager.Singleton.LocalClientId}) is handling a death for Client {deadPlayerId}");

        // Tell all clients to play the death animation
        GameManager.Instance.PlayPlayerDeathClientRpc(deadPlayerId, tippingAxis);
        
        StartCoroutine(HandlePlayerLostSequence(deadPlayer, deadPlayerId));        
    }

    private IEnumerator HandlePlayerLostSequence(NetworkObject losingPlayer, ulong losingId)
    {
        // Wait til death animation finished, then begin loss-sequence
        yield return new WaitForSeconds(0.75f);
        Debug.Log($"Death animation has finished, despawning Player {losingId}");

        LevelManager.Instance.RemoveCharacter(losingId, true);
        TargetManager.Instance.UnregisterTarget(losingPlayer.GetComponent<Targetable>());

        // show the 'you lost' banner, client-side, on the dead player
        if (losingPlayer.gameObject != null && losingPlayer.gameObject.TryGetComponent<PlayerController>(out var losingController))
        {
            losingController.TellClientToShowYouLostBanner();
        }

        // Wait til banner animation finished, then despawn the loser
        //yield return new WaitForSeconds(5.0f);

        // despawn the loser, return him to the pool.
        if (losingPlayer && losingPlayer.IsSpawned)
        {
            Debug.Log($"Despawining the loser with id {losingId}");
            losingPlayer.Despawn(false);
            AssetManager.Instance.ReturnPlayer(losingPlayer.gameObject);

            // do something else here?  send them back to lobby, or show another player-camera?
        }

        // check for game over and handle process
        StartCoroutine(HandleGameOverSequence());

        yield break;       
    }

    // may be called
    private IEnumerator HandleGameOverSequence()
    {
        // should be called by server only!!

        // check for game over
        Debug.Log($"GameManager is checking for game over");
        bool gameIsOver = LevelManager.Instance.CheckForWinner();

        if (!gameIsOver) yield break;

        // game is over
        Debug.Log($"Game is over!");
        isGameOver.Value = true;
        SetGameState(GameState.GameOver);

        // get the details on the winner
        (NetworkObject winningPlayer,
        ulong winnerId,
        bool winnerIsHuman) = LevelManager.Instance.GetWinnerDetails();
        

        if (winnerIsHuman)
        {
            // show banner for winner
            if (winningPlayer != null && winningPlayer.TryGetComponent<PlayerController>(out var winningController))
            {
                winningController.TellClientToShowYouWonBanner();
            }

            // banner delay
            Debug.Log($"Human Player {winnerId} was the winner!");
            yield return new WaitForSeconds(5.0f);

            // despawn the human winner
            Debug.Log($"Despawining the winner");
            winningPlayer.Despawn(false);
            AssetManager.Instance.ReturnPlayer(winningPlayer.gameObject);
        }
        else
        {
            // ai won, do ai celebrations here
            Debug.Log($"AI {winnerId} was the winner!");

            yield return new WaitForSeconds(5.0f);

            // despawn the ai winner
            Debug.Log($"Despawining the winner");
            winningPlayer.Despawn(false);
            AssetManager.Instance.ReturnAI(winningPlayer.gameObject);
        }

        // cleanup and go to lobby
        Debug.Log("Loading Lobby Scene...");
        LevelManager.Instance?.CleanUpBeforeRestart();
        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }


    // these are called if the server or client quits during gameplay
    [ServerRpc(RequireOwnership = false)]
    public void EndGameServerRpc()
    {
        Debug.Log("ServerRpc: Host is ending the game.");

        // host is quitting, tell all clients (including the host) the game is over
        EndGameClientRpc();
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
        // called by all connected clients
        Debug.Log("ClientRpc: All players (including host) cleaning up.");
        CleanupAndReturnToMenu();
    }

    private void CleanupAndReturnToMenu()
    {
        // called by all connected clients
        ulong clientId = NetworkManager.Singleton.LocalClientId;

        RemoveThisClientFromPlayServerRpc(clientId);        

        Debug.Log("Shutting down the network manager.");
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Debug.Log("Heading to main menu scene.");
        SceneManager.LoadScene("MainMenuScene");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveThisClientFromPlayServerRpc(ulong clientId)
    {
        Debug.Log($"ServerRpc: Server is removing client {clientId} from the game.");

        // remove the player from LevelManager collection
        LevelManager.Instance.RemoveCharacter(clientId, true);   

        // despawn from the network
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) is NetworkObject playerObj)
        {
            Debug.Log("Despawning the network object.");
            playerObj.Despawn();
        } 
    }


    [ServerRpc(RequireOwnership = false)]
    public void NotifyServerClientIsQuittingServerRpc(ulong clientId)
    {
        Debug.Log($"Client {clientId} is quitting the game.");
        // do cleanup or notify other players here

        // remove the player from LevelManager collection
        LevelManager.Instance.RemoveCharacter(clientId, true);

        // do other 'client has left the game' things here...

        // despawn from the network
        if (NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) is NetworkObject playerObj)
        {
            Debug.Log("Despawning the network object.");
            playerObj.Despawn();
        }
        
        // Optionally, force-disconnect that client (safety net)
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            NetworkManager.Singleton.DisconnectClient(clientId);        
    }


    // called when a network connection is broken
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("Entering OnClientDisconnected");
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"This is the server.  Client {clientId} has disconnected");

            // remove the player from LevelManager collection
            LevelManager.Instance.RemoveCharacter(clientId, true);                           
        }
    }

    // this Instance is a global static reference.  Need to ensure that ref is cleared whenever reloading a scene.
    private new void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }      
}

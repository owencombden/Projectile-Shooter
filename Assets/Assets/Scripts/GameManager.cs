
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        Setup,
        Gameplay,
        Win,
        Loss,
        Restarting
    }

    public static GameManager Instance; 
    public AssetManager assetManager;
    public bool allowEnemies = true;    

    [Header("Game Settings")]
    [Range(0, 5)]
    [Tooltip("Number of AI bots to spawn (0 to 5).")]
    public int numberOfAIBots = 3;
    
    private Dictionary<int, PlayerController> playerControllers = new();
    private Dictionary<int, AIController> aiControllers = new();

    private GameState currentState = GameState.Setup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        int playerCount = GameObject.FindGameObjectsWithTag("Player").Length;
        //Debug.Log($"GameManager is Awake.  There are {playerCount} players in the scene.");

    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        

    }

    // Update is called once per frame
    void Update()
    {
               
    }
    
    public void SpawnAllCharacters(Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms)
    {
        int totalCharacters = 2;
        int connectedPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        int aiNeeded = totalCharacters - connectedPlayers;

        // Assign platforms
        List<int> platformIndices = new List<int>(platforms.Keys);

        int currentPlatformIndex = 0;

        // HUMAN PLAYERS (Network-spawned already, just reposition them)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            //Debug.Log($"There are {NetworkManager.Singleton.ConnectedClientsList.Count} connected clients");
            GameObject playerObj = client.PlayerObject.gameObject;
            var spawnTile = HexUtils.GetRandomHexTile(platforms[platformIndices[currentPlatformIndex]]);
            Vector3 spawnPos = spawnTile.transform.position;
            spawnPos.y = 2.33f;

            playerObj.transform.position = spawnPos;

            CharacterMotor playerMotorScript = playerObj.GetComponent<CharacterMotor>();
            playerMotorScript.isPlayer = true;

            var playerController = playerObj.GetComponent<PlayerController>();
            playerController.Set_ID(platformIndices[currentPlatformIndex]);
            playerController.SetPlayerHexMap(platforms[platformIndices[currentPlatformIndex]]);

            RegisterPlayer(platformIndices[currentPlatformIndex], playerController);

            // Set up the camera for the local player
            if (playerController.IsOwner)       // watch this.  was using 'isLocalPlayer', but it changed during multiplayer edits.
            {
                Camera.main.GetComponent<CameraLook>().SetTarget(playerObj.transform, playerObj.transform.Find("CameraLookHere"));
            }

            currentPlatformIndex++;
        }
        
        
        int AICount = GameObject.FindGameObjectsWithTag("AI_Player").Length;
        //Debug.Log($"SpawnAllCharacters is about to start spawning AI.  There are {AICount} AI in the scene.");

        // AI PLAYERS
        for (int i = 0; i < aiNeeded; i++)
        {

            // only the Server should spawn/handle AI
            if (!NetworkManager.Singleton.IsServer) return;

            //Debug.Log($"Spawning AI {i + 1} of {aiNeeded}.");
            int platformIndex = platformIndices[currentPlatformIndex];
            Vector3 aiSpawnPos = HexUtils.GetRandomHexTile(platforms[platformIndex]).transform.position;
            aiSpawnPos.y = 2.77f;

            GameObject ai = AssetManager.Instance.GetAI(aiSpawnPos, Quaternion.identity);

            int AICount2 = GameObject.FindGameObjectsWithTag("AI_Player").Length;
            //Debug.Log($"SpawnAllCharacters has instantiated an AI.  There are {AICount2} AI in the scene.");

            // Ensure the AI prefab has a NetworkObject component
            NetworkObject networkObject = ai.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();

                int AICount3 = GameObject.FindGameObjectsWithTag("AI_Player").Length;
                //Debug.Log($"networkObject has spawned an AI.  There are {AICount3} AI in the scene.");
            }
            else
            {
                //Debug.LogError("AI prefab is missing NetworkObject component.");
            }

            var aiController = ai.GetComponent<AIController>();
            aiController.SetAIHexMap(platforms[platformIndex]);
            int aiID = 1000 + platformIndex;
            aiController.Set_ID(aiID);

            RegisterAI(aiID, aiController);  // this registration is local (in GameManager), NOT Network client/player registration

            currentPlatformIndex++;
        }

        TransitionToGameplay();
    }

    private void TransitionToGameplay()
    {
        currentState = GameState.Gameplay;
        Debug.Log("Game has started!");
    } 

    public void RegisterPlayer(int player_ID, PlayerController controllerScript)
    {
        playerControllers[player_ID] = controllerScript;
    }

    public void RegisterAI(int ai_ID, AIController controllerScript)
    {
        aiControllers[ai_ID] = controllerScript;
    }

    public void RemoveCharacter(int id, bool isPlayer)
    {
        if (isPlayer)
        {
            playerControllers.Remove(id);
            OnPlayerDefeated();
        }
        else
        {
            aiControllers.Remove(id);
            CheckIfAllAIsDefeated();
        }
    }

    // NEED TO IMPLEMENT ACTIONS FOR WHEN PLAYERS ARE DEFEATED!!

    private void OnPlayerDefeated()
    {
        if (currentState != GameState.Gameplay) return;

        Debug.Log("Player defeated!");
        currentState = GameState.Loss;
        HandleLoss();
    }

    private void CheckIfAllAIsDefeated()
    {
        if (currentState != GameState.Gameplay) return;

        if (aiControllers.Count == 0)
        {
            Debug.Log("All AI defeated!");
            currentState = GameState.Win;
            HandleWin();
        }
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
            string currentSceneName = SceneManager.GetActiveScene().name;

            // This method reloads the scene across all connected clients
            NetworkManager.Singleton.SceneManager.LoadScene(
                currentSceneName, 
                LoadSceneMode.Single
            );

            Debug.Log("Host started a new game.");
        }
    }      
}

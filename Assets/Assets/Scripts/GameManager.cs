
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    [Header("Game Settings")]
    [Range(1, 5)]
    [Tooltip("Number of AI bots to spawn (1 to 5).")]
    public int numberOfAIBots = 3;
    
    private Dictionary<int, PlayerController> playerControllers = new();
    private Dictionary<int, AIController> aiControllers = new();

    private GameState currentState = GameState.Setup;    

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
       
    }

    public void SpawnAllCharacters(Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms)
    {
        // Spawn the human player on platform 0
        Vector3 playerTilePos = HexUtils.GetRandomHexTile(platforms[0]).transform.position;
        playerTilePos.y = 2.33f;
        GameObject player0 = AssetManager.Instance.GetPlayer(playerTilePos, Quaternion.identity);
        PlayerController playerControllerScript = player0.GetComponent<PlayerController>();
        playerControllerScript.SetPlayerHexMap(platforms[0]);
        int playerID = 0; // fix this when ready for multiplayer.
        playerControllerScript.Set_ID(playerID);  
        RegisterPlayer(playerID, playerControllerScript);

        // Set up the camera for the local player
        if (playerControllerScript.isLocalPlayer)
        {
            Camera.main.GetComponent<CameraLook>().SetTarget(player0.transform, player0.transform.Find("CameraLookHere"));
        }

        // Spawn AI bots on subsequent platforms
        for (int i = 0; i < numberOfAIBots; i++)
        {
            int platformIndex = i + 1;
            if (!platforms.ContainsKey(platformIndex))
            {
                Debug.LogWarning($"Platform {platformIndex} not found. Skipping AI spawn.");
                continue;
            }

            Vector3 aiTilePos = HexUtils.GetRandomHexTile(platforms[platformIndex]).transform.position;
            aiTilePos.y = 2.77f;
            GameObject ai = AssetManager.Instance.GetAI(aiTilePos, Quaternion.identity);
            ai.name = $"AI_{i}";
            AIController aiControllerScript = ai.GetComponent<AIController>();
            aiControllerScript.SetAIHexMap(platforms[platformIndex]);
            int ai_id = 1000 + i; // fix this when ready for multiplayer.
            aiControllerScript.Set_ID(ai_id);
            RegisterAI(ai_id, aiControllerScript);
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
        SceneManager.LoadScene("SampleScene");
    }  

    
}

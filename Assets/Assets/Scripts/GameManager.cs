
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public AssetManager assetManager;

    private List<Vector3> playerSpawnPoints = new List<Vector3>();
    private List<Vector3> enemySpawnPoints = new List<Vector3>();


    public static GameManager Instance;    

    private Dictionary<int, PlayerMove> playerControllers = new();
    private Dictionary<int, EnemyMove> enemyControllers = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Cursor.lockState = CursorLockMode.Confined;        
        //Cursor.visible = false; 
        //Cursor.lockState = CursorLockMode.Locked;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        

    }

    // Update is called once per frame
    void Update()
    {
        MoveCharacters();
    }
    
    public void ReloadScene()
    {
        SceneManager.LoadScene("SampleScene");
    }

    private void MoveCharacters()
    {
        foreach (var kvp in playerControllers)
        {
            int playerID = kvp.Key;
            PlayerMove playerMove = kvp.Value;

            //Vector2 input = InputManager.Instance.GetInputForPlayer(playerID);  // however you’ve set this up
            //playerMove.Move(input);
        }

        foreach (var kvp in enemyControllers)
        {
            EnemyMove enemyMove = kvp.Value;

            //Vector2 aiInput = enemyMove.DecideNextMove(); // Or from an AIManager
            //enemyMove.Move(aiInput);
        }
    }

    public void RegisterPlayer(int playerID, PlayerMove moveScript)
    {
        playerControllers[playerID] = moveScript;
    }

    public void RegisterEnemy(int enemyID, EnemyMove moveScript)
    {
        enemyControllers[enemyID] = moveScript;
    }

    public void RemoveCharacter(int id, bool isPlayer)
    {
        if (isPlayer)
            playerControllers.Remove(id);
        else
            enemyControllers.Remove(id);
    }

    public void SpawnAllCharacters()
    {
        Debug.Log($"SpawnAllCharacters called on {gameObject.name}, array length: {playerSpawnPoints.Count}");

        // hardcoding spawnpoints for now.  fix this later
        playerSpawnPoints.Add(new Vector3(3, 2.63f, -1));
        enemySpawnPoints.Add (new Vector3(40, 2.8f, 55));
        enemySpawnPoints.Add (new Vector3(-30, 2.8f, 57));
        enemySpawnPoints.Add (new Vector3(6, 2.8f, 90));

        for (int i = 0; i < playerSpawnPoints.Count; i++)
        {
            GameObject playerGO = Instantiate(assetManager.GetPlayer(), playerSpawnPoints[i], Quaternion.identity);
            PlayerMove moveScript = playerGO.GetComponent<PlayerMove>();

            RegisterPlayer(i, moveScript);
        }

        for (int i = 0; i < enemySpawnPoints.Count; i++)
        {
            GameObject enemyGO = Instantiate(assetManager.GetEnemy(), enemySpawnPoints[i], Quaternion.identity);
            EnemyMove moveScript = enemyGO.GetComponent<EnemyMove>();
            //ensure all enemies/players have unique IDs
            RegisterEnemy(i + 1000, moveScript);
        }

        Debug.Log($"Spawned {playerControllers.Count} players and {enemyControllers.Count} enemies");
    }
}

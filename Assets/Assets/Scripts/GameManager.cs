
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public AssetManager assetManager;
    public static GameManager Instance;   

    
    private Dictionary<int, PlayerMove> playerControllers = new();
    private Dictionary<int, EnemyMove> enemyControllers = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        //Cursor.lockState = CursorLockMode.Confined;        
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

    public void SpawnAllCharacters(Dictionary<int, Dictionary<Vector2Int, HexTile>> platforms)
    {
        // put the player on platform 0.   random hex tile
        Vector3 randomTilePos = HexUtils.GetRandomHexTile(platforms[0]).transform.position;
        randomTilePos.y = 2.33f;        
        GameObject player0 = AssetManager.Instance.GetPlayer(randomTilePos, Quaternion.identity);
        player0.GetComponent<PlayerController>().SetPlayerHexMap(platforms[0]);
        PlayerMove playerMoveScript = player0.GetComponent<PlayerMove>();        
        RegisterPlayer(0, playerMoveScript);  //can pass a playerID here when ready for multiplayer

        // set up the camera on Player0.
        // when implementing multiplayer, we can use isLocal to enable logic for input, attach cameras, etc.
        bool isLocal = player0.GetComponent<PlayerController>().isLocalPlayer;
        if (isLocal) { Camera.main.GetComponent<CameraLook>().SetTarget(player0.transform, player0.transform.Find("CameraLookHere")); }
        
        // put an enemy AI on platform 1.  random hex tile
        randomTilePos = HexUtils.GetRandomHexTile(platforms[1]).transform.position;
        randomTilePos.y = 2.77f;
        GameObject ai0 = AssetManager.Instance.GetAI(randomTilePos, Quaternion.identity);
        ai0.transform.name = "AI_0";
        ai0.GetComponent<AIController>().SetAIHexMap(platforms[1]);
        EnemyMove enemyMoveScript = ai0.GetComponent<EnemyMove>();        
        RegisterEnemy(0 + 1000, enemyMoveScript);  //can pass a enemyID here when ready for multiplayer
                
        // put an enemy AI on platform 2.  random hex tile
        randomTilePos = HexUtils.GetRandomHexTile(platforms[2]).transform.position;
        randomTilePos.y = 2.77f;
        GameObject ai1 = AssetManager.Instance.GetAI(randomTilePos, Quaternion.identity);
        ai1.transform.name = "AI_1";
        ai1.GetComponent<AIController>().SetAIHexMap(platforms[2]);
        enemyMoveScript = ai1.GetComponent<EnemyMove>();        
        RegisterEnemy(1 + 1000, enemyMoveScript);  //can pass a enemyID here when ready for multiplayer
        /*
        // put an enemy AI on platform 3.  random hex tile
        randomTilePos = HexUtils.GetRandomHexTile(platforms[3]).transform.position;
        randomTilePos.y = 2.77f;
        GameObject ai2 = AssetManager.Instance.GetAI(randomTilePos, Quaternion.identity);
        ai2.transform.name = "AI_2";
        ai2.GetComponent<AIController>().SetAIHexMap(platforms[3]);
        enemyMoveScript = ai2.GetComponent<EnemyMove>();        
        RegisterEnemy(2 + 1000, enemyMoveScript);  //can pass a enemyID here when ready for multiplayer   
          */
    }    
}

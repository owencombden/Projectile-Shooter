using UnityEngine;

public class Level : MonoBehaviour
{
    public int numEnemies;
    public int ammoSpawnDelay; 


    private AssetManager assetManagerScript;
    private Transform player;
    private GameObject enemy;
    private GameObject treasurePrefab;    
    private Transform playerGround;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        assetManagerScript = GameObject.Find("AssetManager").GetComponent<AssetManager>();

        //iceSheet = assetManagerScript.GetIceSheet();
        player   = assetManagerScript.GetPlayer().transform;
        //iceSheet.transform.position = new Vector3(0,0,0);        
        //player.transform.position   = new Vector3(5,2.67f,5);

        //iceSheet = assetManagerScript.GetIceSheet();
        //enemy    = assetManagerScript.GetEnemy();
        //iceSheet.transform.position = new Vector3(50,0,50);        
        //enemy.transform.position    = new Vector3(50,2.67f,50);

        //spawn a pickup prefab
        //treasurePrefab = assetManagerScript.GetTreasure();
        //Vector3 pos = new Vector3(treasurePrefab.transform.position.x + 10, treasurePrefab.transform.position.y + 2, treasurePrefab.transform.position.z);
        //Quaternion rot = treasurePrefab.transform.rotation;
        //GameObject treasure = GameObject.Instantiate(treasurePrefab, pos, rot);
        

        // we need to spawn more ammo when...
            // a timer expires?  check if ammo exists, and if not then spawn some?

        // using this to help spawn ammo.  
        Vector3 rayStart = player.position;
        Vector3 rayDir = transform.up * -1;
        Ray ray = new Ray(rayStart, rayDir);        
        float rayLength = 10f;
        RaycastHit hitData;
        if (Physics.Raycast(ray, out hitData, rayLength))
        {            
            string tag = hitData.collider.tag;
            if(tag == "Ground")
            {
                //we hit a tile, get the parent container
                playerGround = hitData.transform.parent;
                Debug.Log("Level found " + playerGround.name + " as player ground container.");
            }
        }
        else { Debug.Log(gameObject.name + " could not find the player ground!"); }
    }

    // Update is called once per frame
    void Update()
    {
        
        
    }

    public void SpawnNewPlayerAmmo()
    {
        Vector3 spawnPos = playerGround.GetChild(Random.Range(0, playerGround.childCount)).position;
        spawnPos.y += 1;
        assetManagerScript.GetAmmoSpawnPrefab(spawnPos);
    }
    
}

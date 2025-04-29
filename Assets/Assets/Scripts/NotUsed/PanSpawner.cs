using System.Collections.Generic;
using UnityEngine;

public class PanSpawner : MonoBehaviour
{
    /*
    public GameObject tile;
    public Transform parent;
    
    //2D Array of tiles
    static private int gridWidth  = 20;
    static private int gridHeight = 20;
    static private int tileWidth  = 1;
    static private int tileHeight = 1;
    public Tile[,]     allTiles   = new Tile[gridWidth,gridHeight];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Quaternion rot = transform.rotation;
        Vector3    pos = transform.position;
        // populate the tiles in a grid.  store in a 2D list structure
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                GameObject newTile = GameObject.Instantiate(tile, pos, rot, parent);
                newTile.name = "Tile " + i + ", 0, " + j;
                allTiles[i,j] = newTile.GetComponent<Tile>();
                pos.z = pos.z + tileHeight;                
            }
            pos.x = pos.x + tileWidth;
            pos.z = transform.position.z;
        }

        allTiles[4, 8].ApplyDamage(35);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    */
}

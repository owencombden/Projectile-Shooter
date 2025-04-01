using System.Collections.Generic;
using UnityEngine;

public class HexTile : MonoBehaviour
{

    public int hitPoints;

    public Renderer tileRenderer;

    public Vector2Int    gridCoords; // Axial or offset grid coordinates
    public List<HexTile> neighbors = new List<HexTile>();
    
    void Start()
    {
        // get the size of the hex prefab if needed (should be 2 X Sqrt(3)!!) 
        /*
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider)
        {
            Vector3 size = meshCollider.bounds.size;
            Debug.Log($"Hex Size: Width = {size.x}, Height = {size.z}");
        }
        else
        {
            Debug.LogWarning("No MeshCollider found on this object!");
        }
        */
    
        tileRenderer = transform.GetChild(0).GetComponent<Renderer>();
    
    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void HandleCollision(Collider other)
    {
        ApplyDamage(50);
    }


    public void ApplyDamage(int damage)
    {
        hitPoints -= damage;
        if (hitPoints <= 0){DestroyTile();}
        else {UpdateColor();}
    }

    void UpdateColor()
    {
        tileRenderer.material.color = Color.blue;
    }

    void DestroyTile()
    {
        //print("Tile " + gameObject.name + " has been destroyed.");
        Destroy(gameObject);
    }
    
}

using UnityEngine;

public class HexTile : MonoBehaviour
{
    public int hitPoints;

    public Renderer tileRenderer;  
    
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log("Tile " + gameObject.name + " was hit");
        ApplyDamage(50);
    }


    public void ApplyDamage(int damage)
    {
        hitPoints -= damage;
        //print(gameObject.name + " was hit. " + hitPoints + " hitPoints remaining.");
        if (hitPoints <= 0)
        {
            DestroyTile();
        }
        else
        {
            UpdateColor();
        }

    }

    void UpdateColor()
    {
        tileRenderer.material.color = Color.blue;
    }

    void DestroyTile()
    {
        //print("Tile " + gameObject.name + " has been destroyed.");
        Destroy(transform.parent.gameObject);
    }
    
}

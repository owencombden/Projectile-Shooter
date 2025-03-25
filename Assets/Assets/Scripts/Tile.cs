using UnityEngine;

public class Tile : MonoBehaviour
{
    public int hitPoints;

    public Renderer tileRenderer;  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
              
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
        Destroy(gameObject);
    }
}

using UnityEngine;

public class HexTileCollision : MonoBehaviour
{
    private HexTile parentScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        parentScript = transform.parent.GetComponent<HexTile>();    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        parentScript?.HandleCollision(other);
    }
}

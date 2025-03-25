using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector3 startPos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DestroyBullet();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Bullet Height: " +transform.position.y);
        
    }

    public void SetStartPos(Vector3 spawnedPos)
    {
        startPos = spawnedPos;
    }

    public Vector3 GetStartPos()
    {
        return startPos;
    }

    void DestroyBullet()
    {
        //need to move this to the asset manager!
        GameObject.Destroy(gameObject, 10f);
    }
}

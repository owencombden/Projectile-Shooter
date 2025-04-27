using UnityEngine;

public class EnemyMaster : MonoBehaviour
{
    /*
    public bool enemyDead = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }    

    public void KillEnemy(Vector3 feetPosition, Vector3 tippingAxis)
    {
        // flag the player as dead.  
        enemyDead = true;        

        //tip the player towards the water in the direction of player velocity        
        LeanTween.rotateAround(gameObject, tippingAxis, -120, 0.4f);
        Vector3 fallDestination = new Vector3(transform.position.x, transform.position.y - 5f, transform.position.z);
        LeanTween.move(gameObject, fallDestination, 0.8f).setOnComplete(DestroyEnemy);
    }

    void DestroyEnemy()
    {
        LeanTween.cancel(gameObject);
        // move this to asset manager when pooling is implemented
        GameObject.Destroy(gameObject, 1f);
    }
    */
}

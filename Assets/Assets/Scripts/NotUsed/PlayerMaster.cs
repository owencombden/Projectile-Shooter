using System.Collections.Generic;
using UnityEngine;

public class PlayerMaster : MonoBehaviour
{   
    /*
    public Camera mainCamera;
    public Transform floor;
    public int maxClipSize = 4;
    public bool playerDead = false;

    
    GameManager gameManagerScript;
    AssetManager assetManagerScript;
    private ICharacterInputProvider input;
    CharacterMotor characterMotorScript;
    PlayerShoot playerShootScript;
    CharacterController playerController;
    Queue<string> playerAmmoClip = new Queue<string>();
    Transform lastGoodTile;
    
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mainCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
        gameManagerScript = GameObject.Find("GameManager").GetComponent<GameManager>();
        assetManagerScript = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        characterMotorScript = gameObject.GetComponent<CharacterMotor>();
        playerShootScript = gameObject.GetComponent<PlayerShoot>();
        playerController = gameObject.GetComponent<CharacterController>();

        //start with some ammo
        for (int i = 0; i < 4; i++)
        {
            playerAmmoClip.Enqueue("normal");
        }
                       
    }

    // Update is called once per frame
    void Update()
    {
        if (playerDead) { return; }

        // if player is tweening (edge avoidance, rotation), no further movement or shooting
        if(LeanTween.isTweening(gameObject)){ return;}

        // get current ground
        // use SphereCast so we can ignore tiny gaps in the floor tiles                   
        Vector3 rayStart  = transform.position;
        float   rayRadius = 0.1f;
        Vector3 rayDir    = transform.up * -1;                      
        float   rayLength = 5f;        
        string  tag       = "";
        RaycastHit hitData;        
        if (Physics.SphereCast(rayStart, rayRadius, rayDir, out hitData, rayLength))
        {
            tag = hitData.transform.tag;
        }
        else
        {
            Debug.Log("Could not find ground!");
            return;
        }

        if(tag == "Ground")
        {
            lastGoodTile = hitData.transform;
        }

        // check for kill player
        if(tag == "Water")
        {
            TweenBackFromEdge();            
            //KillPlayer(groundHit.point, Vector3.Cross(playerMoveScript.controller.velocity, transform.up));
            return;
        }
    }    

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Player is unexpectedly colliding with " + other.tag + ".  Fix this in Physics Layers!!");        
    }

    public void ReloadGun(List<string> bulletTypes)
    {
        // track bullet types as strings in the player's clip.
        // these are instantiated as a 'type' when shooting        

        // add bullets to the chamber until full
        // add whatever type is desired.  needs to have a matching string in AssetManager.GetPlayerBullet() and prefab for spawning that type.
        int numToAdd  = maxClipSize - playerAmmoClip.Count;
        Debug.Log("Current clip count: " + playerAmmoClip.Count);        
        for (int i = 0; i < numToAdd; i++)
        {
            playerAmmoClip.Enqueue(bulletTypes[i]);
        }
        Debug.Log("Player found ammo.  Loading " + numToAdd + " bullets.  Player now has " + playerAmmoClip.Count + " total.");
    }

    public bool GetNextBullet(Vector3 spawnPos, out GameObject bullet)
    {
        if(playerAmmoClip.Count > 0)
        {
            // get and remove the bullet-type(string) from the clip
            // spawn and return a bullet of that type to the Shoot script
            string bulletType = playerAmmoClip.Dequeue();
            bullet = assetManagerScript.GetBullet(spawnPos); 
            return true; 
        }
        else
        {
            bullet = null; 
            return false; 
        }
    }

    private void TweenBackFromEdge()
    {
        Vector3 currentVelocity = playerController.velocity.normalized;
        currentVelocity.y = 0f;
        Vector3 destination = lastGoodTile.position;
        destination.y = transform.position.y;
        LeanTween.move(gameObject, destination, 0.2f).setOnComplete(CheckIfGrounded);
    }

    private void CheckIfGrounded()
    {
        // if the target tile has been destroyed beneath the player, kill the player
        // get current ground
        RaycastHit hitData;              
        Vector3 rayStart = transform.position;
        Vector3 rayDir   = transform.up * -1;               
        float  rayLength = 5;
        Ray    ray       = new Ray(rayStart, rayDir);
        string tag       = "";
        
        if(Physics.Raycast(ray, out hitData, rayLength))
        {
            tag = hitData.transform.tag;
            if(tag != "Ground")
            {
                //KillPlayer(hitData.point, Vector3.Cross(characterMotorScript.controller.velocity, transform.up));
            }
        }
    }

    public void KillPlayer(Vector3 feetPosition, Vector3 tippingAxis)
    {
        // flag the player as dead.  Uncouple and deactivate the camera
        playerDead = true;
        mainCamera.transform.parent = null;  //needed?

        //tip the player towards the water in the direction of player velocity        
        LeanTween.rotateAround(gameObject, tippingAxis, -120, 0.4f);
        Vector3 fallDestination = new Vector3(transform.position.x, transform.position.y - 2f, transform.position.z);
        LeanTween.move(gameObject, fallDestination, 0.8f)
                 .setOnComplete(restartGame);
    }

    private void restartGame()
    {
        gameManagerScript.ReloadScene();
    }    

    */
}

using System.Collections.Generic;
using UnityEngine;

public class PlayerMaster : MonoBehaviour
{   
    public Camera mainCamera;
    public Transform floor;
    public int maxClipSize;
    public bool playerDead = false;

    
    GameManager gameManagerScript;
    AssetManager assetManagerScript;
    PlayerInputHandler inputHandlerScript; 
    PlayerMove playerMoveScript;
    PlayerShoot playerShootScript;
    Queue<string> playerAmmoClip = new Queue<string>();
    
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManagerScript = GameObject.Find("GameManager").GetComponent<GameManager>();
        assetManagerScript = GameObject.Find("AssetManager").GetComponent<AssetManager>();
        inputHandlerScript = gameObject.GetComponent<PlayerInputHandler>();
        playerMoveScript = gameObject.GetComponent<PlayerMove>();
        playerShootScript = gameObject.GetComponent<PlayerShoot>();

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

        // get current ground
        RaycastHit groundHit;
        string tag = CheckForGround(out groundHit);

        // check for kill player
        if(tag == "Water")
        {            
            KillPlayer(groundHit.point, Vector3.Cross(playerMoveScript.controller.velocity, transform.up));
            return;
        }

        // if player is tweening a rotation, no further movement or shooting
        if(LeanTween.isTweening(gameObject)){ return;}

        
        if(tag == "Ground")
        {
            
        }

        // handle movement and rotation
        Vector2 moveInput = inputHandlerScript.GetMovementInput();
        Vector2 lookInput = inputHandlerScript.GetLookInput();
        if ( moveInput.magnitude > 0.1f )
        {
            playerMoveScript.MovePlayer(moveInput);
        }

        //handle shooting from the hip
        if(inputHandlerScript.GetShootFromTheHipInput()) {playerShootScript.ShootFromTheHip();}
        
        //handle autoshooting (click-to-shoot at target)
        else if (inputHandlerScript.GetShootAtTargetInput())
        { 
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // the object identified by hit.transform was clicked
            if (Physics.Raycast(ray, out hit))
            {
                // ignore clicks on water/floor
                if(hit.transform.tag == "Water") { return; }  

                playerMoveScript.RotatePlayerToTarget(hit);                
            }
        }


    }

    private string CheckForGround(out RaycastHit hitData)
    {
        //check for ground
        Vector3 rayStart = transform.position;
        Vector3 rayDir = transform.up * -1;
        Ray ray = new Ray(rayStart, rayDir);        
        float rayLength = playerMoveScript.controller.height/2 + 5;
        
        //RaycastHit hitInfo;
        Physics.Raycast(ray, out hitData, rayLength);
        return hitData.transform.tag;
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
            bullet = assetManagerScript.GetPlayerBullet(spawnPos, bulletType); 
            return true; 
        }
        else
        {
            bullet = null; 
            return false; 
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
        gameManagerScript.reloadScene();
    }    
}

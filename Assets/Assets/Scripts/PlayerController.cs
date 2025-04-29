using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    // this will be used later during multiplayer.
    public int id { get; private set; }
    public bool isLocalPlayer = true;

    public bool playerDead = false;
    
    private CharacterMotor motor;
    private CharacterShooter shooter;
    private ICharacterInputProvider input;
    Transform lastGoodTile;
    GameManager gameMgr;
    Dictionary<Vector2Int, HexTile> hexMap;  // the collection of tiles that the Player is standing on

    private void Awake()
    {
        motor = GetComponent<CharacterMotor>();
        motor.SetCamera(Camera.main.transform);   // Only needed for player
        input = GetComponent<ICharacterInputProvider>();

       
    }

    private void Start()
    {
        gameMgr = GameObject.Find("GameManager").GetComponent<GameManager>();
        shooter = GetComponent<CharacterShooter>();   
    }

    private void Update()
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

        // check for player over water
        if(tag == "Water")
        {
            TweenBackFromEdge();            
            //KillPlayer(groundHit.point, Vector3.Cross(playerMoveScript.controller.velocity, transform.up));
            return;
        }

        if(tag == "Ground")
        {
            lastGoodTile = hitData.transform;
        }        

        // check for movement.
        Vector2 moveInput = input.MoveInput;
        if ( moveInput.magnitude > 0.1f ) { motor.Move(moveInput); }

        //check for shooting
        if (input.ShootAtTarget) 
        { 
            Transform clickedTarget = GetMouseClickTarget();
            if (clickedTarget != null)
            {
                // ignore clicks on water/floor
                if(clickedTarget.tag == "Water") { return; }  

                motor.RotateToTargetAndShoot(clickedTarget); 
            }
        }
    }

    private void TweenBackFromEdge()
    {
        Vector3 currentVelocity = motor.GetVelocity(true);
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
                KillPlayer(hitData.point, Vector3.Cross(motor.GetVelocity(false), transform.up));
            }
        }
    }

    private Transform GetMouseClickTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit)) { return hit.transform; }
        return null;
    }

    public void SetPlayerHexMap(Dictionary<Vector2Int, HexTile> platform)
    {
        hexMap = platform;
    }

    public void Set_ID(int character_id)
    {
        id = character_id;        
    }

    public void KillPlayer(Vector3 feetPosition, Vector3 tippingAxis)
    {
        // flag the player as dead.
        playerDead = true;

        gameMgr.RemoveCharacter(id, true);

        //tip the player towards the water in the direction of player velocity        
        LeanTween.rotateAround(gameObject, tippingAxis, -120, 0.4f);
        Vector3 fallDestination = new Vector3(transform.position.x, transform.position.y - 2f, transform.position.z);
        LeanTween.move(gameObject, fallDestination, 0.8f);
    }      
}
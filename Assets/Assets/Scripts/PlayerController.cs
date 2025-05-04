using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
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
        gameMgr = GameObject.Find("GameManager").GetComponent<GameManager>();
        shooter = GetComponent<CharacterShooter>();   
    }
    

    private void Update()
    {
        if (playerDead) { return; }        

        // get current ground
        // use SphereCast so we can ignore tiny gaps in the floor tiles
        string tag = "";
        if (TryGetGroundHit(out RaycastHit hitData))
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

                // Shoot!
                StartCoroutine(Shoot(clickedTarget));
                
                // Cooldown wait
                //yield return new WaitForSeconds(shootCooldown);  // this is being done in tryshoot
            }
        }
    }

    private void TweenBackFromEdge()
    {
        Vector3 currentVelocity = motor.GetVelocity(true);
        currentVelocity.y = 0f;
        Vector3 destination = lastGoodTile.position;
        destination.y = transform.position.y;
        StartCoroutine(MoveToPosition(destination, 0.2f));
    }

    private IEnumerator MoveToPosition(Vector3 destination, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, destination, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = destination;
        CheckIfGrounded();
    }

    private IEnumerator Shoot(Transform target)
    {
        Debug.Log("this ran");
        yield return StartCoroutine(motor.RotateTowardTargetAndShoot(target));
    }

    private void CheckIfGrounded()
    {
        // if the target tile has been destroyed beneath the player, kill the player
        // get current ground
        // use SphereCast so we can ignore tiny gaps in the floor tiles
        //RaycastHit hitData;        
        if (TryGetGroundHit(out RaycastHit hitData))
        {
            if(hitData.transform.tag != "Ground")
            {
                KillPlayer(hitData.point, Vector3.Cross(motor.GetVelocity(false), transform.up));
            }
        }
    }

    private bool TryGetGroundHit(out RaycastHit hitData)
    {
        Vector3 rayStart = transform.position;
        float rayRadius = 0.1f;
        Vector3 rayDir = Vector3.down;
        float rayLength = 5f;

        return Physics.SphereCast(rayStart, rayRadius, rayDir, out hitData, rayLength);
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
        StartCoroutine(FallOver(tippingAxis));
    }

    private IEnumerator FallOver(Vector3 tippingAxis)
    {
        float duration = 0.4f;
        float angle = -120f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.AngleAxis(angle, tippingAxis) * startRot;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = endRot;

        Vector3 fallDestination = new Vector3(transform.position.x, transform.position.y - 2f, transform.position.z);
        float fallDuration = 0.8f;
        elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < fallDuration)
        {
            transform.position = Vector3.Lerp(startPos, fallDestination, elapsed / fallDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = fallDestination;
    }      
}
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{    
    public NetworkVariable<ulong> id;
    public bool playerDead = false;
    
    private CharacterMotor motor;
    private CharacterShooter shooter;
    private ICharacterInputProvider input;
    private Vector3 lastGroundPos;
    private bool isAvoidingWater = false;
    Dictionary<Vector2Int, HexTile> hexMap;  // the collection of tiles that the Player is standing on

    public override void OnNetworkSpawn()
    {        
        if (!IsOwner) return;
        
        var camera = Camera.main;
        if (camera != null && camera.TryGetComponent(out CameraLook lookScript))
        {
            lookScript.SetTarget(transform, transform.Find("CameraLookHere"));
        }
        motor = GetComponent<CharacterMotor>();
        motor.SetCamera(camera.transform);   // Only needed for player
        input = GetComponent<ICharacterInputProvider>();
        shooter = GetComponent<CharacterShooter>();
        lastGroundPos = transform.position;   
    }
    
    void Awake()
    {
        //Debug.Log($"Player prefab instantiated at runtime! Scene: {gameObject.scene.name} | Time: {Time.time}");
    }

    void Start()
    {   
        PlayerInputHandler handler = GetComponent<PlayerInputHandler>();
        handler.OnShootClicked += HandleShootClicked;   
    }

    private void Update()
    {
        if (!IsOwner || playerDead) return;

        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
        return;

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
        if (tag == "Water")
        {
            if (!isAvoidingWater)
            {
                isAvoidingWater = true;
                lastGroundPos.y = transform.position.y;
                //Debug.Log($"Player is about to step in water!  Moving back to {lastGroundPos}");
                StartCoroutine(TryToAvoidWater(lastGroundPos, 0.2f));
            }
            return;
        }

        if (tag == "Ground")
        {
            lastGroundPos = hitData.transform.position;
        }

        // check for movement.
        Vector2 moveInput = input.MoveInput;
        if (moveInput.magnitude > 0.1f) { motor.Move(moveInput); }

        //check for shooting (using the event in PlayerInputHandler instead...testing)
        /*
        if (input.ShootAtTarget)
        {
            Transform clickedTarget = GetMouseClickTarget();
            if (clickedTarget != null)
            {
                // ignore clicks on water/floor
                if (clickedTarget.tag == "Water") { return; }

                // ignore clicks on players own platform (for now...set up move to position here?)
                if (clickedTarget.tag == "Ground" && IsOwnPlatform(clickedTarget)) { return; }

                // Shoot!
                //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} clicked on something.");
                StartCoroutine(Shoot(clickedTarget));
            }
        }
        */
    }

    private void HandleShootClicked()
    {
        if (!IsOwner || playerDead) return;
        
        Transform clickedTarget = GetMouseClickTarget();
        if (clickedTarget == null) return;
        if (clickedTarget.tag == "Water") return;
        if (clickedTarget.tag == "Ground" && IsOwnPlatform(clickedTarget)) return;

        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} clicked on something.");
        StartCoroutine(Shoot(clickedTarget));
    }

    private bool IsOwnPlatform(Transform clickedTarget)
    {
        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is checking if isOwnPlatform.");
        return clickedTarget.GetComponentInParent<HexTile>().ownerId == id.Value;
    }

    private IEnumerator TryToAvoidWater(Vector3 destination, float duration)
    {
        // move to position (in duration)
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, destination, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = destination;

        // if still not grounded, KillPlayer
        if (TryGetGroundHit(out RaycastHit hitData))
        {
            if(hitData.transform.tag != "Ground")
            {
                KillPlayer(hitData.point, Vector3.Cross(motor.GetVelocity(false), transform.up));
            }
        }

        // we've finished avoiding the water
        isAvoidingWater = false;
    }

    private IEnumerator Shoot(Transform target)
    {

        // wait until body rotates to face target
        yield return StartCoroutine(motor.RotateTowardTarget(target));

        //check if can shoot
        var shooter = transform.GetComponent<CharacterShooter>();
        if (shooter.GetCurrentAmmo() <= 0)
        {
            Debug.Log($"NO SHOT.  Client {NetworkManager.Singleton.LocalClientId} has no ammo.");
            yield break;
        }
        if (Time.time - shooter.lastShootTime < shooter.shootCooldown)
        {
            Debug.Log($"NO SHOT.  Client {NetworkManager.Singleton.LocalClientId} is still in cooldown.");
            yield break;
        }    
        shooter.lastShootTime = Time.time;

        // check target tag
        if (target.tag == "Ground" || target.tag == "Player" || target.tag == "AI_Player")
        {
            float fineTune = 0.97f;  //finetune range if needed
            float distToTarget = Vector3.Distance(transform.position, target.transform.position) * fineTune;

            // calculate the angle, rotate the gun to position, get the required speed            
            float shotSpeed = shooter.AimAtTarget(distToTarget, shooter.gun);
            Vector3 shotVelocity = shotSpeed * shooter.spawnpoint.forward;
            Debug.Log($"Shot speed: {shotSpeed}    Shot velocity: {shotVelocity}");

            // request server to shoot in the direction the gun is pointing
            Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is requesting a shot from the server.");
            shooter.SpawnBulletServerRPC(id.Value, shooter.spawnpoint.position, shotVelocity);

            //PauseManager.Instance.TogglePauseServerRpc();
        }
        else if (target.tag == "Bullet")
        {
            Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is trying to shoot at a bullet.");
            Vector3 lookDir = shooter.AimAtEnemyBullet(target);

            if (lookDir == Vector3.zero)
            {
                Debug.Log($"NO SHOT. Client {NetworkManager.Singleton.LocalClientId} couldn't get a tracking vector.");
                yield break;
            }

            float angleDist = Vector3.Angle(transform.forward, lookDir);
            float rotateTime = angleDist / (2f * 200f);

            Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is tracking the bullet.");
            StartCoroutine(shooter.RotateToFuturePos(lookDir, rotateTime));

            Vector3 shotVelocity = shooter.ShootAtEnemyBullet();

            Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is shooting at the bullet.");
            shooter.SpawnBulletServerRPC(id.Value, shooter.spawnpoint.position, shotVelocity);
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

    public void ApplyBlastForce(Vector3 direction, float force)
    {
        if (playerDead) return;

        // Optionally, cancel player movement or shooting here

        // could tie duration to characterWeight and use it to adjust the knockback?
        float duration = 1.25f;
        motor.ApplyBlastForce(direction, force, duration);
    }

    private ulong GetMyId()
    {
        if (transform.tag == "Player")
        {
            return transform.GetComponent<PlayerController>().id.Value;
        }
        else if (transform.tag == "AI_Player")
        {
            return transform.GetComponent<AIController>().id.Value;
        }
        else { Debug.Log("Could not get ownerID!"); }

        return ulong.MaxValue;
    }

    public void SetPlayerHexMap(Dictionary<Vector2Int, HexTile> platform)
    {
        hexMap = platform;
    }

    public void KillPlayer(Vector3 feetPosition, Vector3 tippingAxis)
    {
        // flag the player as dead.
        playerDead = true;
        LevelManager.Instance.RemoveCharacter(id.Value, true);        

        //tip the player towards the water in the direction of player velocity        
        StartCoroutine(FallOver(tippingAxis));

        // Tell Netcode to despawn this player (but we're pooling so don’t destroy the GameObject!!)
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(destroy: false);
        }

        // return the player to the pool
        AssetManager.Instance.ReturnPlayer(gameObject);
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
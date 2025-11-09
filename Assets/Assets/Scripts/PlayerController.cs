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
    private PlayerInputHandler handler;
    private Vector3 lastGroundPos;
    private Vector3 storedTippingAxis;
    public bool canFallAndDie = true;  // from GameSettings
    private bool isAvoidingWater = false;
    public bool isBeingKnockedBack = false;
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
        handler = GetComponent<PlayerInputHandler>();
        handler.OnShootClicked += HandleShootClicked;
    }

    private void Update()
    {
        if (!IsOwner || playerDead) return;

        if (GameManager.Instance.GetGameState() == GameManager.GameState.GameOver) return;

        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
            return;

        if (isBeingKnockedBack) return;

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
            if (isAvoidingWater) return;
            
            if (canFallAndDie)
            {
                KillPlayer(Vector3.Cross(motor.GetVelocity(false), transform.up));
            }
            else
            {
                isAvoidingWater = true;
                lastGroundPos.y = transform.position.y;
                //Debug.Log($"Player is about to fall off platform!  Moving back to {lastGroundPos}");
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
        motor.Move(moveInput);

        //checking for shooting using the event in PlayerInputHandler

    }

    private void HandleShootClicked(Transform target)
    {

        //Debug.Log("Player Controller has detected that shoot was clicked.");
        if (GameManager.Instance.GetGameState() == GameManager.GameState.GameOver) return;
        if (PauseManager.Instance.isPaused.Value) { return; }
        if (!IsOwner || playerDead) return;

        // get the transform that was hit
        //Debug.Log($"Player will shoot at {target.tag}");

        // filter the things we don't want to hit
        if (target == null) return;
        if (target.tag == "Water") return;
        if (target.tag == "Ground" && IsOwnPlatform(target)) return;

        StartCoroutine(Shoot(target));
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
            if (hitData.transform.tag != "Ground")
            {
                KillPlayer(Vector3.Cross(motor.GetVelocity(false), transform.up));
            }
        }

        // we've finished avoiding the water
        isAvoidingWater = false;
    }

    private IEnumerator Shoot(Transform target)
    {
        //check if can shoot
        var shooter = transform.GetComponent<CharacterShooter>();
        if (shooter.GetCurrentAmmo() <= 0)
        {
            //Debug.Log($"NO SHOT.  Client {NetworkManager.Singleton.LocalClientId} has no ammo.");
            GameplayUI.Instance.DisplayNoAmmoMessage();
            yield break;
        }
        if (Time.time - shooter.lastShootTime < shooter.shootCooldown)
        {
            //Debug.Log($"NO SHOT.  Client {NetworkManager.Singleton.LocalClientId} is still in cooldown.");
            yield break;
        }
        shooter.lastShootTime = Time.time;

        // wait until body rotates to face target
        yield return StartCoroutine(motor.RotateTowardTarget(target));

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
            //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is requesting a shot from the server.");
            shooter.SpawnBulletServerRPC(GetComponent<NetworkObject>(), id.Value, shooter.spawnpoint.position, shotVelocity);

            //PauseManager.Instance.TogglePauseServerRpc();
        }
        else if (target.tag == "Bullet")
        {
            //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is trying to shoot at a bullet.");
            Vector3 lookDir = shooter.AimAtEnemyBullet(target);

            if (lookDir == Vector3.zero)
            {
                //Debug.Log($"NO SHOT. Client {NetworkManager.Singleton.LocalClientId} couldn't get a tracking vector.");
                yield break;
            }

            float angleDist = Vector3.Angle(transform.forward, lookDir);
            float rotateTime = angleDist / (2f * 300f);

            //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is tracking the bullet.");
            StartCoroutine(shooter.RotateToFuturePos(lookDir, rotateTime));

            Vector3 shotVelocity = shooter.ShootAtEnemyBullet();

            //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is shooting at the bullet.");
            shooter.SpawnBulletServerRPC(GetComponent<NetworkObject>(), id.Value, shooter.spawnpoint.position, shotVelocity);
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

    [ClientRpc]
    public void ReceiveKnockbackBlastClientRpc(Vector3 direction, float force, ClientRpcParams rpcParams = default)
    {
        // Ensure only the owner runs this
        if (!IsOwner) return;

        if (playerDead) return;

        isBeingKnockedBack = true;

        float duration = 1.25f;
        motor.ApplyBlastForce(direction, force, duration, true);
    }

    public void SetPlayerHexMap(Dictionary<Vector2Int, HexTile> platform)
    {
        hexMap = platform;
    }

    public void KillPlayer(Vector3 tippingAxis)
    {
        if (playerDead) return;

        // flag the player as dead.
        playerDead = true;

        // Store tipping axis so server can use it too
        storedTippingAxis = tippingAxis;

        // Tell the server to handle the rest (despawn, return to pool)
        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId}) submitting death to ServerRPC");
        GameManager.Instance.SubmitDeathServerRpc(tippingAxis);
    }

    public IEnumerator FallOver(Vector3 tippingAxis)
    {
        // currently the NetworkObject is set to synch x,y,z pos, and x,y,z rot to allow this animation
        // network optomization is available here.
        // should look at only synching x,z pos and y rot...
        // ...could 'hide' the player prefab on the network and have each client locally spawn/animate a dummy.

        //Debug.Log($"Client {NetworkManager.Singleton.LocalClientId} is running death animation on Client {id.Value}");

        float duration = 0.2f;
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

        Vector3 fallDestination = new Vector3(transform.position.x, transform.position.y - 6f, transform.position.z);
        float fallDuration = 0.5f;
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

    public void TellClientToShowYouLostBanner()
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        ShowYouLostBannerClientRpc(rpcParams);
    }

    [ClientRpc]
    private void ShowYouLostBannerClientRpc(ClientRpcParams rpcParams = default)
    {
        //Debug.Log($"Displaying 'you lost' banner for player {id}");
        // Only executes on the targeted client’s PlayerController
        string message = "Better Luck Next Time!";        
        GameplayUI.Instance.DisplayGameOverMessage(message);
    }

    public void TellClientToShowYouWonBanner()
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        ShowYouWonBannerClientRpc(rpcParams);
    }

    [ClientRpc]
    private void ShowYouWonBannerClientRpc(ClientRpcParams rpcParams = default)
    {
        //Debug.Log($"Displaying 'you won' banner for player {id}");

        // Only executes on the targeted client’s PlayerController
        string message = "You are the winner!";
        GameplayUI.Instance.DisplayGameOverMessage(message);
    }

}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

[RequireComponent(typeof(CharacterMotor), typeof(CharacterShooter), typeof(AIInputHandler))]
public class AIController : NetworkBehaviour
{
    public float rotationSpeed = 360f; // degrees per second

    [SerializeField] private float waypointThreshold = 0.5f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float pickupCheckInterval = 1.0f; // seconds between pickup scans
    private float pickupCheckCooldown = 0f;
    private GameObject cachedNearestAmmo = null;
    public NetworkVariable<ulong> id;

    private CharacterMotor motor;
    private CharacterShooter shooter;
    private AIInputHandler input;

    private Dictionary<Vector2Int, HexTile> hexMap;

    private Vector3 currentDestination;
    private bool isWaiting = false;
    bool isDead = false;


    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;   // only the server runs AI logic
        
        motor = GetComponent<CharacterMotor>();
        shooter = GetComponent<CharacterShooter>();
        input = GetComponent<AIInputHandler>();
    }
    

    private void Update()
    {
        if (!IsServer) return;   // only the server runs AI logic
        
        if (isDead) return;

        if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value)
        return;

        // Check ground
        if (TryGetGroundHit(out RaycastHit hitData))
        {
            string hitTag = hitData.collider.tag;
            if (hitTag == "Water")
            {
                Vector3 currentVelocity = (currentDestination - transform.position).normalized;
                KillEnemy(hitData.point, Vector3.Cross(currentVelocity, transform.up));
                return;
            }
            else if (hitTag == "Ground")
            {
                motor.Move(input.MoveInput);
            }
        }

        if (isWaiting || hexMap == null || hexMap.Count == 0) return;

        //  prioritize ammo seeking if empty
        if (shooter != null && shooter.GetCurrentAmmo() <= 0)
        {
            pickupCheckCooldown -= Time.deltaTime;
            if (pickupCheckCooldown <= 0f)
            {
                pickupCheckCooldown = pickupCheckInterval;
                cachedNearestAmmo = FindClosestPickup("Ammo");
            }

            if (cachedNearestAmmo != null)
            {
                float distToAmmo = Vector3.Distance(transform.position, cachedNearestAmmo.transform.position);
                if (distToAmmo > waypointThreshold)
                {
                    currentDestination = cachedNearestAmmo.transform.position;
                    input.SetMoveTarget(currentDestination);
                    return;
                }
                else
                {
                    input.ClearMoveTarget();
                    return;
                }
            }
        }

        if (!ReachedDestination())
        {
            input.SetMoveTarget(currentDestination);
            return;
        }

        input.ClearMoveTarget();
        StartCoroutine(ShootThenMove());
    }

    private bool TryGetGroundHit(out RaycastHit hitData)
    {
        Vector3 rayStart = transform.position;
        float rayRadius = 0.1f;
        Vector3 rayDir = Vector3.down;
        float rayLength = 5f;

        return Physics.SphereCast(rayStart, rayRadius, rayDir, out hitData, rayLength);
    }

    private GameObject FindClosestPickup(string tag)
    {
        GameObject[] pickups = GameObject.FindGameObjectsWithTag(tag);
        GameObject closest = null;
        float minDist = Mathf.Infinity;
        Vector3 currentPos = transform.position;

        foreach (GameObject pickup in pickups)
        {
            float dist = Vector3.Distance(currentPos, pickup.transform.position);
            if (dist < minDist)
            {
                closest = pickup;
                minDist = dist;
            }
        }

        return closest;
    }    

    private bool ReachedDestination()
    {
        Vector3 flatDelta = currentDestination - transform.position;
        flatDelta.y = 0f;
        return flatDelta.magnitude <= waypointThreshold;
    }

    private IEnumerator ShootThenMove()
    {
        isWaiting = true;

        Transform target = GetRandomTarget();
        if (target != null)
        {
            Debug.Log($"AI {id.Value} is attempting a shot.");

            // rotate body
            yield return StartCoroutine(motor.RotateTowardTarget(target));

            //check if can shoot
            var shooter = transform.GetComponent<CharacterShooter>();
            if (shooter.GetCurrentAmmo() <= 0)
            {
                Debug.Log($"NO SHOT.  AI {id.Value} has no ammo.");
                yield break;
            }
            if (Time.time - shooter.lastShootTime < shooter.shootCooldown)
            {
                Debug.Log($"NO SHOT.  AI is still in cooldown.");
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
                Debug.Log($"AI {id.Value} is requesting a shot from the server.");
                shooter.SpawnBulletServerRPC(id.Value, shooter.spawnpoint.position, shotVelocity);

                //PauseManager.Instance.TogglePauseServerRpc();
            }
            else if (target.tag == "Bullet")
            {
                Debug.Log($"AI {id.Value} is trying to shoot at a bullet.");
                Vector3 lookDir = shooter.AimAtEnemyBullet(target);

                if (lookDir == Vector3.zero)
                {
                    Debug.Log($"NO SHOT. AI  {id.Value} couldn't get a tracking vector.");
                    yield break;
                }

                float angleDist = Vector3.Angle(transform.forward, lookDir);
                float rotateTime = angleDist / (2f * 200f);

                Debug.Log($"AI {id.Value} is tracking the bullet.");
                StartCoroutine(shooter.RotateToFuturePos(lookDir, rotateTime));

                Vector3 shotVelocity = shooter.ShootAtEnemyBullet();

                Debug.Log($"AI {id.Value} is shooting at the bullet.");
                shooter.SpawnBulletServerRPC(id.Value, shooter.spawnpoint.position, shotVelocity);
            }   
        }

        // Pick a new destination
        currentDestination = GetRandomPosition(hexMap);

        // Move while waiting
        input.SetMoveTarget(currentDestination);
        float elapsed = 0f;
        while (elapsed < shootCooldown && !ReachedDestination())
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        input.ClearMoveTarget();
        isWaiting = false;
    }

    private Vector3 GetRandomPosition(Dictionary<Vector2Int, HexTile> map)
    {
        HexTile tile = HexUtils.GetRandomHexTile(map);
        if (tile == null) return transform.position;

        Vector3 pos = tile.transform.position;
        pos.y = transform.position.y; // Keep AI on same height
        return pos;
    }

    private Transform GetRandomTarget()
    {
        List<Targetable> potentialTargets = TargetManager.Instance.GetTargets();

        // Filter out self, dead targets, and own-bullets
        potentialTargets.RemoveAll(t => t == null || !t.isActiveAndEnabled || t.gameObject == this.gameObject || (t.transform.GetComponent<Bullet>() && t.transform.GetComponent<Bullet>().ownerId == id.Value));
        //  || (t.transform.GetComponent<Bullet>() && t.transform.GetComponent<Bullet>().ownerId == id)
        
        if (potentialTargets.Count == 0)
        {
            Debug.Log(transform.name + " could not find a target!!  Null returned!");
            return null;
        } 

        int index = Random.Range(0, potentialTargets.Count);
        
        return potentialTargets[index].transform;
    }

    public void SetAIHexMap(Dictionary<Vector2Int, HexTile> platform)
    {
        hexMap = platform;

        // Immediately pick a starting destination
        currentDestination = GetRandomPosition(hexMap);
    }

    public void KillEnemy(Vector3 feetPosition, Vector3 tippingAxis)
    {
        isDead = true;
        LevelManager.Instance.RemoveCharacter(id.Value, false);        

        StartCoroutine(TipAndFall(tippingAxis));        
    }

    private IEnumerator TipAndFall(Vector3 tippingAxis)
    {
        // Rotate over 0.4 seconds
        float duration = 0.4f;
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.AngleAxis(-120, tippingAxis) * startRot;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }

        // Move down over 0.8 seconds
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 5f;
        float moveDuration = 0.8f;
        float moveElapsed = 0f;

        while (moveElapsed < moveDuration)
        {
            moveElapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, moveElapsed / moveDuration);
            yield return null;
        }

        DestroyEnemy();
    }

    void DestroyEnemy()
    {
        // Tell Netcode to despawn this player (but we're pooling so don’t destroy the GameObject!!)
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(destroy: false);
        }

        // return the AI to the pool
        AssetManager.Instance.ReturnAI(gameObject);
    }
}

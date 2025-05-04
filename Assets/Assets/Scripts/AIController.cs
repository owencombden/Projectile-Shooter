using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterMotor), typeof(CharacterShooter), typeof(AIInputHandler))]
public class AIController : MonoBehaviour
{
    public float rotationSpeed = 360f; // degrees per second

    [SerializeField] private float waypointThreshold = 0.5f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float pickupCheckInterval = 1.0f; // seconds between pickup scans
    private float pickupCheckCooldown = 0f;
    private GameObject cachedNearestAmmo = null;

    public int id { get; private set; }
    
    private CharacterMotor motor;
    private CharacterShooter shooter;
    private AIInputHandler input;

    private Dictionary<Vector2Int, HexTile> hexMap;

    private Vector3 currentDestination;
    private bool isWaiting = false;
    bool isDead = false;

    private void Awake()
    {
        //Debug.Log("AI was added to pool with position: " + transform.position);
        motor = GetComponent<CharacterMotor>();
        shooter = GetComponent<CharacterShooter>();
        input = GetComponent<AIInputHandler>();
    }

    private void Update()
    {
        if (isDead) return;

        // Check ground
        Vector3 rayStart = transform.position;
        float rayRadius = 0.1f;
        Vector3 rayDir = Vector3.down;
        float rayLength = 5f;
        RaycastHit hitData;
        string tag = "";

        if (Physics.SphereCast(rayStart, rayRadius, rayDir, out hitData, rayLength))
        {
            tag = hitData.collider.tag;
            if (tag == "Water")
            {
                Vector3 currentVelocity = (currentDestination - transform.position).normalized;
                KillEnemy(hitData.point, Vector3.Cross(currentVelocity, transform.up));
                return;
            }
            else if (tag == "Ground")
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

    private GameObject FindClosestPickup(string tag)
    {
        Debug.Log($"{gameObject.name} is looking for a {tag} pickup.");
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

    // for debug only right now
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log($"AIController reported: {transform.name} hit {hit.gameObject.name} at {hit.point}");
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
        yield return StartCoroutine(motor.RotateTowardTargetAndShoot(target));
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

        // Filter out self and dead targets
        potentialTargets.RemoveAll(t => t == null || !t.isActiveAndEnabled || t.gameObject == this.gameObject);

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

    public void Set_ID(int character_id)
    {
        id = character_id;        
    }

    public void KillEnemy(Vector3 feetPosition, Vector3 tippingAxis)
    {
        isDead = true;
        GameManager.Instance.RemoveCharacter(id, false);
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
        // move this to asset manager when pooling is implemented
        if (AssetManager.Instance)
        {
            AssetManager.Instance.ReturnAI(gameObject);
        }
        else { Debug.Log("DestroyEnemy cannot find AssetManager!!"); }
        
    }
}

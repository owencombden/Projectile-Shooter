using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterMotor), typeof(CharacterShooter), typeof(AIInputHandler))]
public class AIController : MonoBehaviour
{
    public float rotationSpeed = 360f; // degrees per second

    [SerializeField] private float waypointThreshold = 0.5f;
    [SerializeField] private float shootCooldown = 2f;

    public int id { get; private set; }
    
    private CharacterMotor motor;
    private CharacterShooter shooter;
    private AIInputHandler input;

    private Dictionary<Vector2Int, HexTile> hexMap;

    private Vector3 currentDestination;
    private bool isWaiting = false;
    bool enemyDead = false;

    private void Awake()
    {
        //Debug.Log("AI was added to pool with position: " + transform.position);
        motor = GetComponent<CharacterMotor>();
        shooter = GetComponent<CharacterShooter>();
        input = GetComponent<AIInputHandler>();
    }

    private void Update()
    {

        if (enemyDead) return;

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
            tag = hitData.collider.tag;
            if(tag == "Water")
            {
                //enemy has fallen in the water
                Vector3 currentVelocity = (currentDestination - transform.position).normalized;
                KillEnemy(hitData.point, Vector3.Cross(currentVelocity, transform.up));
                return;
            }
            else if(tag == "Ground")
            {
                motor.Move(input.MoveInput);           
            }
            else
            {
                Debug.Log("Unknown tag!");
            }
        }        

        if (isWaiting || hexMap.Count ==0 || hexMap == null) return;

        if (!ReachedDestination())
        {
            input.SetMoveTarget(currentDestination);
            return;
        }

        input.ClearMoveTarget();
        StartCoroutine(ShootThenMove());
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Debug.Log($"{transform.name} hit {hit.gameObject.name} at {hit.point}");
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
            // Step 1: Rotate to face the target
            yield return StartCoroutine(RotateTowards(target.position));

            // Step 1.5: Pause before shooting
            //yield return new WaitForSeconds(Random.Range(0.5f, 1.75f));

            // Step 2: Shoot
            input.SetShootTarget(target.position);
            shooter.TryShoot(target);

            // Step 3: Cooldown wait
            yield return new WaitForSeconds(shootCooldown);

            input.ClearShootTarget();
        }

        // Step 4: Move to new destination
        currentDestination = GetRandomPosition(hexMap);
        isWaiting = false;
    }

    private IEnumerator RotateTowards(Vector3 targetPosition)
    {        
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0f;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            yield return null;
        }
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
            Debug.Log("..............." + transform.name + " could not find a target!!  Null returned!");
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
        // flag the player as dead.  
        enemyDead = true;        

        GameManager.Instance.RemoveCharacter(id, false);

        //tip the player towards the water in the direction of player velocity        
        LeanTween.rotateAround(gameObject, tippingAxis, -120, 0.4f);
        Vector3 fallDestination = new Vector3(transform.position.x, transform.position.y - 5f, transform.position.z);
        LeanTween.move(gameObject, fallDestination, 0.8f).setOnComplete(DestroyEnemy);
    }

    void DestroyEnemy()
    {
        LeanTween.cancel(gameObject);
        // move this to asset manager when pooling is implemented
        AssetManager.Instance.ReturnAI(gameObject);
    }
}

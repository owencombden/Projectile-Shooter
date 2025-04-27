using UnityEngine;

public class AIInputHandler : MonoBehaviour, ICharacterInputProvider
{
    private Vector3? moveTarget = null;
    private Vector3? shootTarget = null;

    public Vector2 MoveInput { get; private set; }

    public bool ShootAtTarget => shootTarget.HasValue;

    public Vector2 LookInput
    {
        get
        {
            Vector3? lookTarget = shootTarget ?? moveTarget;
            if (lookTarget.HasValue)
            {
                Vector3 direction = lookTarget.Value - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    return new Vector2(direction.x, direction.z).normalized;
                }
            }

            // Default to current forward
            Vector3 forward = transform.forward;
            return new Vector2(forward.x, forward.z).normalized;
        }
    }

    public void SetMoveTarget(Vector3 worldPos)
    {
        moveTarget = worldPos;

        Vector3 direction = worldPos - transform.position;
        direction.y = 0f;
        MoveInput = direction.sqrMagnitude > 0.01f
            ? new Vector2(direction.x, direction.z).normalized
            : Vector2.zero;
    }

    public void ClearMoveTarget()
    {
        moveTarget = null;
        MoveInput = Vector2.zero;
    }

    public void SetShootTarget(Vector3 worldPos)
    {
        shootTarget = worldPos;
    }

    public void ClearShootTarget()
    {
        shootTarget = null;
    }
}

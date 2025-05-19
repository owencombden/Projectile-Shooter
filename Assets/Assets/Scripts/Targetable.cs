
using UnityEngine;

public class Targetable : MonoBehaviour
{
    private void OnEnable()
    {   
        if (TargetManager.Instance != null)
            TargetManager.Instance.RegisterTarget(this);
    }

    private void OnDisable()
    {
        if (TargetManager.Instance != null)
            TargetManager.Instance.UnregisterTarget(this);
    }

    // Optionally, also on destroy to be extra safe
    private void OnDestroy()
    {
        if (TargetManager.Instance != null)
            TargetManager.Instance.UnregisterTarget(this);
    }    
}

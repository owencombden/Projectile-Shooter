
using UnityEngine;

public class Targetable : MonoBehaviour
{
    public string myId    { get; private set; }
    public float  currentHealth;

    private float maxHealth = 100f;    

    private void OnEnable()
    {
        myId = System.Guid.NewGuid().ToString();
        
        if (TargetManager.Instance != null)
            TargetManager.Instance.RegisterTarget(this);
    }

    private void OnDisable()
    {
        if (TargetManager.Instance != null)
            TargetManager.Instance.UnregisterTarget(this);
        
        myId = null;
        currentHealth = maxHealth;
    }

    // Optionally, also on destroy to be extra safe
    private void OnDestroy()
    {
        if (TargetManager.Instance != null)
            TargetManager.Instance.UnregisterTarget(this);
    }
}

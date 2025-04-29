using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }
    
    public List<Targetable> targets = new List<Targetable>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void RegisterTarget(Targetable target)
    {
        if (!targets.Contains(target))
            targets.Add(target);
    }

    public void UnregisterTarget(Targetable target)
    {
        if (targets.Contains(target))
            targets.Remove(target);
    }

    public List<Targetable> GetTargets()
    {
        // PrintAllTargets();
        // Optionally filter dead/inactive targets
        return targets.FindAll(t => t != null && t.isActiveAndEnabled);
    }

    public void PrintAllTargets()
    {
        string output = "...";
        Debug.Log("------------------------------------");
        Debug.Log("Targets list currently contains: ");
        Debug.Log("------------------------------------");
        foreach (Targetable t in targets)
        {
            output += t.transform.name + "...";            
        }
        Debug.Log(output);
        Debug.Log("-----------------------------------");
    }
}

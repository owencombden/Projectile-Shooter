using UnityEngine;
using Unity.Netcode;

public class NetworkManagerScript : MonoBehaviour
{
    private void Awake()
    {
        // If there's already a NetworkManager singleton AND it's not this one, destroy this duplicate.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton != GetComponent<NetworkManager>())
        {
            Destroy(gameObject); // Kill this duplicate
            return;
        }

        DontDestroyOnLoad(gameObject); // Persist the original
    }
}

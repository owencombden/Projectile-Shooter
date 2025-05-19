using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance;

    [Header("Network & Transport")]
    public UnityTransport transport; // Assign in Inspector
    public ushort port = 7777;

    public bool IsHost => NetworkManager.Singleton.IsHost;
    public bool IsClient => NetworkManager.Singleton.IsClient;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.IsListening) return;

        Debug.Log("Starting Host...");
        NetworkManager.Singleton.StartHost();        
        
        int playerCount2 = GameObject.FindGameObjectsWithTag("Player").Length;
        //Debug.Log($"Finished MultiplayerManager.StartHost.  There are {playerCount2} players in the scene.");
    }

    public void StartClient(string ip)
    {
        if (NetworkManager.Singleton.IsListening) return;

        Debug.Log($"Starting Client → {ip}:{port}...");
        transport.SetConnectionData(ip, port);
        NetworkManager.Singleton.StartClient();
    }

    public void Shutdown()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.Log("Shutting down network...");
            NetworkManager.Singleton.Shutdown();
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected with ID: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected with ID: {clientId}");
    }
}

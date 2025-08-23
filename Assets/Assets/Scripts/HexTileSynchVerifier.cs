using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class HexTileSynchVerifier : NetworkBehaviour
{
    // this will be used during debugging and development to ensure the hexmaps are
    // synched across all clients during gameplay.

    [SerializeField] private float checkInterval = 5f;
    private Dictionary<ulong, int> clientHashes = new();  // store the incoming client hashes here for comparison

    private void Start()
    {
        // only run this in the editor or in dev builds
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsClient)
            StartCoroutine(RunRegularCheck());
        #endif
    }

    private IEnumerator RunRegularCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            
            if (GameManager.Instance.GetGameState() == GameManager.GameState.GameOver) yield break;

            if (PauseManager.Instance != null && PauseManager.Instance.isPaused.Value) yield break;

            int hash = ComputeHexMapHash(LevelManager.Instance.GetAllHexMaps());
            SubmitHashToServerServerRpc(hash);
        }
    }

    private int ComputeHexMapHash(Dictionary<ulong, Dictionary<Vector2Int, HexTile>> platforms)
    {
        int hash = 17;
        foreach (var platform in platforms)
        {
            ulong platformId = platform.Key;
            foreach (var kvp in platform.Value)
            {
                Vector2Int grid = kvp.Key;
                HexTile tile = kvp.Value;

                hash = hash * 31 + platformId.GetHashCode();
                hash = hash * 31 + grid.GetHashCode();
                hash = hash * 31 + tile.GetHealthHash(); // We'll define this next
            }
        }
        return hash;
    }    

    [ServerRpc(RequireOwnership = false)]
    private void SubmitHashToServerServerRpc(int hash, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        clientHashes[senderId] = hash;

        //Debug.Log($"Client {senderId} reported hash: {hash}");

        if (clientHashes.Count == NetworkManager.Singleton.ConnectedClients.Count)
        {
            CheckHashes();
            clientHashes.Clear(); // Reset for next interval
        }
    }

    private void CheckHashes()
    {
        var referenceHash = clientHashes.Values.First();
        foreach (var kvp in clientHashes)
        {
            if (kvp.Value != referenceHash)
            {
                Debug.LogWarning($"❌ Client {kvp.Key} is out of sync! Hash: {kvp.Value}, expected: {referenceHash}");
            }
            else
            {
                //Debug.Log($"✅ Client {kvp.Key} is in sync.");
            }
        }
    }
}

using UnityEngine;
using Unity.Netcode;


// This lightweight struct is used to transfer hexTile data across the network

[System.Serializable]
public struct HexTileData : INetworkSerializable
{
    public int platformId;
    public Vector2Int gridCoords;
    public Vector3 worldPos;

    // Optional: health or tile type if needed
    // public float health;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref platformId);
        serializer.SerializeValue(ref gridCoords);
        serializer.SerializeValue(ref worldPos);
        // serializer.SerializeValue(ref health);
    }
}
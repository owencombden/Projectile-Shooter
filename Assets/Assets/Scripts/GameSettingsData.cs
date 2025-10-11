using Unity.Netcode;

[System.Serializable]
public struct GameSettingsData : INetworkSerializable
{
    public int enemyCount;
    public bool enemiesCanShoot;
    public bool gameIsMultiplayer;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref enemyCount);
        serializer.SerializeValue(ref enemiesCanShoot);
        serializer.SerializeValue(ref gameIsMultiplayer);
    }
}
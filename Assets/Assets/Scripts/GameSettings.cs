public static class GameSettings
{
    public static int EnemyCount { get; set; }
    public static bool CanFallOffPlatform { get; set; }
    public static bool EnemiesCanShoot { get; set; }
    public static bool GameIsMultiplayer { get; set; }

    public static GameSettingsData ToData()
    {
        return new GameSettingsData
        {
            enemyCount = EnemyCount,
            canFallOffPlatform = CanFallOffPlatform,
            enemiesCanShoot = EnemiesCanShoot,
            gameIsMultiplayer = GameIsMultiplayer
        };
    }
}
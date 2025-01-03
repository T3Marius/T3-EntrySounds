using static EntrySounds.EntrySounds;
using CounterStrikeSharp.API.Core;
using MySqlConnector;
using Dapper;

namespace EntrySounds;

public static class Database
{
    private static string DatabaseConnectionString { get; set; } = string.Empty;

    private static async Task<MySqlConnection> ConnectAsync()
    {
        MySqlConnection connection = new(DatabaseConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task CreateEntrySoundsTableAsync(Database_Config config)
    {
        if (string.IsNullOrEmpty(config.DatabaseHost) ||
            string.IsNullOrEmpty(config.DatabaseName) ||
            string.IsNullOrEmpty(config.DatabaseUser) ||
            string.IsNullOrEmpty(config.DatabasePassword))
        {
            throw new Exception("[T3-EntrySounds] You need to setup Database credentials in config");
        }

        MySqlConnectionStringBuilder builder = new()
        {
            Server = config.DatabaseHost,
            Database = config.DatabaseName,
            UserID = config.DatabaseUser,
            Password = config.DatabasePassword,
            Port = config.DatabasePort,
            Pooling = true,
            MinimumPoolSize = 0,
            MaximumPoolSize = 600,
            ConnectionIdleTimeout = 30,
            AllowZeroDateTime = true
        };

        DatabaseConnectionString = builder.ConnectionString;

        await using MySqlConnection connection = await ConnectAsync();
        await using MySqlTransaction transaction = await connection.BeginTransactionAsync();

        try
        {
            await connection.ExecuteAsync(CreateTableQuery, transaction: transaction);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[ERROR] Failed to create T3-EntrySounds table: {ex.Message}");
            throw;
        }
    }

    const string CreateTableQuery = @"
    CREATE TABLE IF NOT EXISTS T3_EntrySounds (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        SteamID VARCHAR(50) NOT NULL UNIQUE,
        VolumeSetting FLOAT DEFAULT 60,
        JoinMessageDisabled BOOLEAN DEFAULT FALSE
    );";

    public static async Task SavePlayerVolumeAsync(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return;

        var steamID = player.SteamID.ToString();
        if (!EntrySoundsVolume.TryGetValue(steamID, out var volume))
        {
            volume = Instance.Config.Settings.DefaultVolume; // Fallback to default volume
        }

        const string SaveQuery = @"
            INSERT INTO T3_EntrySounds (SteamID, VolumeSetting)
            VALUES (@SteamID, @VolumeSetting)
            ON DUPLICATE KEY UPDATE
                VolumeSetting = VALUES(VolumeSetting);";

        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.ExecuteAsync(SaveQuery, new
        {
            SteamID = steamID,
            VolumeSetting = volume
        });
    }

    public static async Task<float> LoadPlayerVolumeAsync(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return Instance.Config.Settings.DefaultVolume;

        var steamID = player.SteamID.ToString();

        const string LoadQuery = @"
            SELECT VolumeSetting
            FROM T3_EntrySounds
            WHERE SteamID = @SteamID;";

        using var connection = new MySqlConnection(DatabaseConnectionString);
        var volume = await connection.QueryFirstOrDefaultAsync<float?>(LoadQuery, new { SteamID = steamID });

        return volume ?? Instance.Config.Settings.DefaultVolume;
    }

    public static async Task SaveJoinMessagePreferenceAsync(CCSPlayerController player, bool isDisabled)
    {
        if (player == null || !player.IsValid)
            return;

        var steamID = player.SteamID.ToString();
        const string SaveQuery = @"
        INSERT INTO T3_EntrySounds (SteamID, JoinMessageDisabled)
        VALUES (@SteamID, @JoinMessageDisabled)
        ON DUPLICATE KEY UPDATE
            JoinMessageDisabled = VALUES(JoinMessageDisabled);";

        using var connection = new MySqlConnection(DatabaseConnectionString);
        await connection.ExecuteAsync(SaveQuery, new { SteamID = steamID, JoinMessageDisabled = isDisabled });
    }

    public static async Task<bool> LoadJoinMessagePreferenceAsync(CCSPlayerController player)
    {
        if (player == null || !player.IsValid)
            return false;

        var steamID = player.SteamID.ToString();
        const string LoadQuery = @"
        SELECT JoinMessageDisabled
        FROM T3_EntrySounds
        WHERE SteamID = @SteamID;";

        using var connection = new MySqlConnection(DatabaseConnectionString);
        return await connection.QueryFirstOrDefaultAsync<bool>(LoadQuery, new { SteamID = steamID });
    }
}

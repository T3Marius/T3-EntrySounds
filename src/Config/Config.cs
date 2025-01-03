using CounterStrikeSharp.API.Core;

namespace EntrySounds;

public class PluginConfig : BasePluginConfig
{
    public Settings_Config Settings { get; set; } = new Settings_Config();
    public Dictionary<string, EntrySounds_Config> EntrySounds { get; set; } = new Dictionary<string, EntrySounds_Config>();
    public Database_Config Database { get; set; } = new Database_Config();
}


public class EntrySounds_Config
{
    public string SoundPath { get; set; } = "";
    public string JoinMessage { get; set; } = "";
    public string SteamID { get; set; } = "";
    public List<string> Flags { get; set; } = new List<string>();
}
public class Settings_Config
{
    public List<string> MenuCommand { get; set; } = new List<string> { "es", "entrysounds" };
    public int DefaultVolume { get; set; } = 60;
}
public class Database_Config
{
    public string DatabaseHost { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string DatabaseUser { get; set; } = "";
    public string DatabasePassword { get; set; } = "";
    public uint DatabasePort { get; set; } = 3306;
}

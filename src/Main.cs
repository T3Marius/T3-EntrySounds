using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core;
using T3MenuSharedApi;
using System.Text.RegularExpressions;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core.Translations;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EntrySounds;

public class EntrySounds : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleAuthor => "T3Marius";
    public override string ModuleName => "T3-EntrySounds";
    public override string ModuleVersion => "1.0";
    public PluginConfig Config { get; set; } = new PluginConfig();
    public static EntrySounds Instance { get; set; } = new EntrySounds();

    public static Dictionary<string, float> EntrySoundsVolume = new Dictionary<string, float>();
    public static Dictionary<string, string> SelectedEntrySounds = new Dictionary<string, string>();

    public async void OnConfigParsed(PluginConfig config)
    {
        Config = config;

        await Database.CreateEntrySoundsTableAsync(config.Database);
    }
    public IT3MenuManager? MenuManager;
    public IT3MenuManager? GetMenuManager()
    {
        if (MenuManager == null)
            MenuManager = new PluginCapability<IT3MenuManager>("t3menu:manager").Get();

        return MenuManager;
    }
    public override void Load(bool hotReload)
    {
        Instance = this;

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        foreach (var cmd in Config.Settings.MenuCommand)
        {
            AddCommand($"css_{cmd}", "Opens The Menu", Command_Menu);
        }
    }
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        Task.Run(async () =>
        {
            await Database.SavePlayerVolumeAsync(player);
        });

        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            return HookResult.Continue;

        string steamID = player.SteamID.ToString();

        var soundConfig = Config.EntrySounds
            .Where(entry => entry.Value.SteamID == steamID)
            .Select(entry => entry.Value)
            .FirstOrDefault();



        if (soundConfig != null)
        {

            Task.Run(async () =>
            {
                float playerVolume = EntrySoundsVolume.TryGetValue(steamID, out var volume)
                    ? volume
                    : await Database.LoadPlayerVolumeAsync(player);

                if (playerVolume > 0)
                {
                    string updatedSoundPath = Regex.Replace(soundConfig.SoundPath, @"\d+_volume", $"{(int)playerVolume}_volume");
                    Server.NextFrame(() =>
                    {
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p != null && p.IsValid && !p.IsBot)
                            {
                                p.ExecuteClientCommand($"play {updatedSoundPath}");
                            }
                        }
                    });
                }
            });
        }
        if (soundConfig != null && !string.IsNullOrEmpty(soundConfig.JoinMessage))
        {
            string joinMessage = soundConfig.JoinMessage.Replace("{name}", player.PlayerName);
            string colorMessage = StringExtensions.ReplaceColorTags(joinMessage);

            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid && !p.IsBot)
                {
                    p.PrintToChat(colorMessage);
                }
            }
        }

        return HookResult.Continue;
    }

    public void Command_Menu(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null)
            return;

        var manager = GetMenuManager();
        if (manager == null)
            return;

        var mainMenu = manager.CreateMenu(Localizer["mainmenu<title>"], isSubMenu: false);

        mainMenu.Add(Localizer["option<changevolume>"], (p, option) =>
        {
            float currentVolume = EntrySoundsVolume.TryGetValue(player.SteamID.ToString(), out var volume) ? volume : Config.Settings.DefaultVolume;

            int defaultVolumeValue = (int)Math.Clamp(currentVolume, 0, 100);
            List<int> volumeOptions = new List<int> { 20, 40, 60, 80, 100 };

            if (defaultVolumeValue == 0)
            {
                volumeOptions.Insert(0, 0);
            }

            string volumeTitle = defaultVolumeValue == 0
                ? Localizer["volumemenu<title>", 0]
                : Localizer["volumemenu<title>", defaultVolumeValue];

            var volumeMenu = manager.CreateMenu(volumeTitle, isSubMenu: true);
            volumeMenu.ParentMenu = mainMenu;

            volumeMenu.AddBoolOption(Localizer["suboption<mutesounds>"], defaultValue: defaultVolumeValue == 0, (p, option) =>
            {
                if (option is IT3Option boolOption)
                {
                    bool isMuted = boolOption.OptionDisplay!.Contains("✔");
                    if (isMuted)
                    {
                        EntrySoundsVolume[p.SteamID.ToString()] = 0;
                        p.PrintToChat(Localizer["prefix"] + Instance.Localizer["mutesounds<enabled>"]);
                    }
                    else
                    {
                        EntrySoundsVolume[p.SteamID.ToString()] = Config.Settings.DefaultVolume;
                        p.PrintToChat(Localizer["prefix"] + Instance.Localizer["mutesounds<disabled>"]);
                    }
                }
            });
            volumeMenu.AddSliderOption(display: " ", customValues: volumeOptions, defaultValue: defaultVolumeValue, onSlide: (p, option) =>
            {
                string steamID = p.SteamID.ToString();

                if (!EntrySoundsVolume.ContainsKey(steamID))
                    EntrySoundsVolume.Add(steamID, option.SliderValue);

                EntrySoundsVolume[steamID] = option.SliderValue;
                p.PrintToChat(Localizer["prefix"] + Localizer["volume<selected>", option.SliderValue + "%"]);

                Task.Run(async () =>
                {
                    await Database.SavePlayerVolumeAsync(p);
                });
            });


            manager.OpenSubMenu(p, volumeMenu);
        });

        manager.OpenMainMenu(player, mainMenu);
    }

}

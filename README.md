# T3-EntrySounds
Custom Entry Sounds and Join Messages with Modifiable Volume for CS2.

# Dependecies
- [**CounterStrikeSharp**](https://github.com/roflmuffin/CounterStrikeSharp)
- [**T3Menu-API**](https://github.com/T3Marius/T3Menu-API)
- [**VolumeAdjuster**](https://github.com/oqyh/Volume-Adjuster-GoldKingZ)
- [**MultiAddonManager**](https://github.com/Source2ZE/MultiAddonManager)

# Conifg:

```json
{
  "Settings": {
    "MenuCommand": ["es", "entrysounds"],
    "DefaultVolume": 60

  },
  "EntrySounds": {
    "Marius": {
        "SoundPath": "sounds/mvp_straine/100_volume/gangstasparadise.vsnd_c",
        "JoinMessage": "Player {red}{name}{default} just joined the server!",
        "SteamID": "76561199478674655",
        "Flags": []
    }
  },
  "Database": {
    "DatabaseHost": "",
    "DatabaseName": "",
    "DatabaseUser": "",
    "DatabasePassword": "",
    "DatabasePort": 3306
  },
  "ConfigVersion": 1
}
```

# Info
- After you create your sounds with VolumeAdjuster the app will create you the folders with volumes that you selected for example if you choose: 100,80,60,40,20 all the sounds will be in separate folders ex 100_volume/sounds/sound1.vsnd_c and 20_volume/sounds/sound1.vsnd_c.

- In config file you will only need to put the path with th 100_volume since my code it will automaticly replace the 100 based on player selected volume.

Many greetings to [**@oqyh**](https://github.com/oqyh) for creating the VolumeAdjuster app.

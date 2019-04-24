# Infinity
#### A collection of utilities and hacks

## Installing
- Install Meepen's Mod Loader ([thunderstore.io](https://thunderstore.io/package/meepen/Meepens_Mod_Loader/) [github](https://github.com/meepen/ror2-modloader))
- Download latest release.zip from https://github.com/PixelToast/ror2-infinity-mod/releases
- Unizp into RoR2 install directory

## Building from source
- Install Meepen's RoR2 modloader ([thunderstore.io](https://thunderstore.io/package/meepen/Meepens_Mod_Loader/) [github](https://github.com/meepen/ror2-modloader))
- Download [premake5](https://github.com/premake/premake-core/releases)
- Run `premake5 vs2017` (for vs2017)
- Open the solution in the `project` folder
- Build the project
- Copy `bin\mods\infinity.dll` to your game's mods folder
- Alternatively, automatically copy it by adding the following to your .csproj:
```
<Target Name="AfterBuild">
	<Copy SourceFiles="$(ProjectDir)\..\bin\mods\infinity.dll" DestinationFolder="<steam game folder>\Risk of Rain 2\Mods" ContinueOnError="true" />
</Target>
```

## Commands
Misc:

- **modded** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Whether or not the game is considered modded, modded games are put in a seperate quickplay queue

Player:

- **stat_maxhealth** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players maximum health (Host only)
- **stat_regen** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base health regen (Host only)
- **stat_maxshield** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base shield (Host only)
- **stat_movespeed** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base movement speed
- **stat_acceleration** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base movement acceleration
- **stat_jumppower** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base jump power
- **stat_damage** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base damage multiplier (Host only)
- **stat_attackspeed** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp;  Your local players base attack speed
- **stat_crit** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base crit chance
- **stat_armor** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base armor points (Host only)
- **stat_jumpcount** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Your local players base jump count

_note: base stats reset every level_

- **lunar** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; How many lunar coins you have
- **money** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; How much money you have
- **team_money** num &nbsp;&nbsp;-&nbsp;&nbsp; Reward money to the entire team (Host only)

Chests:

- **chest_unlock** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Prevent interactables from being locked while teleporter is active (Host only)
- **chest_stacks** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; How many duplicate items chests will drop (Host only)

Teleporter:

- **tele_speed** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Speed multiplier for teleporter charging (Host only)
- **tele_boss_stacks** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Boss credit multiplier from mountain shrines (Host only)
- **tele_reward_stacks** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Boss item reward multiplier (Host only)
- **tele_shop_portal** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; If a blue portal should spawn (Host only)
- **tele_gold_portal** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; If a gold portal should spawn (Host only)
- **tele_ms_portal** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; If a celestial portal should spawn (Host only)
- **tele_ping** &nbsp;&nbsp;-&nbsp;&nbsp; Reveals position of teleporter
- **tele_exit** &nbsp;&nbsp;-&nbsp;&nbsp; Teleport to next level immediately (Host only)

Level:

- **level_intr_stacks** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Interactable credit multiplier, spawns more stuff around the map (Host only)
- **level_monster_stacks** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Monster credit multiplier, spawns harder enemies (Host only)

Lobby:

- **lobby_join_delay** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Delay before your quickplay lobby is open to join
- **lobby_start_delay** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Delay before your quickplay lobby starts when someone joins
- **lobby_host_min** [_value_] &nbsp;&nbsp;-&nbsp;&nbsp; Minimum players in your lobby before you won't join someone else's lobby, set to 1 if you always want to be the host

## Configs
- **Risk of Rain 2_Data\Config\infinity_start.cfg** &nbsp;&nbsp;-&nbsp;&nbsp; executed after infinity loads
- **Risk of Rain 2_Data\Config\infinity_pod.cfg** &nbsp;&nbsp;-&nbsp;&nbsp; executed after your launch pod opens

## Hooks

* **RoR2.Console.Awake** (Postfix) &nbsp;&nbsp;-&nbsp;&nbsp; Passive hook to register commands
* **EntityStates.SurvivorPod.Release.OnEnter** (Postfix) &nbsp;&nbsp;-&nbsp;&nbsp; Passive hook to run `infinity_pod.cfg`
* **RoR2.ChestBehavior.ItemDrop** (Prefix) &nbsp;&nbsp;-&nbsp;&nbsp; Full override for **chest_stacks**
* **RoR2.TeleporterInteraction.OnStateChanged** (Prefix) &nbsp;&nbsp;-&nbsp;&nbsp; Full override for **chest_unlock**
* **RoR2.TeleporterInteraction.StateFixedUpdate** (Prefix) &nbsp;&nbsp;-&nbsp;&nbsp; Full override for **tele_speed**
* **RoR2.BossGroup.OnCharacterDeathCallback** (Prefix) &nbsp;&nbsp;-&nbsp;&nbsp; Full override for **tele_reward_stacks**
* **RoR2.SceneDirector.Start** (Prefix) &nbsp;&nbsp;-&nbsp;&nbsp; Full override for **level_intr_stacks** and **evel_monster_stacks**
* **RoR2.Networking.SteamLobbyFinder.Awake** (Postfix) &nbsp;&nbsp;-&nbsp;&nbsp; Passive hook for **lobby_join_delay** and **lobby_start_delay**

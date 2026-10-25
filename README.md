# NoFoodBurn
This mod is designed to enhance the cooking experience in Valheim by ensuring that food is never burned and turned into coal. This mod works for any
place where food needs to be cooked, including ovens, and modded cooking stations. It is compatible with all food types, including modded ones.

## Configuration
The mod provides several configuration options to customize its behavior:

| Option | Description |
|--------|-------------|
| `EnableWoodenStation` | Toggles anti-burn protection for wooden cooking stations. |
| `EnableIronStation` | Toggles anti-burn protection for iron cooking stations. |
| `EnableStoneOven` | Toggles anti-burn protection for stone ovens. |
| `EnableModdedStations` | Toggles anti-burn protection for modded cooking stations. |

## Install
###### Using a Mod Manager to search for the mod is recommended, but you can also install manually.
#### Direct
1. Download the latest .dll from the releases section.
2. Place the .dll file in your Valheim `BepInEx/plugins` folder.
#### Mod Manager
###### Search for "NoFoodBurn" in your mod manager of choice or follow below to import as .zip
1. Download the latest .zip from the releases section.
2. Navigate to the import mod section of your mod manager and select the .zip file you downloaded.
	- Usually in settings somewhere.
		- r2modman: Settings > Profile > Import Local Mod\

##### If you install the mod from the .dll or .zip you will need to update the mod manually when a new version is released. If you install the mod from a mod manager search, it will handle updates for you.

## Build from source
1. Clone the repository.
2. Open the solution in Visual Studio.
3. Change configuration to Release.
4. Build the solution.
5. Copy the resulting .dll from `NoFoodBurn\build\Release` to your Valheim `BepInEx/plugins` folder.

## Contributing
Any contributions to this project are welcome. If you would like to contribute, please fork the repository and submit a pull request. 
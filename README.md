# Quest Data Builder

Quest Data Builder is a quest data generator for Morrowind, designed for use with the [Quest Guider](https://www.nexusmods.com/morrowind/mods/55593) and [Quest Guider Lite](https://www.nexusmods.com/morrowind/mods/57285) mods.

**Requires [.NET Runtime 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) to run.**

This project is based on [ESMSharp](https://github.com/demonixis/ESMSharp).

## Command-Line Arguments

- `-l, --loglevel <LogLevel>`: Logging level (0-3). Default is 1.
- `-L, --logToFile <bool>`: Enable logging to file. Default is `false`.
- `-c, --configFile <string>`: Path to the configuration file (JSON format with comment support). Default is `config.json` in the current directory.

## Example Configuration File
```
{
	//Initialization type.
	//Possible options:
	//"Auto" - the generator tries to find paths to data files automatically, or reads them from "morrowindDirectory", or "omwLauncherCfgPath" + "omwOpenmwCfgPath", or "mo2BaseDirectory".
	//	If data is found, the first valid source will be used for generation: Morrowind directory > OpenMW data > Mod Organizer 2 data.
	//"Manual" - Same as "Auto", but allows you to select the data source for generation via CLI.
	//"Config" - Data is loaded from the "files" field or from "morrowindDirectory" + "gameFiles" in the config file.
	"initializer": "Manual",
	
	//Also log to log.txt. If the parameter is set from the config and not from the command line, the initial part of the logs will not be written to the file.
	"logToFile": true,
	
	//Logging level. 0-3
	"logLevel": 1,

	//Encoding for game archives. 1250 - Polish version, 1251 - Russian version, 1252 - others.
	"encoding": 1252,
	//Directory for output files.
	"output": "questData",
	//Format of output files. "json", "yaml" or "lua".
	"outputFormat": "yaml",
	
	//The path settings below are needed when the generator cannot find these paths automatically or specific paths are required.
	//Uncomment the part you need, as paths set in the config will override those found by other means.
	
	//List of files with full paths.
	//"files": [],

	//Game directory.
	//"morrowindDirectory": "D:\\Games\\Steam\\steamapps\\common\\Morrowind\\",
	//List of game file names without paths.
	//"gameFiles": [],

	//Path to OpenMW "launcher.cfg".
	//"omwLauncherCfgPath": "",
	//Path to OpenMW "openmw.cfg".
	//"omwOpenmwCfgPath": "",

	//Path to MO2 directory for Morrowind. Usually this directory is located at "..\AppData\Local\ModOrganizer\ModOrganizer\Morrowind\".
	//"mo2BaseDirectory": "",

	//Regex patterns for ignoring files
	"ignoredDataFilePatterns": [],
	
	//Maximum number of game object positions to be saved in the output files.
	"maxObjectPositions": 50,
	//Remove from output files any data that cannot be used by the mod.
	"removeUnused": true,
	//Number of decimal places for numbers in the output files.
	"fractionDigits": 3,
	//Number of stages in a quest (journal-type dialogue) for its information to be saved.
	"stagesNumToAddQuestInfo": 1,

	//Enable generation and saving of the height map image
	"enableHeightMapImageGeneration": false,
	//Downscale factor for height map image. 1 - full size (64x64 for one cell).
	"heightMapImageDownscaleFactor": 2,
	
	//Try to find information about dialogues that can open quest dialogues.
	"findDialogueLinks": true,
	//Maximum search depth.
	"dialogueSearchDepth": 2,
	//Do not save dialogue data for individual dialogues if there are too many.
	"optimizeData": true
}
```

## Third-Party Libraries and Licenses

This project uses the following third-party libraries:

- [CXuesong.Luaon](https://github.com/CXuesong/Luaon.NET)  
  Licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0.html).

- [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp)  
  Licensed under the [Six Labors Split License](https://github.com/SixLabors/ImageSharp/blob/main/LICENSE).  

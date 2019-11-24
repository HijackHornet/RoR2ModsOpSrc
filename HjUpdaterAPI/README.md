![](https://lh3.googleusercontent.com/8pxcLPj9w-mzVGApS19ZoY1ECpWDiDsQ1gqF7ph8_-n9pEPsTWQBQ208FXVAwGffITcQgkqhSJzOYN0k2LUGi1D6MflZYFEIKvSej2pyOXX67xXDCPGn17dZqhO01KCxptZfO-5Awr6Uja-VtIpqGXMFweme5UJLjmRLzUz_xqoJTQVw3zUBwjDmmY4LO-lfuLBaH738Gc6L0IBFlf2iUeLjFYYRn5vuf-LPm4oXoTLKxROc27R4TlKVS9zZdduLK9oO044bR_KJgidLq8cL-oxKFJcDV9hDFx9-AZjhn3X7ggslq1SX-O1XJohij6Fq8KlxJDF8kXW-JQKHETblH6mmgStqjIHosNlXUmuGykHDEdQTvQjJtZFyUUGbo9oeATbhlz5NdNzT6H2Sf3sUL7mrn694CyTID7iVEakOx9xZiymyEDm3zoaGX0-U53WxFcYZd6tONZw_EGhUGSWzn2tMjNDCQB_PEWmYQS756r4Ga60_jzNXgjALVkCi2sXjuorz8Z7xhwti_OwuLPXeyEsjlTzPpZ_YR6WHQetyZVep8QOd8zAo79Z3uiy-4kP6NBPFIV5EwMjT4RQysdRFdNv2NraqN1Jdx8s4zBMXwHYE3HYVyyQ68312e6o87eWYHYrNx5VH5IkaOZRjkFO5AuFiq-IO-6VYUvZdek9lkq2BR52OVqm0SE4=w1024-h300-no)

# Hj Updater API  
This 'mod' is actually an API for developers willing to implement an auto-update feature for their mod in only one function call. As a user of the mod, every other mod using Hj Updater API will be updated automatically while you are playing. As a developer, many options are available for you to decide how the update should behave. If you only want to warn the mod user that an update is available, Hj will provide you an easy way to do so.

## How does it work ?  
I won't enter the details but BepInEx loads all of your mods into your RAM when you start the game. That means that **while still playing, modification to the mods files are possible** without impacting your gameplay experience. For mods that require runtime access to files resources (assets, sandbanks,etc.)., Hj will wait for you to close the game before deploying the changes.  

## How much time is needed to add it to my mod ?
About 5 minutes for one dll mods, maybe 10 for complex mods with multiple non embed ressources. The docs seems hard but just add the dependency and copypast one of the case study if you don't want to enter into the details.
## Is it safe to use ?  
**Yes**. Every update is logged, and every old mod version is kept into a **Backup** folder under `BepInEx/plugin/HijackHornet-HjUpdaterAPI/Backup`. Also, Hj create an interface between mods and the folder structure in which they exist. So **no matter if a user put the dll file into the root of /plugin or into a folder inside another folder** named pepeDontLikeToFeel : the mod will still update and keep this folder structure.

## User Guide  
As a user, you can install this mod by unzipping the archive into the plugin folder. Don't forget to add **BOTH** dll next to each other.  
However, this mod won't do anything if the other mods you are using aren't compatible with it. Some mods might put Hj as a dependency, forcing you to install it, while others might only suggest you install it because they made their mod compatible.  
### Config  
If a mod has been updated automatically but the update is broken and you would like to use the backup files (previous version), you should change values in the config file as you please.
Just go to /BepInEx/config/hjupdaterapi.config and change things according to your preferences. You can even choose to overwriter modders decisions over what type of update should be performed.

## Developer Guide  
As a modder, you can use Hj function to add an update behaviour for your mod. It's very simple to do and should provide enough flexibility to work with basically any mod. Here is how to setup Hj into your mods. It also ensures that if your mod end up being deprecated, it will be automatically deactivated on users’ computers.

  
### Adding the dll into your project lib folder  
In Visual Studio (or whatever IDE you use) add both dll included in the mod download link into your library folder, and add a reference to them into your project settings.

### Adding the dependency  
You can choose to HjUpdaterAPI as a dependency so that you are assured that everyone using it will get the latest version or at least be informed that a new version is available. However you can also choose to add it has an optional dependency and check in the awake function if the mod is installed and if so perform the command call.
To add it as required dependency (recommanded) :
```json
//manifest.json
{
    "name": "MyModName",
    "version_number": "1.2.1",
    "website_url": "myWebSite",
    "description": "Restart a run with a simple key !",
    "dependencies": [
        "bbepis-BepInExPack-3.0.0",
		"HijackHornet-HjUpdaterAPI-1.1.0" <--- Add this !
    ]
}
```
```cs
//Your mod class

[BepInDependency(HjUpdaterAPI.GUID)] //Add this
```
No need to update the dependency version when HjUpdaterApi will be updated.

To add it as an optional dependency, just add put a if statement arround the static function like this :
```cs
 if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(HjUpdater.GUID)){
                
        RegisterForUpdate(  
                "MyModName",  
                MetadataHelper.GetMetadata(this).Version
        );  

}
```

### Using the namespace  
First add the namespace usage at the beginning of your mods main class.  
```c  
using Hj;  
//your other namespace  
```  
Now go into your awake function (it has to be put into Awake and not into Start) and call the register function :  

```c
void Awake(){  
HjUpdaterAPI.RegisterForUpdate(  
	string packageName,  
	System.Version currentVersion,  
	[enum flag],  
	[List<string> otherFilesLocationRelativeToTheDll],  
	[bool modUseRuntimeRessourceLoading]
	);  
}  
```  
Let's go through each parameter  
Mandatory parameters  
- packageName - The name of your mod inside the manifest.json name  
- currentVersion - The version of your mod. Can bet fetch using `MetadataHelper.GetMetadata(this).Version`, but don't forget to update this number in both the manifest.json and in the main class bepinEx plugin declaration.

Optional parameters

- flag - This is used to specify the behaviour of the update. Options are :  
HjUpdaterAPI.Flag.**UpdateAlways**  
HjUpdaterAPI.Flag.**UpdateIfSameDependencyOnlyElseWarnOnly**  
HjUpdaterAPI.Flag.**UpdateIfSameDependencyOnlyElseWarnAndDeactivate**  
HjUpdaterAPI.Flag.**WarnOnly**  
HjUpdaterAPI.Flag.**WarnAndDeactivate**

By default, **UpdateIfSameDependencyOnlyElseWarnOnly** is used.  
**DO NOT** use UpdateAlways except if you know what you are doing. It could be useful for instance if you know that the next dependency update will not break your mod, but it's risky and could end up breaking your mod.

- otherFilesLocationRelativeToTheDll - Other files relative path inside of you mod. You do not have to add the readme, manifest and icon positions here. Those files could be other dll, resources, assets.  
- modUseRuntimeRessourceLoading - True if your mod is loading files at runtime (in or after the Start() call. Default : false.  
If this value is true, then the deployment process of the update is postponed to the game closure call, so that it doesn’t affect the mod usage at runtime.

That's it ! Your mod is now able to self-update at runtime when newer versions are uploaded on the thunderstore.

### Case study / Example  
#### A simple dll mod with no other files  
```c  
void Awake(){  
RegisterForUpdate(  
	"MyModNameAsInTheManifest",  
	MetadataHelper.GetMetadata(this).Version
	);  
//...your other function calls  
}  
```  
#### A mod with one dll but that don't update and just warn the user that a new version exists  
```c  
void Awake(){  
List<string> files = new List<string>();  
files.Add('mySecondDll.dll');  
RegisterForUpdate(  
	"MyModNameAsInTheManifest",  
	MetadataHelper.GetMetadata(this).Version,    
	HjUpdaterAPI.Flag.WarnOnly  
);  
//...your other function calls  
}  
```

#### A mod with two dll next to each other  
```c  
void Awake(){  
List<string> files = new List<string>();  
files.Add('mySecondDll.dll');  
RegisterForUpdate(  
	"MyModNameAsInTheManifest",  
	MetadataHelper.GetMetadata(this).Version,   
	HjUpdaterAPI.Flag.UpdateIfSameDependencyOnlyElseWarnOnly  
	files  
);  
//...your other function calls  
}  
```  
#### A mod with resources loaded at runtime inside a subfolder  
```c  
void Awake(){  
List<string> files = new List<string>();  
files.Add('/asset/myAssetPack.asset');  
files.Add('/font/myFont.ttf');  
files.Add('/sounds/mySoundBank.bnk');  
RegisterForUpdate(  
	"MyModNameAsInTheManifest",  
	MetadataHelper.GetMetadata(this).Version,  	
	HjUpdaterAPI.Flag.UpdateIfSameDependencyOnlyElseWarnOnly,  
	files,  
	true  
	);  
//...your other function calls  
}  
```

  
## Versions  
- 1.0.0 - Initial release
- 1.0.3 - Removing the neccesity to give the assembly path
- 1.0.4 - Fixing a small bug on the file location
- 1.0.5 - Fixing critical issue introduced in the process of making the all thing easier to use...
- 1.0.6 - Add some documentation for adding this as an optional dependency.
- 1.1.0 - Add config for users to choose what type of options not to perform & switching to an enum for flags (non breaking, byte still work until 1.2.0)
- 1.1.1 - Change the package GUID from static to const on peoples advice
## Contact  
I'm available on the ROR2 Official discord server (@Hijack Hornet).

## Thanks  
And thanks to @Bepis for explaining to me how he made BepInEx. That really helped me find how to make this API possible. Thanks to @Rein too for its encouragement in this project.

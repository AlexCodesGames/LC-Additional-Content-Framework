# Additional Content Framework
## About
This mod acts as loader for other mods, empowering modders to easily add their own custom items to Lethal Company by only adding resource files (defs, textures, etc.)!

![Lethal_Company_sZ6nxSqOOj](https://github.com/AlexCodesGames/LC-Additional-Suits/assets/9707185/10aa7d2c-db8f-4e15-a80f-a626dcdc9ea0)
 
Please Note: For this mod to work, ALL players in your lobby need to have this mod (along with any content modules being used) downloaded!

## Usage

The purpose of this repository is to make modding Lethal Company more accessible and raise the level of community knowledge. This repo contains the pre-build project file for the mod, which means it includes some details that are usually changed/culled during the build process (comments, references, etc.). Feel free to reuse any portions of the code for learning or making your own mods!

### Creations:
[Additional Suits - Thunderstore](https://thunderstore.io/c/lethal-company/p/AlexCodesGames/AdditionalSuits/)

## Instructions - Installation
Automatic: Click 'Install with Mod Manager' button (ensure you have the Thunderstore mod manager installed).

Manual: Export the main folder into your BepInEx/plugins folder.

After installation you will see the new suits when you launch the game.

## Instructions - Adding New Content
 Currently it only provides an interface for adding suits, but terminal items & scrap are planned as well.

### Adding Suits:
To add new suits to your game simply:
	
	1 - Download the additional content template folder from github [Here]()
	2 - Rename the resource folder (in the plugin folder) from 'resTemplate' to 'res' + your_mod_name (the mod folder MUST have 'res' as a prefix)
	3 - In the resource folder, replace the template suit textures with your own textures
	4 - In the resource folder, update the 'suit-defs.json', linking your new suits
	5 - Update the 'readme' and 'manifest' with your mod details
	6 - Add your own icon
	7 - Pack into a .zip file and upload to Thunderstore!

## Additional Info
If you are interested in the raw source, you can find the project file in this [GitHub Repo](https://github.com/RabidCodeHog/LC-Additional-Suits/). Feel free to use it in any way you wish!

[X (Twitter)](https://twitter.com/AlexCodesGames) | [Instagram](https://www.instagram.com/alexcodesgames/) | [Discord](https://discordapp.com/users/the_shadow_wizard)

## Changelog
```
	- v1.0.0
		- Release
```

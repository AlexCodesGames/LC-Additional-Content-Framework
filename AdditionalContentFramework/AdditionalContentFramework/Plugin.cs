using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/** LETHAL COMPANY MOD - ADDITIONAL CONTENT FRAMEWORK
 *  if you are reading this then have a great day ^_^
 *  
 *  this mod acts as loader for other mods, allowing you to easily add your own custom items to Lethal Company!
 *  
 *  feel free to read/reuse any snippets below to learn and create your own mods!
 *  
 *  discord: the_shadow_wizard
 */
namespace AdditionalContentFramework
{
    /// <summary>
    /// data container representing a custom suit
    /// </summary>
    [Serializable]
    public class AdditionalSuitDef
    {
        //indexing
        public string suitID;
        //terminal unlockable settings
        //public bool isUnlockable;
        //public bool isUnlocked;
        //public int unlockCost;
        //display
        public string suitName;
        public string suitTexture;
    }

    /// <summary>
    /// data container representing a content module
    /// </summary>
    [Serializable]
    public class AdditionalContentModule
    {
        //path to resource folder (contains def json & texture files)
        public string resourceFolder;

        //all defs contained in module
        public List<AdditionalSuitDef> suitDefList = new List<AdditionalSuitDef>();

        //constructor
        public AdditionalContentModule(string folder)
        {
            resourceFolder = folder;
        }
    }

    /// <summary>
    /// mod base class
    /// </summary>
    [BepInPlugin(modGUID, modName, modVersion)]
    public class AdditionalContentFrameworkBase : BaseUnityPlugin
    {
        //singleton instance access (we only ever want 1 base class active at a time)
        private static AdditionalContentFrameworkBase Instance;

        //mod details
        private const string modGUID = "ACS.AdditionalContentFramework";
        public string ModGUID { get { return modGUID; } }
        private const string modName = "AdditionalContentFramework";
        public string ModName { get { return modName; } }
        private const string modVersion = "1.0.2";
        public string ModVersion { get { return modVersion; } }

        //harmony reference
        private readonly Harmony harmony = new Harmony(modGUID);

        //logger reference
        private static ManualLogSource mls;
        public static void AddLog(string log) { mls.LogInfo(modName + " - " + log); }

        //run-time variables
        //  when true mod has been loaded (ensures content are only added once per game)
        public static bool IsContentLoaded = false;
        //  default unlockable suit (used as a starting point when loading all suits)
        public static UnlockableItem PrefabUnlockableSuit = null;
        //  all registered content modules
        public static List<AdditionalContentModule> ContentModules = new List<AdditionalContentModule>();

        /// <summary>
        /// use to prepare our mod's environment
        /// called when the script is initialized
        /// </summary>
        void Awake()
        {
            //ensure instance is set
            if (Instance == null) { Instance = this; }

            //attach new logger
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            AddLog("initializing...");

            //apply patch via harmony
            harmony.PatchAll();

            AddLog("initialized!");
        }

        /// <summary>
        /// adds a set of suit defs from a given location
        /// </summary>
        public static void LoadContentModule(string path)
        {
            AddLog("attempting to load suits from path:" + path);

            //set folder location
            string pathFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path);

            //translate data from json into parsable classes
            //NOTE: it does not look like the standard approaches work x_x so we need to be creative~
            /*SuitDefManifest = JsonUtility.FromJson<UnlockableSuitDefListing>(jsonFile);
            if (SuitDefManifest == null || SuitDefManifest.unlockableSuits == null)
            {
                AddLog("ERROR: failed to convert json file to manifest");
                return;
            }*/

            //load json file
            string jsonPath = Path.Combine(pathFolder, "suit-defs.json");
            AddLog("attempting to parse json file: " + jsonPath);
            string jsonText = File.ReadAllText(jsonPath);
            if (jsonText == null)
            {
                AddLog("ERROR: json file was not found or invalid");
                return;
            }

            //create new suit def manifest for given module (we do this down here AFTER json is found and read)
            AdditionalContentModule contentModule = new AdditionalContentModule(pathFolder);

            //parse json file for all suit defs
            string[] split1 = jsonText.Split('[');
            split1 = split1[1].Split(']');
            split1 = split1[0].Split('{');
            //it was late in the night man .-. just go with it
            for (int i = 1; i < split1.Length; i++)
            {
                //break def apart and convert to a def data object
                string defStr = "{" + split1[i].Trim();
                if (i != split1.Length - 1)
                {
                    defStr = defStr.Substring(0, defStr.Length - 1);
                }
                AdditionalSuitDef def = JsonUtility.FromJson<AdditionalSuitDef>(defStr);
                AddLog("\tloaded suit def: " + def.suitName);

                //add def to module's def listing
                contentModule.suitDefList.Add(def);
            }

            //add content module to listing of valid modules
            ContentModules.Add(contentModule);

            AddLog("finished loading suits from path:" + path);
        }

        /// <summary>
        /// processes the given module, adding all associated defs into the game
        /// </summary>
        public static void ApplyContentModule(StartOfRound __instance, AdditionalContentModule suitDefManifest)
        {
            AddLog("applying additional content module from: " + suitDefManifest.resourceFolder + "...");

            //	process all suit defs, adding them into the game
            foreach (AdditionalSuitDef suitDef in suitDefManifest.suitDefList)
            {
                AddSuitToRack(__instance, suitDef, suitDefManifest.resourceFolder);
            }

            AddLog("applyed additional content module from: " + suitDefManifest.resourceFolder + "!");
        }

        /// <summary>
        /// processes the given suit def and adds it to the ship's suit rack
        /// </summary>
        public static void AddSuitToRack(StartOfRound __instance, AdditionalSuitDef suitDef, string resourcePath)
        {
            AddLog("adding suit to rack {id=" + suitDef.suitID + ", name=" + suitDef.suitName + "}...");

            //create new suit item based on default suit
            UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(PrefabUnlockableSuit));

            //set suit details
            //  create new texture container
            Texture2D suitTexture = new Texture2D(2, 2);
            //  load texture from file
            ImageConversion.LoadImage(suitTexture, File.ReadAllBytes(Path.Combine(resourcePath, suitDef.suitTexture)));
            //  apply texture to new material
            Material suitMaterial = Instantiate<Material>(newUnlockableItem.suitMaterial);
            suitMaterial.mainTexture = suitTexture;
            //  apply material to suit
            newUnlockableItem.suitMaterial = suitMaterial;
            //  set name
            newUnlockableItem.unlockableName = suitDef.suitName;

            //add new item to the listing of tracked unlockable items
            __instance.unlockablesList.unlockables.Add(newUnlockableItem);

            AddLog("added suit to rack {id=" + suitDef.suitID + ", name=" + suitDef.suitName + "}! (new unlockable count is " + __instance.unlockablesList.unlockables.Count + ")");
        }

        /// <summary>
        /// called when a game lobby fist loads, checking for any custom content modules then loading/applying them
        /// </summary>
        [HarmonyPatch(typeof(StartOfRound))]
        internal class acgAdditionalContentPatch
        {
            //processes all loaded modules that have requested to add suits, adding those suits to the game
            //called after the game scene has loaded, right before the first update frame (just as the game actually starts)
            [HarmonyPatch("Start")]
            [HarmonyPrefix]
            private static void acsAdditionalContentPatch(StartOfRound __instance)
            {
                try
                {
                    //halt if suits are already loaded
                    if (IsContentLoaded) return;

                    //process all unlockable items to find the default suit (one day i'll find your're ID)
                    AddLog("finding suit prefab...");
                    for (int i = 0; i < __instance.unlockablesList.unlockables.Count; i++)
                    {
                        //get reference to current unlockable item
                        UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[i];

                        //ensure item has a suit material and is unlocked (we want to target the default suit)
                        if (unlockableItem.suitMaterial != null && unlockableItem.alreadyUnlocked)
                        {
                            PrefabUnlockableSuit = unlockableItem;
                            AddLog("found suit prefab!");
                            break;
                        }
                    }

                    //ensure suit prefab was found
                    if (PrefabUnlockableSuit == null)
                    {
                        AddLog("ERROR: suit prefab was not found!");
                        return;
                    }

                    //load all valid additional content modules
                    AddLog("loading content modules...");
                    //  grab listing of subdirectories in plugins folder and iterate through each one
                    //  NOTE: we need to do a double-depth check b.c of Thunderstore's mod profile system
                    string parseFolder = Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    string[] subdirectories = Directory.GetDirectories(parseFolder);
                    foreach (string subdirectory in subdirectories)
                    {
                        //parse any sub directories (thunderstore mod profile specific processing)
                        string[] subdirectories2 = Directory.GetDirectories(subdirectory);
                        foreach (string subdirectory2 in subdirectories2)
                        {
                            //if target folder is an additional content resource folder
                            DirectoryInfo directoryInfo2 = new DirectoryInfo(subdirectory2);
                            if (directoryInfo2.Name.StartsWith("res") == true) LoadContentModule(subdirectory2);
                        }

                        //if target folder is an additional content resource folder
                        DirectoryInfo directoryInfo = new DirectoryInfo(subdirectory);
                        if (directoryInfo.Name.StartsWith("res") == true) LoadContentModule(subdirectory);
                        
                    }
                    AddLog("loaded content modules! (count=" + ContentModules.Count + ")");

                    //apply all custom content modules
                    AddLog("applying content modules...");
                    foreach (AdditionalContentModule contentModule in ContentModules)
                    {
                        ApplyContentModule(__instance, contentModule);
                    }
                    AddLog("applied content modules!");

                    //all suits have been added
                    IsContentLoaded = true;
                }
                catch (Exception error)
                {
                    AddLog("initialization failed!\nERROR: " + error);
                }
            }
        }
    }
}
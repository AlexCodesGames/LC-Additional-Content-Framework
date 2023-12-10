using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

/** LETHAL COMPANY MOD - ADDITIONAL SUITS
 *  if you are reading this then you are pretty cool ;)
 *  
 *  this mod adds several additional suits to the game, so you can die in style!
 *  includes: red, yellow, green, blue, purple, pink, white and black suits
 *  
 *  feel free to read/reuse any snippets below to learn and create your own mods!
 *   
 *  discord: the_shadow_wizard
 */
namespace AdditionalSuits
{
    //data container for a single suit def
    [Serializable]
    public class UnlockableSuitDef
    {
        //indexing
        public string suitID;
        //display
        public string suitName;
        public string suitTexture;
    }

    //data container for all suit defs
    public class UnlockableSuitDefListing
    {
        public List<UnlockableSuitDef> unlockableSuits = new List<UnlockableSuitDef>();
    }

    //mod base class
    [BepInPlugin(modGUID, modName, modVersion)]
    public class AdditionalSuitsBase:BaseUnityPlugin
    {
        //singleton instance access (we only ever want 1 base class active at a time)
        private static AdditionalSuitsBase Instance;

        //mod details
        private const string modGUID = "ACS.AdditionalSuits";
        private const string modName = "AdditionalSuits";
        private const string modVersion = "1.1.0";

        //harmony reference
        private readonly Harmony harmony = new Harmony(modGUID);

        //logger reference
        public static ManualLogSource mls;

        //when true mod has loaded (suits have been added)
        public static bool SuitsLoaded;

        //folder reference for suit textures
        public static string ModResourceFolder;
        public static UnlockableSuitDefListing SuitDefManifest;

        //called when mod loads
        void Awake()
        {
            //ensure instance is set
            if(Instance == null) { Instance = this; }
            
            //attach new logger
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo(modName + " - initializing...");

            //set folder location
            ModResourceFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "res"+modName);

            //apply patch via harmony
            harmony.PatchAll();

            mls.LogInfo(modName + " - initialized!");
        }

        //called at the start of every round
        //  we basically load the mod when the first suit (default) in the game is loaded,
        //  duplicating the def of the first suit and just replacing the texture/material.
        [HarmonyPatch(typeof(StartOfRound))]
        internal class StartOfRoundPatch
        {
            [HarmonyPatch("Start")]
            [HarmonyPrefix]
            private static void StartPatch(ref StartOfRound __instance)
            {
                try
                {
                    //halt if suits are already loaded
                    if (SuitsLoaded) return;

                    UnlockableItem suitPrefab = null;

                    //process all unlockable items to find the default suit (one day i'll find your're ID)
                    for (int i = 0; i < __instance.unlockablesList.unlockables.Count; i++)
                    {
                        //get reference to current unlockable item
                        UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[i];

                        //skip if item does not have a suit material or if item is not unlocked (we want to target the default suit)
                        if (unlockableItem.suitMaterial == null || !unlockableItem.alreadyUnlocked)
                        {
                            continue;
                        }

                        suitPrefab = unlockableItem;
                        break;
                    }

                    //populate listing of all items to add to the game
                    //	attempt to load json file
                    string jsonPath = Path.Combine(ModResourceFolder, "suit-defs.json");
                    mls.LogInfo(modName + " - attempting to parse json file: "+jsonPath);
                    string jsonText = File.ReadAllText(jsonPath);
                    if (jsonText == null)
                    {
                        mls.LogInfo(modName + " - ERROR: json file was not found");
                        return;
                    }

                    //translate data from json into parsable classes
                    mls.LogInfo(modName + " - converting json file to manifest...");
                    //NOTE: it does not look like the standard approaches work x_x so we need to be creative~
                    /*SuitDefManifest = JsonUtility.FromJson<UnlockableSuitDefListing>(jsonFile);
                    if (SuitDefManifest == null || SuitDefManifest.unlockableSuits == null)
                    {
                        mls.LogInfo(modName + " - ERROR: failed to convert json file to manifest");
                        return;
                    }*/
                    string[] split1 = jsonText.Split('[');
                    split1 = split1[1].Split(']');
                    split1 = split1[0].Split('{');
                    SuitDefManifest = new UnlockableSuitDefListing();
                    //it was late in the night man .-. just go with it
                    for(int i = 1; i < split1.Length; i++)
                    {
                        string defStr = "{" + split1[i].Trim();
                        if(i != split1.Length-1)
                        {
                            defStr = defStr.Substring(0, defStr.Length - 1);
                        }
                        //mls.LogInfo(modName + " - "+i+": " + defStr);
                        UnlockableSuitDef def = JsonUtility.FromJson<UnlockableSuitDef>(defStr);
                        SuitDefManifest.unlockableSuits.Add(def);
                    }

                    //load custom defs
                    //	process all suit defs, adding them into the game
                    mls.LogInfo(modName + " - loading item defs from manifest, "+SuitDefManifest.unlockableSuits.Count+" items were found...");
                    foreach (UnlockableSuitDef unlockableSuitDef in SuitDefManifest.unlockableSuits)
                    {
                        mls.LogInfo(modName + " - processing custom suit {id=" + unlockableSuitDef.suitID + ", name=" + unlockableSuitDef.suitName + "}...");

                        //create new suit item based on default suit
                        UnlockableItem newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(suitPrefab));

                        //set suit details
                        //  create new texture container
                        Texture2D suitTexture = new Texture2D(2, 2);
                        //  load texture from file
                        ImageConversion.LoadImage(suitTexture, File.ReadAllBytes(Path.Combine(ModResourceFolder, unlockableSuitDef.suitTexture)));
                        //  apply texture to new material
                        Material suitMaterial = Instantiate<Material>(newUnlockableItem.suitMaterial);
                        suitMaterial.mainTexture = suitTexture;
                        //  apply material to suit
                        newUnlockableItem.suitMaterial = suitMaterial;
                        //  set name
                        newUnlockableItem.unlockableName = unlockableSuitDef.suitName;

                        //add new item to the listing of tracked unlockable items
                        __instance.unlockablesList.unlockables.Add(newUnlockableItem);
                        mls.LogInfo(modName + " - added custom suit {id=" + unlockableSuitDef.suitID + ", name=" + unlockableSuitDef.suitName + "}!");
                    }
                    mls.LogInfo(modName + " - loaded item defs from json file!");

                    //all suits have been added
                    SuitsLoaded = true;
                }
                catch (Exception error)
                {
                    mls.LogInfo(modName + " - initialization failed!\nERROR: " + error);
                }
            }
        }
    }
}

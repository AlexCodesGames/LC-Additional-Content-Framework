using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/** LETHAL COMPANY MOD - ADDITIONAL SUITS
 *  if you are reading this then you are pretty cool ;)
 *  
 *  this mod adds several additional suits to the game so you can die in style!
 *  includes: red, yellow, green, blue, purple, pink, and white suits
 *  
 *  feel free to read/reuse any snippets below to learn and create your own mods!
 *   
 *  Discord: the_shadow_wizard
 */
namespace AdditionalSuits
{
    //mod base class
    [BepInPlugin(modGUID, modName, modVersion)]
    public class AdditionalSuitsBase:BaseUnityPlugin
    {
        //singleton instance access (we only ever want 1 base class active at a time)
        private static AdditionalSuitsBase Instance;

        //mod details
        private const string modGUID = "ACS.AdditionalSuits";
        private const string modName = "AdditionalSuits";
        private const string modVersion = "1.0.1";

        //harmony reference
        private readonly Harmony harmony = new Harmony(modGUID);

        //logger reference
        public static ManualLogSource mls;

        //when true mod has loaded (suits have been added)
        public static bool SuitsLoaded;

        //folder reference for suit textures
        public static string SuitTextureFolder;

        //called when mod loads
        void Awake()
        {
            //ensure instance is set
            if(Instance == null) { Instance = this; }
            
            //attach new logger
            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            mls.LogInfo(modName + " - initializing...");

            //set folder location
            SuitTextureFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "res"+modName);

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

                    //process all unlockable items
                    for (int i = 0; i < __instance.unlockablesList.unlockables.Count; i++)
                    {
                        //get reference to current unlockable item
                        UnlockableItem unlockableItem = __instance.unlockablesList.unlockables[i];
                        mls.LogInfo(modName + " - processing unlockable {index=" + i + ", name=" + unlockableItem.unlockableName + "}");

                        //skip if item does not have a suit material or if item is not unlocked
                        if (unlockableItem.suitMaterial == null || !unlockableItem.alreadyUnlocked)
                        {
                            continue;
                        }

                        //get reference to all additional suit textures from folder location and begin processing
                        string[] suitTextureFiles = Directory.GetFiles(SuitTextureFolder, "*.png");
                        foreach (string suitTextureFile in suitTextureFiles)
                        {
                            mls.LogInfo(modName + " - adding suit {file=" + suitTextureFile + "}");

                            //references for new suit item
                            UnlockableItem newUnlockableItem;
                            Material suitMaterial;

                            //create new suit item based on default suit
                            newUnlockableItem = JsonUtility.FromJson<UnlockableItem>(JsonUtility.ToJson(unlockableItem));
                            suitMaterial = Instantiate<Material>(newUnlockableItem.suitMaterial);

                            //create new texture for suit
                            Texture2D suitTexture = new Texture2D(2, 2);
                            ImageConversion.LoadImage(suitTexture, File.ReadAllBytes(Path.Combine(SuitTextureFolder, suitTextureFile)));

                            //apply new texture to material
                            suitMaterial.mainTexture = suitTexture;

                            //set unlockable item details
                            newUnlockableItem.suitMaterial = suitMaterial;

                            //prepare and set name
                            string nameStr = Path.GetFileNameWithoutExtension(suitTextureFile);
                            newUnlockableItem.unlockableName = nameStr.Replace("_", " ").Substring(2);

                            //add new item to the listing of tracked unlockable items
                            __instance.unlockablesList.unlockables.Add(newUnlockableItem);
                        }

                        //all suits have been added
                        SuitsLoaded = true;
                        break;
                    }
                }
                catch (Exception error)
                {
                    mls.LogInfo(modName + " - initialization failed!\nERROR: " + error);
                }
            }
        }
    }
}

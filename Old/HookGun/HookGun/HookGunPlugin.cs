using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using HookGun.Patches;
using LethalLib.Modules;
using ReservedItemSlotCore.Config;
using ReservedItemSlotCore.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace HookGun
{
    [BepInDependency("FlipMods.ReservedItemSlotCore", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginInfo.MODUID3, PluginInfo.MODNAME3, PluginInfo.MODVERSION)]
    public class HookGunPlugin : BaseUnityPlugin
    {

        private readonly Harmony harmony = new Harmony(PluginInfo.MODUID);

        private static HookGunPlugin Instance;

        internal ManualLogSource mls;

        public static ConfigEntry<int> itemPrice { get; set; }

        public static ConfigEntry<float> itemCooldown { get; set; }

        public static ConfigEntry<float> energyCost { get; set; }




        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            mls = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.MODUID);

            bool isRSModLoaded = Chainloader.PluginInfos.Any(plugin => plugin.Key == "FlipMods.ReservedItemSlotCore");

            Debug.Log("isRSModLoaded: " + isRSModLoaded);

            //mls.LogInfo("HookGun Awaken!");


            Assets.LoadAssetBundle();
            //Assets.LoadSoundbank();
            Assets.PopulateAssets();

            harmony.PatchAll(typeof(HookGunPlugin));
            harmony.PatchAll(typeof(HookGunMain));
            harmony.PatchAll(typeof(AllowDeathPatch));
            //harmony.PatchAll(typeof(RSHookGunMain));

            if (isRSModLoaded)
            {
                try
                {
                    // Carrega o assembly manualmente, se necessário
                    Assembly reservedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => a.GetName().Name == "ReservedItemSlotCore");

                    if (reservedAssembly == null)
                    {
                        Logger.LogError("Assembly 'ReservedItemSlotCore' não encontrado, mesmo com mod detectado.");
                        return;
                    }

                    // Tenta carregar o tipo com namespace completo
                    Type reservedSlotType = reservedAssembly.GetType("ReservedItemSlotCore.Data.ReservedItemSlotData");
                    Type reservedItemType = reservedAssembly.GetType("ReservedItemSlotCore.Data.ReservedItemData");

                    if (reservedSlotType != null)
                    {
                        harmony.PatchAll(reservedSlotType);
                        Logger.LogInfo("Patch aplicado para ReservedItemSlotData.");
                    }
                    else
                    {
                        Logger.LogError("Tipo 'ReservedItemSlotData' não encontrado, mesmo com mod detectado.");
                    }

                    if (reservedItemType != null)
                    {
                        harmony.PatchAll(reservedItemType);
                        Logger.LogInfo("Patch aplicado para ReservedItemSlotData.");
                    }
                    else
                    {
                        Logger.LogError("Tipo 'ReservedItemData' não encontrado, mesmo com mod detectado.");
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogError($"Erro ao aplicar patch para ReservedItemSlotData: {ex.Message}");
                }
            }
            else
            {
                Logger.LogInfo("Outro mod não detectado. Prosseguindo sem ReservedItemSlotData.");
            }

            //if (isRSModLoaded)
            //{
            //    try
            //    {
            //        // Localiza o assembly do mod `ReservedItemSlotCore`
            //        Assembly reservedAssembly = AppDomain.CurrentDomain.GetAssemblies()
            //            .FirstOrDefault(a => a.GetName().Name == "ReservedItemSlotCore");

            //        if (reservedAssembly != null)
            //        {
            //            // Lista todos os tipos e namespaces dentro do assembly
            //            Logger.LogInfo("Tipos disponíveis em ReservedItemSlotCore:");
            //            foreach (Type type in reservedAssembly.GetTypes())
            //            {
            //                Logger.LogInfo($"Namespace: {type.Namespace}, Tipo: {type.Name}");
            //            }
            //        }
            //        else
            //        {
            //            Logger.LogError("Assembly 'ReservedItemSlotCore' não encontrado, mesmo com mod detectado.");
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Logger.LogError($"Erro ao listar tipos de ReservedItemSlotCore: {ex.Message}");
            //    }
            //}
            //else
            //{
            //    Logger.LogInfo("Outro mod não detectado.");
            //}




            //harmony.PatchAll();


            // netcode patching stuff
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }



            //-----------------CONFIGURATION ------------------

            itemPrice = Config.Bind<int>(
            "ItemPrice",
            "Price",
                25,
            "This is the item price, my default is 25, [INTEGER 1,2,30...]"
            );


            itemCooldown = Config.Bind<float>(
            "ItemCooldown",
            "Cooldown",
                2f,
            "This is the item cooldown, my default is 2, [FLOAT 0.5,1.8,18.9,...]"
            );

            energyCost = Config.Bind<float>(
            "EnergyCost",
            "Cost",
                0.05f,
            "This is the item energy cost of activation, my default is 0.05, the MAX energy is 1f so 0.05 give you 20 sucessfull activations [FLOAT 0.01,0.1,1 -- DON'T USE MORE THAN 1]"
            );



        }




        public class Assets
        {

            //-------------------ASSETS

            internal static AssetBundle mainAssetBundle;

            // CHANGE THIS
            private const string assetbundleName = "hookgunitem";

            private static string[] assetNames = new string[0];

            public static Item HGItem;
            public static Item HGItemRS;

            public static Sprite HGSprite;

            public static AudioClip ShootSFX;
            public static AudioClip HitSFX;
            public static AudioClip MissSFX;
            public static AudioClip NoAmmoSFX;

            public static Mesh HGMesh;
            public static Mesh HOMesh;

            internal static void LoadAssetBundle()
            {
                if (mainAssetBundle == null)
                {
                    using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HookGun." + assetbundleName))
                    {
                        mainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                    }
                }

                assetNames = mainAssetBundle.GetAllAssetNames();
            }


            internal static void PopulateAssets()
            {
                if (!mainAssetBundle)
                {
                    Debug.LogError("There is no AssetBundle to load assets from.");
                    return;
                }


                HGSprite = mainAssetBundle.LoadAsset<Sprite>("HGSprite");

                ShootSFX = mainAssetBundle.LoadAsset<AudioClip>("ShootSFX");
                HitSFX = mainAssetBundle.LoadAsset<AudioClip>("HitSFX");
                MissSFX = mainAssetBundle.LoadAsset<AudioClip>("MissSFX");
                NoAmmoSFX = mainAssetBundle.LoadAsset<AudioClip>("NoAmmoSFX");

                HGItem = mainAssetBundle.LoadAsset<Item>("HookGunItem");
                HGItemRS = mainAssetBundle.LoadAsset<Item>("RSHookGunItem");


            }

        }


    }
}

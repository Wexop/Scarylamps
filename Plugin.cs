using System;
using System.Collections.Generic;
using BepInEx;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using UnityEngine;
using LethalLib.Modules;
using ScaryLamps.Utils;

namespace ScaryLamps
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class ScaryLampsPlugin : BaseUnityPlugin
    {

        const string GUID = "wexop.scary_lamps";
        const string NAME = "ScaryLamps";
        const string VERSION = "1.0.0";
        
        public ConfigEntry<string> spawnMoonRarity;
        public ConfigEntry<int> scaryLampDamage;

        public static ScaryLampsPlugin instance;

        void Awake()
        {
            instance = this;
            
            Logger.LogInfo($"ScaryLamps starting....");

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "scarylamps");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);
            
            Logger.LogInfo($"ScaryLamps bundle found !");
            
            NetcodePatcher();
            LoadConfigs();
            RegisterMonster(bundle);
            
            
            Logger.LogInfo($"ScaryLamps is ready!");
        }

        string RarityString(int rarity)
        {
            return
                $"Modded:{rarity},ExperimentationLevel:{rarity},AssuranceLevel:{rarity},VowLevel:{rarity},OffenseLevel:{rarity},MarchLevel:{rarity},RendLevel:{rarity},DineLevel:{rarity},TitanLevel:{rarity},Adamance:{rarity},Embrion:{rarity},Artifice:{rarity},All:{rarity}";

        }

        void LoadConfigs()
        {
            
            //GENERAL
            
            spawnMoonRarity = Config.Bind("ScaryLampRarity", "ScaryLampRarity", 
                RarityString(40),           
                "Chance for Scary Lamp to spawn for any moon, example => assurance:100,offense:50 . You need to restart the game.");
            CreateStringConfig(spawnMoonRarity, true);
            
            scaryLampDamage = Config.Bind("ScaryLampRarity", "ScaryLampDamage", 
                10,           
                "Damage for each tick of ScaryLamp attack . No need to restart the game.");
            CreateIntConfig(scaryLampDamage, 1, 100);
 
        }
        
        void RegisterMonster(AssetBundle bundle)
        {
            //Scary Lamp
            EnemyType scaryLamp = bundle.LoadAsset<EnemyType>("Assets/LethalCompany/Mods/ScaryLamps/LampMonster/ScaryLamp.asset");
            
            Logger.LogInfo($"{scaryLamp.name} FOUND");
            Logger.LogInfo($"{scaryLamp.enemyPrefab} prefab");
            NetworkPrefabs.RegisterNetworkPrefab(scaryLamp.enemyPrefab);
            Utilities.FixMixerGroups(scaryLamp.enemyPrefab);
            
            TerminalNode terminalNodeScaryLamp = new TerminalNode();
            terminalNodeScaryLamp.creatureName = "ScaryLamp";
            terminalNodeScaryLamp.displayText = "Don't wake him, or he will be very angry...";

            TerminalKeyword terminalKeywordScaryLamp = new TerminalKeyword();
            terminalKeywordScaryLamp.word = "ScaryLamp";
            
            
            RegisterUtil.RegisterEnemyWithConfig(spawnMoonRarity.Value, scaryLamp,terminalNodeScaryLamp , terminalKeywordScaryLamp, scaryLamp.PowerLevel, scaryLamp.MaxCount);
        }
        
        /// <summary>
        ///     Slightly modified version of: https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
        /// </summary>
        private static void NetcodePatcher()
        {
            Type[] types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // This goofy try catch is needed here to be able to use soft dependencies in the future, though none are present at the moment.
                types = e.Types.Where(type => type != null).ToArray();
            }

            foreach (Type type in types)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false).Length > 0)
                    {
                        // Do weird magic...
                        _ = method.Invoke(null, null);
                    }
                }
            }
        }
        private void CreateFloatConfig(ConfigEntry<float> configEntry, float min = 0f, float max = 100f)
        {
            var exampleSlider = new FloatSliderConfigItem(configEntry, new FloatSliderOptions
            {
                Min = min,
                Max = max,
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateIntConfig(ConfigEntry<int> configEntry, int min = 0, int max = 100)
        {
            var exampleSlider = new IntSliderConfigItem(configEntry, new IntSliderOptions()
            {
                Min = min,
                Max = max,
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        private void CreateStringConfig(ConfigEntry<string> configEntry, bool requireRestart = false)
        {
            var exampleSlider = new TextInputFieldConfigItem(configEntry, new TextInputFieldOptions()
            {
                RequiresRestart = requireRestart
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
        public bool StringContain(string name, string verifiedName)
        {
            var name1 = name.ToLower();
            while (name1.Contains(" ")) name1 = name1.Replace(" ", "");

            var name2 = verifiedName.ToLower();
            while (name2.Contains(" ")) name2 = name2.Replace(" ", "");

            return name1.Contains(name2);
        }
        
        private void CreateBoolConfig(ConfigEntry<bool> configEntry)
        {
            var exampleSlider = new BoolCheckBoxConfigItem(configEntry, new BoolCheckBoxOptions
            {
                RequiresRestart = false
            });
            LethalConfigManager.AddConfigItem(exampleSlider);
        }
        
    }
}
using System;
using UnityEngine;
using HarmonyLib;

namespace Liquid_Filtration_Machine
{
    /// <summary>
    /// Entry point of the mod. The loader finds the one class implementing IMod, creates it,
    /// applies the [HarmonyPatch] classes of this assembly, then calls Init and OnEnable.
    /// All of that happens in the main menu: no save is loaded yet, so do not touch game
    /// state here. Subscribe to ModHook events instead (see ModdingGuide.md, "Hook Points").
    /// </summary>
    public class ModEntry : IMod
    {
        public static ModEntry Instance { get; private set; }
        public ModManifest Manifest { get; private set; }
        public ModLog Log { get; private set; }

        private void RegisterItems(ModItemDirectory directory)
        {
            directory.Add(
                LiquidFiltrationMachine.ItemId,
                LiquidFiltrationMachine.CreateMachine
            );

            Log.Log("Liquid Filtration Machine registered.");
        }

        public void Init(ModManifest manifest)
        {
            Instance = this;
            Manifest = manifest;
            Log = new ModLog(manifest);
            Log.Log($"{manifest.Name} v{manifest.ModVersion} loaded.");
        }

        public void OnEnable()
        {
            ModHook.OnModItemDirectoryInit += RegisterItems;
            LiquidFiltrationMerchants.Register();
            Log.Log("Enabled.");
        }

        public void OnDisable()
        {
            ModHook.OnModItemDirectoryInit -= RegisterItems;
            LiquidFiltrationMerchants.Unregister();
            Log.Log("Disabled.");
        }

        // private void OnGameLoaded()
        // {
        //     // A save is loaded: game systems exist from here on.
        // }
    }
}

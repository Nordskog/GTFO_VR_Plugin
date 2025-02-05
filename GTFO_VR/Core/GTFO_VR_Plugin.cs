﻿using BepInEx;
using BepInEx.IL2CPP;
using GTFO_VR.Core.PlayerBehaviours;
using GTFO_VR.Core.UI;
using GTFO_VR.Core.UI.Terminal.Pointer;
using GTFO_VR.Core.UI.Terminal;
using GTFO_VR.Core.VR_Input;
using GTFO_VR.Detours;
using GTFO_VR.UI;
using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics;
using GTFO_VR.Core.PlayerBehaviours.BodyHaptics;
using GTFO_VR.Core.PlayerBehaviours.BodyHaptics.Bhaptics;
using GTFO_VR.Core.PlayerBehaviours.BodyHaptics.Shockwave;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using GTFO_VR.Util;

namespace GTFO_VR.Core
{
    /// <summary>
    /// Main entry point of the mod. Responsible for managing the config and running all patches if the mod is enabled.
    /// </summary>
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class GTFO_VR_Plugin : BasePlugin
    {
        public const string
            MODNAME = "GTFO_VR_Plugin",
            AUTHOR = "Spartan",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.3.3";

        public override void Load()
        {
            Core.Log.Setup(BepInEx.Logging.Logger.CreateLogSource(MODNAME));
            Core.Log.Info($"Loading VR plugin v.{VERSION}");

            VRConfig.SetupConfig(Config);

            if (SteamVRRunningCheck())
            {
                InjectVR();
            }
            else
            {
                Log.LogWarning("VR launch aborted, VR is disabled or SteamVR is off!");
            }
        }

        private void InjectVR()
        {
            SetupIL2CPPClassInjections();
            TerminalInputDetours.HookAll();
            BioscannerDetours.HookAll();
            HammerAttackCheckDetour.HookAll();
            InjectPlayerHudEventsDetour.HookAll();

            Harmony harmony = new Harmony("com.github.dsprtn.gtfovr");
            harmony.PatchAll();
        }

        private void SetupIL2CPPClassInjections()
        {
            ClassInjector.RegisterTypeInIl2Cpp<VRSystems>();
            ClassInjector.RegisterTypeInIl2Cpp<VRAssets>();
            ClassInjector.RegisterTypeInIl2Cpp<VRKeyboard>();
            ClassInjector.RegisterTypeInIl2Cpp<VR_UI_Overlay>();
            ClassInjector.RegisterTypeInIl2Cpp<VRWorldSpaceUI>();
            ClassInjector.RegisterTypeInIl2Cpp<Controllers>();
            ClassInjector.RegisterTypeInIl2Cpp<HMD>();
            ClassInjector.RegisterTypeInIl2Cpp<VRRendering>();
            ClassInjector.RegisterTypeInIl2Cpp<CollisionFade>();
            ClassInjector.RegisterTypeInIl2Cpp<LaserPointer>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerOrigin>();
            ClassInjector.RegisterTypeInIl2Cpp<VRPlayer>();
            ClassInjector.RegisterTypeInIl2Cpp<Haptics>();
            ClassInjector.RegisterTypeInIl2Cpp<BodyHapticsIntegrator>();
            ClassInjector.RegisterTypeInIl2Cpp<ElevatorSequenceIntegrator>();
            ClassInjector.RegisterTypeInIl2Cpp<Snapturn>();
            ClassInjector.RegisterTypeInIl2Cpp<Watch>();
            ClassInjector.RegisterTypeInIl2Cpp<VRMeleeWeapon>();
            ClassInjector.RegisterTypeInIl2Cpp<DividedBarShaderController>();
            ClassInjector.RegisterTypeInIl2Cpp<MovementVignette>();
            ClassInjector.RegisterTypeInIl2Cpp<RadialMenu>();
            ClassInjector.RegisterTypeInIl2Cpp<RadialItem>();
            ClassInjector.RegisterTypeInIl2Cpp<WeaponRadialMenu>();
            ClassInjector.RegisterTypeInIl2Cpp<WeaponAmmoHologram>();

            ClassInjector.RegisterTypeInIl2Cpp<MonoPointerEvent>();
            ClassInjector.RegisterTypeInIl2Cpp<RoundedCubeBackground>();       
            ClassInjector.RegisterTypeInIl2Cpp<PhysicalButton>();              
            ClassInjector.RegisterTypeInIl2Cpp<TerminalKeyboardCanvas>();      
            ClassInjector.RegisterTypeInIl2Cpp<TerminalPointer>();
            ClassInjector.RegisterTypeInIl2Cpp<TerminalKeyboardInterface>();
            ClassInjector.RegisterTypeInIl2Cpp<TerminalReader>();

            ClassInjector.RegisterTypeInIl2Cpp<GTFODebugDraw3D>();
        }

        private bool SteamVRRunningCheck()
        {
            if (!VRConfig.configCheckSteamVR.Value)
            {
                return true;
            }
            List<Process> possibleVRProcesses = new List<Process>();
            
            possibleVRProcesses.AddRange(Process.GetProcessesByName("vrserver"));
            possibleVRProcesses.AddRange(Process.GetProcessesByName("vrcompositor"));

            Core.Log.Debug("VR processes found - " + possibleVRProcesses.Count);
            foreach (Process p in possibleVRProcesses)
            {
                Core.Log.Debug(p.ToString());
            }
            return possibleVRProcesses.Count > 0;
        }
    }
}
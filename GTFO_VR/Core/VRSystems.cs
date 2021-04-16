﻿using GTFO_VR.Core.PlayerBehaviours;
using GTFO_VR.Core.UI;
using GTFO_VR.Core.VR_Input;
using GTFO_VR.Events;
using Player;
using System;
using UnityEngine;
using Valve.VR;

namespace GTFO_VR.Core
{
    /// <summary>
    /// Responsible for seting up all VR related classes and handling focus state changes.
    /// </summary>
    public class VRSystems : MonoBehaviour
    {
        public VRSystems(IntPtr value)
: base(value) { }

        public static VRSystems Current;
        private static VR_UI_Overlay m_overlay;

        private VRPlayer m_player;

        private static FPSCamera m_currentFPSCameraRef;
        private static PlayerAgent m_currentPlayerAgentRef;

        private void Awake()
        {
            if (Current)
            {
                Log.Error("Trying to create duplicate VRSystems class!");
                return;
            }
            Current = this;

            // Prevent SteamVR from adding a tracking script automatically. We handle this manually in VR_Input.HMD
            SteamVR_Camera.useHeadTracking = false;
            SteamVR_Settings.instance.poseUpdateMode = SteamVR_UpdateModes.OnLateUpdate;

            FocusStateEvents.OnFocusStateChange += FocusChanged;
            Setup();
        }

        private void Setup()
        {
            DontDestroyOnLoad(gameObject);

            SteamVR.Initialize(false);
            var res = SteamVR_Camera.GetSceneResolution();
            Log.Info($"SteamVR Setup - HMD Res: {res.width}x{res.height}");
            WeaponArchetypeVRData.Setup();
            SteamVR_InputHandler.Setup();
            gameObject.AddComponent<HMD>();
            gameObject.AddComponent<Controllers>();
            gameObject.AddComponent<VRKeyboard>();
            gameObject.AddComponent<VRAssets>();
            // Delay the overlay setup so we don't 'hitch' the player's camera while everything else loads.
            Invoke(nameof(VRSystems.SetupOverlay), .5f);
            GuiManager.Current.OnResolutionChange(new Resolution());
        }

        public static void OnPlayerSpawned(FPSCamera fpsCamera, PlayerAgent playerAgent)
        {
            m_currentFPSCameraRef = fpsCamera;
            m_currentPlayerAgentRef = playerAgent;
        }

        private void SetupOverlay()
        {
            GameObject o = new GameObject("Overlay");
            DontDestroyOnLoad(o);
            m_overlay = o.AddComponent<VR_UI_Overlay>();
        }

        /// <summary>
        /// Prevent reprojection when entering/exiting map or menu by clearing the associated UI render texture
        /// </summary>
        public static void ClearUIRenderTex()
        {
            RenderTexture rt = RenderTexture.active;
            RenderTexture.active = UI_Core.UIPassHUD.Camera.targetTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = rt;
        }

        public void FocusChanged(eFocusState state)
        {
            if (state.Equals(eFocusState.FPS) || state.Equals(eFocusState.InElevator))
            {
                HandleIngameFocus();
            }

            if (state.Equals(eFocusState.MainMenu) || state.Equals(eFocusState.Map))
            {
                HandleOutOfGameFocus();
            }
            ClearUIRenderTex();
        }

        private void HandleOutOfGameFocus()
        {
            if (!m_overlay)
            {
                return;
            }
            Log.Debug("Focus change - Enabling overlay/Disabling player");
            ToggleOverlay(true);
            TogglePlayerCam(false);
        }

        private void HandleIngameFocus()
        {
            if (!m_overlay)
            {
                return;
            }
        
            if (m_player == null)
            {
                AppendVRComponents();
            }

            ToggleOverlay(false);
            TogglePlayerCam(true);
        }

        private void AppendVRComponents()
        {
            
            if (m_currentFPSCameraRef == null)
            {
                Log.Error("Tried to spawn player but FPS camera ref was null!");
            }

            if (m_currentPlayerAgentRef == null)
            {
                Log.Error("Tried to spawn player but playeragent ref was null!");
            }
            m_player = m_currentFPSCameraRef.gameObject.AddComponent<VRPlayer>();
            m_player.Setup(m_currentFPSCameraRef, m_currentPlayerAgentRef);
            
        }

        private void ToggleOverlay(bool toggle)
        {
            if (!toggle)
            {
                m_overlay.DestroyOverlay();
            }
            else
            {
                m_overlay.SetupOverlay();
            }

            m_overlay.gameObject.SetActive(toggle);
            m_overlay.OrientateOverlay();
        }

        private void TogglePlayerCam(bool toggle)
        {
            SteamVR_Render.pauseRendering = !toggle;
        }

        private void OnDestroy()
        {
            FocusStateEvents.OnFocusStateChange -= FocusChanged;
        }
    }
}
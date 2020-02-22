﻿

using UnityEngine;
using Valve.VR;

namespace GTFO_VR.Input
{
    public class HMD : MonoBehaviourExtended
    {
        public static GameObject hmd;

        SteamVR_TrackedObject tracking;

        void Awake()
        {
            SetupHMDObject();
        }

        private void SetupHMDObject()
        {
            hmd = new GameObject("HMD_ORIGIN");
            tracking = hmd.AddComponent<SteamVR_TrackedObject>();
            tracking.index = SteamVR_TrackedObject.EIndex.Hmd;

            UnityEngine.Object.DontDestroyOnLoad(hmd);
        }

        public static Vector3 GetPosition()
        {
            Vector3 position = hmd.transform.position; 


            // TODO Incorporate origin and crouching a little better
            //if(PlayerVR.LoadedAndInGame && PlayerVR.playerAgent)
           // {
           //     if(PlayerVR.playerAgent.Locomotion.m_currentStateEnum.Equals(Player.PlayerLocomotion.PLOC_State.Crouch))
            //    {
           //         position.y = Mathf.Min(position.y, 1.1f);
           //     }
           // }
            return position;
        }

        

        public static Vector3 GetVRCameraEulerRotation()
        {
            Quaternion localRotation = hmd.transform.rotation;
            // TODO Snaprot
            // TODO Incorporate origin into transform code
            //localRotation *= snapTurnRot;
            if(!PlayerVR.fpscamera || FocusStateManager.CurrentState.Equals(eFocusState.InElevator))
            {
                return localRotation.eulerAngles;
            }
            localRotation = Quaternion.Inverse(PlayerVR.fpscamera.m_holder.transform.rotation) * localRotation;
            // Get local rotation for FPS Camera from world hmd rotation to keep using the game's systems and keep player rotation in multiplayer in sync
            //if (PlayerVR.LoadedAndInGame && PlayerVR.fpscamera && !FocusStateManager.CurrentState.Equals(eFocusState.InElevator))
            // {
            //     localRotation = Quaternion.Inverse(PlayerVR.fpscamera.m_holder.transform.rotation) * localRotation;
            // }
            return localRotation.eulerAngles;
        }
    }
}

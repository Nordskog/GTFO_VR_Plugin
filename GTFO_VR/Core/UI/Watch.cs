﻿using GTFO_VR.Core;
using GTFO_VR.Core.VR_Input;
using GTFO_VR.Events;
using GTFO_VR.Util;
using Player;
using SteamVR_Standalone_IL2CPP.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Mathf = SteamVR_Standalone_IL2CPP.Util.Mathf;

namespace GTFO_VR.UI
{
    /// <summary>
    /// Handles all VR watch UI related functions
    /// </summary>
    
    // ToDO - Refactor this into something more manageable, or not, if no new UI is planned.

    public class Watch : MonoBehaviour
    {

        public Watch(IntPtr value)
: base(value) { }

        enum WatchState
        {
            Inventory,
            Objective
        }

        MeshRenderer[] inventoryMeshes;
        WatchState currentState = WatchState.Inventory;

        Vector3 handOffset = new Vector3(0, -.05f, -.15f);
        Quaternion handRotationOffset = Quaternion.Euler(new Vector3(205, -100f, -180f));

        static TextMeshPro objectiveDisplay;
        Dictionary<InventorySlot, DividedBarShaderController> UIMappings = new Dictionary<InventorySlot, DividedBarShaderController>();

        DividedBarShaderController BulletsInMag;
        TextMeshPro numberAmmoDisplay;

        static DividedBarShaderController Health;
        static DividedBarShaderController Infection;
        static DividedBarShaderController Oxygen;

        static readonly Color normalHealthCol = new Color(0.66f, 0f, 0f);
        static readonly Color normalInfectionCol = new Color(0.533f, 1, 0.8f);
        static readonly Color normalOxygenCol = Color.cyan;

        static string mainObj;
        static string subObj;

        void Awake()
        {
            ItemEquippableEvents.OnPlayerWieldItem += ItemSwitched;
            InventoryAmmoEvents.OnInventoryAmmoUpdate += AmmoUpdate;

            Setup();
        }

        void Start()
        {
            transform.GetChild(0).GetComponent<MeshRenderer>().material.color = VR_Settings.watchColor;
        }

        void Update()
        {
            if (SteamVR_InputHandler.GetActionDown(InputAction.Aim))
            {
                SwitchState();
            }
        }
        public static void UpdateMainObjective(string mainObj)
        {
            Watch.mainObj = mainObj;
            Watch.UpdateObjectiveDisplay();
        }

        public static void UpdateSubObjective(string subObj)
        {
            Watch.subObj = subObj;
            Watch.UpdateObjectiveDisplay();
        }

        public static void UpdateObjectiveDisplay() {
            if (objectiveDisplay != null)
            {
                objectiveDisplay.text = "WARDEN OBJECTIVE: \n \n " + mainObj + " \n \n " + subObj;
                objectiveDisplay.ForceMeshUpdate(false);
                SteamVR_InputHandler.TriggerHapticPulse(0.01f, 1 / .025f, 0.2f, Controllers.GetDeviceFromHandType(Controllers.offHandControllerType));
            }
        }

        public static void UpdateInfection(float infection)
        {
            if (Infection)
            {
                if (infection < 0.01f)
                {
                    Infection.ToggleRendering(false);
                } else
                {
                    Infection.ToggleRendering(true);
                    Infection.SetFill(infection);
                    Infection.SetColor(Color.Lerp(normalInfectionCol, normalInfectionCol * 1.6f, infection));
                }
            }
        }

        public static void UpdateHealth(float health)
        {
            if (Health)
            {
                Health.SetFill(health);
                Health.SetColor(Color.Lerp(normalHealthCol, normalHealthCol * 1.8f, 1 - health));
            }
        }

        public static void UpdateAir(float val)
        {
            if (Oxygen)
            {
                if (val < .95f)
                {
                    Oxygen.SetFill(val);
                    Oxygen.ToggleRendering(true);

                    if (val < 0.5)
                    {
                        Oxygen.SetColor(Color.Lerp(Color.red, normalOxygenCol, val * 1.6f));
                    }
                    else
                    {
                        Oxygen.SetColor(Color.cyan);
                    }
                } else
                {
                    Oxygen.ToggleRendering(false);
                }
            }
        }
        private void ItemSwitched(ItemEquippable item)
        {
            HandleSelectionEffect(item);
            UpdateBulletGridDivisions(item);
        }

        private void AmmoUpdate(InventorySlotAmmo item, int clipLeft)
        {
            UpdateBulletDisplayAmount(item, clipLeft);
            UpdateInventoryAmmoGrids(item, clipLeft);
        }

        private void HandleSelectionEffect(ItemEquippable item)
        {
            foreach (DividedBarShaderController d in UIMappings.Values)
            {
                d.SetUnselected();
            }
            UIMappings.TryGetValue(item.ItemDataBlock.inventorySlot, out DividedBarShaderController UIBar);

            if (UIBar)
            {
                UIBar.SetSelected();
            }
        }

        private void UpdateInventoryAmmoGrids(InventorySlotAmmo item, int clipLeft)
        {
            UIMappings.TryGetValue(item.Slot, out DividedBarShaderController bar);
            if (bar)
            {
                bar.maxValue = item.BulletsMaxCap;
                bar.currentValue = (int)(bar.maxValue * item.RelInPack) + clipLeft;
                bar.SetFill(item.RelInPack);

                if (item.Slot.Equals(InventorySlot.GearStandard) || item.Slot.Equals(InventorySlot.GearSpecial))
                {
                    bar.UpdateWeaponMagDivisions(item.BulletClipSize, item.BulletsMaxCap);
                }

                if (item.Slot.Equals(InventorySlot.Consumable) || item.Slot.Equals(InventorySlot.ResourcePack) || item.Slot.Equals(InventorySlot.ConsumableHeavy))
                {
                    bar.UpdatePackOrConsumableDivisions();
                }
            }
        }

        private void UpdateBulletDisplayAmount(InventorySlotAmmo item, int clipLeft)
        {
            if (ItemEquippableEvents.IsCurrentItemShootableWeapon() &&
                ItemEquippableEvents.currentItem.ItemDataBlock.inventorySlot.Equals(item.Slot))
            {
                if (VR_Settings.useNumbersForAmmoDisplay)
                {
                    numberAmmoDisplay.text = clipLeft + "\n----\n" + ((int)(item.BulletsMaxCap * item.RelInPack)).ToString();
                    numberAmmoDisplay.ForceMeshUpdate(false);
                } else
                {
                    BulletsInMag.maxValue = Mathf.Max(item.BulletClipSize, 1);
                    BulletsInMag.UpdateCurrentAmmo(clipLeft);
                    BulletsInMag.UpdateAmmoGridDivisions();
                }
            }
        }

        private void UpdateBulletGridDivisions(ItemEquippable item)
        {

            if (ItemEquippableEvents.IsCurrentItemShootableWeapon())
            {
                if (!VR_Settings.useNumbersForAmmoDisplay)
                {
                    BulletsInMag.maxValue = item.GetMaxClip();
                    BulletsInMag.currentValue = item.GetCurrentClip();
                    BulletsInMag.UpdateAmmoGridDivisions();
                }
            }
            else
            {
                if (!VR_Settings.useNumbersForAmmoDisplay)
                {
                    BulletsInMag.currentValue = 0;
                    BulletsInMag.UpdateShaderVals(1, 1);
                } else
                {
                    numberAmmoDisplay.text = "";
                    numberAmmoDisplay.ForceMeshUpdate(false);
                }

            }
        }

        private void Setup()
        {
            inventoryMeshes = Utils.FindDeepChild(transform, "Inventory_UI").GetComponentsInChildren<MeshRenderer>();

            SetupTransform();
            SetupObjectiveDisplay();
            SetupInventoryLinkData();
            SetInitialPlayerStatusValues();
            SwitchState(currentState);
        }

        private void SetupTransform()
        {
            transform.SetParent(Controllers.offhandController.transform);
            transform.localPosition = handOffset;
            transform.localRotation = handRotationOffset;
        }

        private void SetupObjectiveDisplay()
        {
            GameObject objectiveParent = Utils.FindDeepChild(transform, "WardenObjective").gameObject;

            RectTransform watchObjectiveTransform = objectiveParent.GetComponent<RectTransform>();
            objectiveDisplay = objectiveParent.AddComponent<TextMeshPro>();

            objectiveDisplay.enableAutoSizing = true;
            objectiveDisplay.fontSizeMin = 18;
            objectiveDisplay.fontSizeMax = 36;
            objectiveDisplay.alignment = TextAlignmentOptions.Center;
            MelonCoroutines.Start(SetRectSize(watchObjectiveTransform, new Vector2(42, 34f)));
        }

        IEnumerator SetRectSize(RectTransform t, Vector2 size)
        {
            yield return new WaitForEndOfFrame();
            t.sizeDelta = size;
        }

        private void SetupInventoryLinkData()
        {
            UIMappings.Add(InventorySlot.GearStandard, Utils.FindDeepChild(transform, "MainWeapon").gameObject.AddComponent<DividedBarShaderController>());
            UIMappings.Add(InventorySlot.GearSpecial, Utils.FindDeepChild(transform, "SubWeapon").gameObject.AddComponent<DividedBarShaderController>());
            UIMappings.Add(InventorySlot.GearClass, Utils.FindDeepChild(transform, "Tool").gameObject.AddComponent<DividedBarShaderController>());
            UIMappings.Add(InventorySlot.ResourcePack, Utils.FindDeepChild(transform, "Pack").gameObject.AddComponent<DividedBarShaderController>());
            UIMappings.Add(InventorySlot.Consumable, Utils.FindDeepChild(transform, "Consumable").gameObject.AddComponent<DividedBarShaderController>());
            UIMappings.Add(InventorySlot.ConsumableHeavy, UIMappings[InventorySlot.Consumable]);

            Health = Utils.FindDeepChild(transform, "HP").gameObject.AddComponent<DividedBarShaderController>();
            Oxygen = Utils.FindDeepChild(transform, "Air").gameObject.AddComponent<DividedBarShaderController>();
            Infection = Utils.FindDeepChild(transform, "Infection").gameObject.AddComponent<DividedBarShaderController>();

            numberAmmoDisplay = Utils.FindDeepChild(transform, "NumberedAmmo").gameObject.AddComponent<TextMeshPro>();

            numberAmmoDisplay.lineSpacing = -30f;

            numberAmmoDisplay.alignment = TextAlignmentOptions.Center;
            numberAmmoDisplay.fontSize = 92f;
            numberAmmoDisplay.enableWordWrapping = false;
            numberAmmoDisplay.fontStyle = FontStyles.Bold;
            numberAmmoDisplay.richText = true;
            numberAmmoDisplay.color = DividedBarShaderController.normalColor;
            BulletsInMag = Utils.FindDeepChild(transform, "Ammo").gameObject.AddComponent<DividedBarShaderController>();
        }

        private static void SetInitialPlayerStatusValues()
        {
            Health.SetColor(normalHealthCol);
            Infection.SetColor(normalInfectionCol);
            Oxygen.SetColor(normalOxygenCol);

            Health.maxValue = 100;
            Health.currentValue = 100;

            Oxygen.maxValue = 100;
            Oxygen.currentValue = 100;

            Infection.maxValue = 100;
            Infection.currentValue = 0;

            Health.UpdateShaderVals(5, 2);
            Infection.UpdateShaderVals(5, 2);
            Oxygen.UpdateShaderVals(5, 2);

            UpdateAir(1f);
        }
        public void SwitchState()
        {
            int maxStateIndex = Enum.GetValues(typeof(WatchState)).Length - 1;
            int nextIndex = (int)currentState + 1;

            if (nextIndex > maxStateIndex)
            {
                nextIndex = 0;
            }
            SwitchState((WatchState)nextIndex);
            SteamVR_InputHandler.TriggerHapticPulse(0.025f, 1 / .025f, 0.3f, Controllers.GetDeviceFromHandType(Controllers.offHandControllerType));
        }

        void SwitchState(WatchState state)
        {
            switch (state)
            {
                case (WatchState.Inventory):
                    ToggleInventoryRendering(true);
                    ToggleObjectiveRendering(false);

                    break;
                case (WatchState.Objective):
                    ToggleInventoryRendering(false);
                    ToggleObjectiveRendering(true);
                    break;
            }
            currentState = state;
        }
        void ToggleInventoryRendering(bool toggle)
        {
            foreach (MeshRenderer m in inventoryMeshes)
            {
                m.enabled = toggle;
            }

            if (VR_Settings.useNumbersForAmmoDisplay)
            {
                numberAmmoDisplay.gameObject.SetActive(toggle);
                BulletsInMag.gameObject.SetActive(false);
                
            } else
            {
                numberAmmoDisplay.gameObject.SetActive(false);
                BulletsInMag.gameObject.SetActive(toggle);
            }
            numberAmmoDisplay.ForceMeshUpdate();
            //Force update to possibly disable those bars depending on oxygen level/infection level
            UpdateAir(Oxygen.currentValue);
            UpdateInfection(Infection.currentValue);
        }

        void ToggleObjectiveRendering(bool toggle)
        {
            objectiveDisplay.enabled = toggle;
            objectiveDisplay.ForceMeshUpdate();
        }

        void OnDestroy()
        {
            ItemEquippableEvents.OnPlayerWieldItem -= ItemSwitched;
            InventoryAmmoEvents.OnInventoryAmmoUpdate -= AmmoUpdate;
            objectiveDisplay = null;
            Health = null;
            Infection = null;
            Oxygen = null;
        }
    }
}
﻿using MelonLoader;
using RumbleModdingAPI;
using RumbleModUI;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Il2CppRUMBLE.Players.BootLoader;
using HarmonyLib;
using System;
using Il2CppRUMBLE.Players.Subsystems;
using Il2CppRUMBLE.Players;

namespace OneHandedRumble
{
    public static class BuildInfo
    {
        public const string ModName = "OneHandedRumble";
        public const string ModVersion = "1.0.0";
        public const string Description = "Accessibility option that allows playing with only one hand";
        public const string Author = "Kalamart";
        public const string Company = "";
    }

    public class MyMod : MelonMod
    {
        public static void Log(string msg)
        {
            MelonLogger.Msg(msg);
        }

        // variables
        Mod Mod = new Mod();

        private static bool needInit = false;
        private static bool initialized = false;
        private static bool modInitialized = false;
        private static bool oneHandedMode = false;
        private static bool tracklessMode = false;
        public bool[] status = { true, true };
        bool useMute = false;
        bool useTurn = false;

        // current left/right sides for each action
        int i_real = 0; // which side is the active controller (in one-handed mode)
        int i_virtual = 1; // which side is the inactive controller (in one-handed mode)
        int i_talk = 1; // which side to use for push-to-talk
        int i_walk = 0; // which side to use for turning
        int i_measure1 = 0; // which side is the first measure button on
        int i_measure2 = 1; // which side is the second measure button on

        private static GameObject realHand;
        private static GameObject virtualHand;
        private static Il2CppRUMBLE.Input.InputManager.Hand virtualHandId;
        private static Il2CppRUMBLE.Input.InputManager.Hand realHandId;

        //constants
        private readonly Il2CppRUMBLE.Input.InputManager.Hand LEFT_HAND = Il2CppRUMBLE.Input.InputManager.Hand.Left;
        private readonly Il2CppRUMBLE.Input.InputManager.Hand RIGHT_HAND = Il2CppRUMBLE.Input.InputManager.Hand.Right;
        private readonly string[] side = { "left", "right" };

        /**
         * <summary>
         * Called when the mod is loaded into the game
         * </summary>
         */
        public override void OnLateInitializeMelon()
        {
            UI.instance.UI_Initialized += OnUIInit;
        }

        /**
         * <summary>
         * Specify the different options that will be used in the ModUI settings
         * </summary>
         */
        public void SetUIOptions()
        {
            Mod.ModName = BuildInfo.ModName;
            Mod.ModVersion = BuildInfo.ModVersion;
            Mod.SetFolder("OneHandedRumble");
            Mod.AddToList("Left enabled", true, 0, "Enable tracking for the left controller.", new Tags { });
            Mod.AddToList("Right enabled", true, 0, "Enable tracking for the right controller.", new Tags { });
            Mod.AddToList("Use Mute on enabled controller", false, 0, "The default action for the primary button is push-to-talk, enable this to switch it with mute. " +
                "The other action is still available on the disabled controller." +
                "\n\nThis option does nothing when both controllers are enabled.", new Tags { });
            Mod.AddToList("Use Turn on enabled controller", false, 0, "The default action for the joystick is walk, enable this to switch it with turn." +
                "The other action is still available on the disabled controller." +
                "\n\nThis option does nothing when both controllers are enabled.", new Tags { });
            Mod.GetFromFile();
        }

        /**
         * <summary>
         * Called when the actual ModUI window is initialized
         * </summary>
         */
        public void OnUIInit()
        {
            Mod.ModSaved += OnUISaved;
            UI.instance.AddMod(Mod);
        }

        /**
         * <summary>
         * Called when the user saves a configuration in ModUI
         * </summary>
         */
        public void OnUISaved()
        {
            UpdateMode();
        }

        /**
         * <summary>
         * Called when the scene has finished loading
         * </summary>
         */
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            Log($"Detected scene '{sceneName}'");
            if (sceneName == "Loader")
            {
                SetUIOptions();
                InitScene(true);
            }
        }

        /**
         * <summary>
         * Initializes the objects and managers that the mod is using.
         * </summary>
         */
        public void InitScene(bool isLoader)
        {
            Utils.Initialize(isLoader);
            UpdateMode();

            if (isLoader)
            {
                modInitialized = true;
            }
            initialized = true;
            Log($"Initialized controls");
        }

        /**
         * <summary>
         * Toggles both controllers as well as their bindings, and logs the notable changes (if any)
         * </summary>
         */
        private void UpdateMode()
        {
            bool[] newStatus = { true, true };
            int matchmakingType = Calls.Matchmaking.getMatchmakingTypeAsInt();
            bool isRandomQueue = (matchmakingType >= 0) && (matchmakingType != 5);
            if (!isRandomQueue)
            {
                // outside of random queue, get the ModUI config
                newStatus[0] = (bool)Mod.Settings[0].SavedValue;
                newStatus[1] = (bool)Mod.Settings[1].SavedValue;
            }
            useMute = (bool)Mod.Settings[2].SavedValue;
            useTurn = (bool)Mod.Settings[3].SavedValue;

            bool needLogging = (!modInitialized) || // if we're at the first mod init
                                (newStatus[0] != status[0] || newStatus[1] != status[1]); // or the status of the hands changed
            SetControllerStatus(true, newStatus[0]);
            SetControllerStatus(false, newStatus[1]);

            // If a mode change notification message is needed in the logs
            if (needLogging)
            {
                if (isRandomQueue)
                {
                    Log($"Mod disabled because a random queue match has been started");
                }
                string logMsg = "";
                if (oneHandedMode)
                {
                    logMsg = $"Entering {side[i_real]}-handed mode";
                }
                else
                {
                    logMsg = status[0] ? "Entering normal two-handed mode" :
                                    "Disabling tracking for both hands (why?)";
                }
                Log(logMsg);
            }

            SetMeasureBindings();
            SetJoystickBindings();
            SetPushToTalkBindings();
        }


        /**
         * <summary>
         * Toggles the left or right controller mode. When one controller is disabled, the game enters one-handed mode.
         * Two enbaled controllers are simply the normal gameplay, and no controllers is also possible but why...
         * </summary>
         */
        private void SetControllerStatus(bool isLeft, bool enabled)
        {
            int index = isLeft ? 0 : 1;
            bool statusChanged = (enabled != status[index]);
            status[index] = enabled;

            oneHandedMode = (status[0] != status[1]); // is one hand mirroring the other
            tracklessMode = (!oneHandedMode && !enabled);

            if (oneHandedMode)
            {
                realHand = status[0] ? Utils.leftHand : Utils.rightHand;
                virtualHand = status[0] ? Utils.rightHand : Utils.leftHand;
                realHandId = status[0] ? LEFT_HAND : RIGHT_HAND;
                virtualHandId = status[0] ? RIGHT_HAND : LEFT_HAND;
                i_real = status[0] ? 0 : 1;
                i_virtual = 1 - i_real;
            }
        }

        /**
         * <summary>
         * Called every couple frames, used for frequent updates
         * </summary>
         */
        public override void OnFixedUpdate()
        {
            if (needInit) // if the local player was initialized
            {
                InitScene(false);
                needInit = false;
            }

            try
            {
                if (initialized)
                {
                    if (oneHandedMode)
                    {
                        if ((Calls.ControllerMap.LeftController.GetTrigger() > 0.3) ||
                            (Calls.ControllerMap.RightController.GetTrigger() > 0.3))
                        {
                            // if trigger is pulled, freeze the virtual hand in place (compared to the chest)
                            Utils.FreezeObject(virtualHand);
                        }
                        else
                        {
                            // if not, mirror the real hand's position
                            Utils.MirrorObject(realHand, virtualHand);
                        }
                    }

                    // ensure that the pose drivers are properly enabled/disabled
                    Utils.leftHand.GetComponent<TrackedPoseDriver>().enabled = status[0];
                    Utils.rightHand.GetComponent<TrackedPoseDriver>().enabled = status[1];
                }
            }
            catch(System.Exception)
            {
                initialized = false;
            }
        }

        /**
         * <summary>
         * Updates the bindings on both t-pose measure buttons
         * </summary>
         */
        public void SetMeasureBindings()
        {
            int i_measure1_old = i_measure1;
            int i_measure2_old = i_measure2;
            i_measure1 = oneHandedMode ? i_real : 0;
            i_measure2 = oneHandedMode ? i_real : 1;

            if (!modInitialized || i_measure1 != i_measure1_old || i_measure2 != i_measure2_old)
            {
                Utils.replaceBindings(Utils.leftMeasureAction, Utils.measureButtons[i_measure1]);
                Utils.replaceBindings(Utils.rightMeasureAction, Utils.measureButtons[i_measure2]);

                if (oneHandedMode)
                {
                    Log($"T-pose measure with the {side[i_real]} controller");
                }
                else
                {
                    Log($"T-pose measure with both controllers");
                }
            }
        }

        /**
         * <summary>
         * Updates the bindings on both joysticks
         * </summary>
         */
        public void SetJoystickBindings()
        {
            int i_walk_old = i_walk;
            i_walk = oneHandedMode ? (useTurn ? i_virtual : i_real) : 0;

            if (!modInitialized || i_walk != i_walk_old)
            {
                Utils.replaceBindings(Utils.walkAction, Utils.joysticks[i_walk]);
                Utils.replaceBindings(Utils.turnAction, Utils.joysticks[1 - i_walk]);

                Log($"Walk with the {side[i_walk]} controller");
                Log($"Turn with the {side[1 - i_walk]} controller");
            }
        }

        /**
         * <summary>
         * Updates the bindings on both push-to-talk buttons
         * </summary>
         */
        public void SetPushToTalkBindings()
        {
            int i_talk_old = i_talk;
            i_talk = oneHandedMode ? (useMute ? i_virtual : i_real) : 1;

            if (!modInitialized || i_talk != i_talk_old)
            {
                Utils.replaceBindings(Utils.talkAction, Utils.pushToTalkButtons[i_talk]);
                Utils.replaceBindings(Utils.muteAction, Utils.pushToTalkButtons[1 - i_talk]);

                Log($"Push To Talk with the {side[i_talk]} controller");
                Log($"Mute with the {side[1 - i_talk]} controller");
            }
        }

        /**
         * <summary>
         * Harmony patch that copies the hand presence for the player controller
         * </summary>
         */
        [HarmonyPatch(typeof(PlayerHandPresence), "GetHandPresenceInputForHand", new Type[] { typeof(Il2CppRUMBLE.Input.InputManager.Hand) })]
        public static class PlayerHandPresenceCopy
        {
            private static bool Prefix(Il2CppRUMBLE.Input.InputManager.Hand hand, ref PlayerHandPresence __instance, ref PlayerHandPresence.HandPresenceInput __result)
            {
                if (tracklessMode) // disable hand presence if tracking is disabled completely
                {
                    return false;
                }
                if (oneHandedMode && virtualHandId == hand)
                {
                    // replace the inputs for the virtual hand by the ones from the real hand
                    __result = __instance.GetHandPresenceInputForHand(realHandId);
                    return false; // prevent the original method from running
                }
                return true;
            }
        }

        /**
         * <summary>
         * Harmony patch that copies the hand presence on the loader screen
         * </summary>
         */
        [HarmonyPatch(typeof(BootloaderBridgeSystem), "GetHandPresenceInputForHand", new Type[] { typeof(Il2CppRUMBLE.Input.InputManager.Hand) })]
        public static class LoaderHandPresenceCopy
        {
            private static bool Prefix(Il2CppRUMBLE.Input.InputManager.Hand hand, ref BootloaderBridgeSystem __instance, ref PlayerHandPresence.HandPresenceInput __result)
            {
                if (tracklessMode) // disable hand presence if tracking is disabled completely
                {
                    return false;
                }
                if (oneHandedMode && virtualHandId == hand)
                {
                    // replace the inputs for the virtual hand by the ones from the real hand
                    __result = __instance.GetHandPresenceInputForHand(realHandId); 
                    return false; // prevent the original method from running
                }
                return true;
            }
        }

        /**
         * <summary>
         * Harmony patch that is called when the local player is initialized
         * </summary>
         */
        [HarmonyPatch(typeof(PlayerController), "Initialize", new System.Type[] { typeof(Player) })]
        public static class playerspawn
        {
            private static void Postfix(ref PlayerController __instance, ref Player player)
            {
                if (Calls.Players.GetLocalPlayer() == player)
                {
                    needInit = true;
                }
            }
        }

    }
}

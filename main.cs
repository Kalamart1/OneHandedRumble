using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Il2CppRUMBLE.Players.BootLoader;
using HarmonyLib;
using System;
using Il2CppRUMBLE.Players.Subsystems;

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
        private static bool initialized = false;
        private static bool modInitialized = false;
        private static bool oneHandedMode = false;
        private static bool tracklessMode = false;
        public bool[] status = { true, true };

        private static GameObject realHand;
        private static GameObject virtualHand;
        private static Il2CppRUMBLE.Input.InputManager.Hand virtualHandId;
        private static Il2CppRUMBLE.Input.InputManager.Hand realHandId;

        //constants
        Il2CppRUMBLE.Input.InputManager.Hand LEFT_HAND = Il2CppRUMBLE.Input.InputManager.Hand.Left;
        Il2CppRUMBLE.Input.InputManager.Hand RIGHT_HAND = Il2CppRUMBLE.Input.InputManager.Hand.Right;

        // TODO use ModUI settings for the initialization instead
        private bool[] initStatus = { false, true }; // left hand disabled by default

        /**
         * <summary>
         * Called when the mod is loaded into the game
         * </summary>
         */
        public override void OnLateInitializeMelon()
        {
        }

        /**
         * <summary>
         * Called when the scene has finished loading
         * </summary>
         */
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            initialized = false;
            Log($"Detected scene '{sceneName}'");
            if (sceneName == "Loader")
            {
                InitScene(true);
            }
        }

        /**
         * <summary>
         * Called when the map is completely loaded, and RMAPI is completely initialized
         * </summary>
         */
        public void OnMapInitialized()
        {
            InitScene(false);
        }

        /**
         * <summary>
         * Initializes the objects and managers that the mod is using.
         * </summary>
         */
        public void InitScene(bool isLoader)
        {
            Utils.Initialize(isLoader);

            SetControllerStatus(true, initStatus[0]);
            SetControllerStatus(false, initStatus[1]);

            if (isLoader)
            {
                // RMAPI is not initialized yet when OnSceneWasLoaded is called, so we add a callback
                Calls.onMapInitialized += OnMapInitialized;

                modInitialized = true;
            }
            initialized = true;
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
            bool needLogging = (!modInitialized && !isLeft) || // if we're at the first mod init
                                (initialized && statusChanged); // or the status of the hand changed

            if (oneHandedMode)
            {
                realHand = status[0] ? Utils.leftHand : Utils.rightHand;
                virtualHand = status[0] ? Utils.rightHand : Utils.leftHand;
                realHandId = status[0] ? LEFT_HAND : RIGHT_HAND;
                virtualHandId = status[0] ? RIGHT_HAND : LEFT_HAND;
                // TODO change input bindings too, so that the game is 100% playable with one controller
            }

            // If a mode change notification message is needed in the logs
            if (needLogging)
            {
                string logMsg = "";
                if (oneHandedMode)
                {
                    string side = status[0] ? "left" : "right";
                    logMsg = $"Entering {side}-handed mode";
                }
                else if (needLogging)
                {
                    logMsg = enabled ? "Entering normal two-handed mode" :
                                    "Disabling tracking for both hands";
                }
                Log(logMsg);
            }
        }

        /**
         * <summary>
         * Called every couple frames, used for frequent updates
         * </summary>
         */
        public override void OnFixedUpdate()
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

    }
}

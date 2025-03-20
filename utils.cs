using RumbleModdingAPI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using MelonLoader;
using System.Collections.Generic;
using Il2CppRUMBLE.Players.Scaling;

namespace OneHandedRumble
{
    public class Utils
    {
        public static void Log(string msg)
        {
            //MyMod.Log(msg);
            MelonLogger.Msg(msg);
        }

        //variables
        private static bool initialized = false;
        private static Transform headset = null;
        private static Transform chest = null;
        private static Transform root = null;
        public static GameObject leftHand = null;
        public static GameObject rightHand = null;

        public static InputAction leftMeasureAction = null;
        public static InputAction rightMeasureAction = null;
        public static InputAction muteAction = null;
        public static InputAction talkAction = null;
        public static InputAction walkAction = null;
        public static InputAction turnAction = null;

        public static List<ReadOnlyArray<InputBinding>> measureButtons = null;
        public static List<ReadOnlyArray<InputBinding>> pushToTalkButtons = null;
        public static List<ReadOnlyArray<InputBinding>> joysticks = null;

        public static System.Exception uninitException = new System.Exception("You must first call Utils.Initialize before calling the methods");

        /**
         * <summary>
         * Updates the current position of the "chest" (ie the headset but without pitch and roll)
         * </summary>
         */
        private static void UpdateChestTransform()
        {
            Vector3 position = headset.localPosition;
            float yaw = headset.localRotation.eulerAngles.y;
            // Remove every rotation except for yaw
            Vector3 rotationEuler = new Vector3(0, yaw, 0);
            chest.SetLocalPositionAndRotation(position, Quaternion.Euler(rotationEuler));
        }

        /**
         * <summary>
         * Initializes the internal variables that are used by all of the other methods
         * </summary>
         */
        public static void Initialize(bool isLoader)
        {
            root = new GameObject("Root").transform;
            if (isLoader)
            {
                // in the loader scene, there is no player controller yet
                Transform bootLoaderPlayerTr = GameObject.Find("BootLoaderPlayer").transform;
                headset = bootLoaderPlayerTr.GetChild(1).GetChild(0);
                leftHand = bootLoaderPlayerTr.GetChild(2).gameObject;
                rightHand = bootLoaderPlayerTr.GetChild(3).gameObject;
                root.SetParent(bootLoaderPlayerTr);
                root.SetLocalPositionAndRotation(new Vector3(0, 0.1f, 0), Quaternion.identity);
            }
            else
            {
                // this works for all post-loader scenes (gym, park, maps)
                Transform playerTr = Calls.Players.GetPlayerController().gameObject.transform.GetChild(1);
                headset = playerTr.GetChild(0).GetChild(0);
                leftHand = playerTr.GetChild(1).gameObject;
                rightHand = playerTr.GetChild(2).gameObject;
                root.SetParent(playerTr.GetChild(3));
                root.SetLocalPositionAndRotation(new Vector3(0, -0.045f, 0), Quaternion.identity);
            }
            chest = new GameObject("Chest").transform;
            chest.SetParent(headset.parent);

            if (!initialized)
            {
                Il2CppRUMBLE.Input.InputManager inputManager = Il2CppRUMBLE.Input.InputManager.instance;

                leftMeasureAction = GetAction(inputManager, true, "Measure");
                rightMeasureAction = GetAction(inputManager, false, "Measure");
                muteAction = GetAction(inputManager, true, "Mute");
                talkAction = GetAction(inputManager, false, "PushToTalk");
                walkAction = GetAction(inputManager, true, "Walk");
                turnAction = GetAction(inputManager, false, "Turn");

                measureButtons = new List<ReadOnlyArray<InputBinding>> { Utils.leftMeasureAction.bindings.ToArray(), Utils.rightMeasureAction.bindings.ToArray() };
                pushToTalkButtons = new List<ReadOnlyArray<InputBinding>> { Utils.muteAction.bindings.ToArray(), Utils.talkAction.bindings.ToArray() };
                joysticks = new List<ReadOnlyArray<InputBinding>> { Utils.walkAction.bindings.ToArray(), Utils.turnAction.bindings.ToArray() };

                initialized = true;
            }

        }

        /**
         * <summary>
         * Updates the position and rotation of obj2 so that it mirrors obj1 (in relation to the chest)
         * </summary>
         */
        public static void MirrorObject(GameObject obj1, GameObject obj2)
        {
            if (!initialized)
            {
                throw uninitException;
            }
            UpdateChestTransform();
            obj2.transform.position = MirrorPosition(obj1.transform.position);
            obj2.transform.rotation = MirrorRotation(obj1.transform.rotation);
        }


        /**
         * <summary>
         * Updates the freezed position of the object (moves along with the chest)
         * </summary>
         */
        public static void FreezeObject(GameObject obj)
        {
            if (!initialized)
            {
                throw uninitException;
            }
            UpdateChestTransform();
            obj.transform.position = GetGlobalPosition();
            obj.transform.rotation = GetGlobalRotation();
        }

        // currently registered transform of the tracked object (in relation to the chest)
        static Vector3 localPosition;
        static Quaternion localRotation;

        /**
         * <summary>
         * Calculates the mirrored position in front of the chest (ie headset position with only the yaw)
         * </summary>
         */
        public static Vector3 MirrorPosition(Vector3 objPos)
        {
            if (!initialized)
            {
                throw uninitException;
            }
            localPosition = chest.InverseTransformPoint(objPos);
            // Mirror the position across the local XZ plane (invert X)
            localPosition.x = -localPosition.x;
            return GetGlobalPosition();
        }

        /**
         * <summary>
         * Calculates the mirrored rotation in front of the chest (ie headset position with only the yaw)
         * </summary>
         */
        public static Quaternion MirrorRotation(Quaternion objRot)
        {
            if (!initialized)
            {
                throw uninitException;
            }
            localRotation = Quaternion.Inverse(chest.rotation) * objRot;
            // Preserve the X (pitch), invert Y (yaw) and Z (roll)
            localRotation = new Quaternion(localRotation.x, -localRotation.y, -localRotation.z, localRotation.w);
            return GetGlobalRotation();
        }

        /**
         * <summary>
         * Get the global position from the last registered local one
         * </summary>
         */
        public static Vector3 GetGlobalPosition()
        {
            if (!initialized)
            {
                throw uninitException;
            }
            return chest.TransformPoint(localPosition);
        }

        /**
         * <summary>
         * Get the global rotation from the last registered local one
         * </summary>
         */
        public static Quaternion GetGlobalRotation()
        {
            if (!initialized)
            {
                throw uninitException;
            }
            return chest.rotation * localRotation;
        }

        public static InputAction GetAction(Il2CppRUMBLE.Input.InputManager inputManager, bool isLeft, string name)
        {
            Il2CppRUMBLE.Input.InputManager.Hand hand = isLeft ? Il2CppRUMBLE.Input.InputManager.Hand.Left : Il2CppRUMBLE.Input.InputManager.Hand.Right;
            InputActionMap actionMap = inputManager.GetActionMap(hand);
            foreach (var inputAction in actionMap.actions)
            {
                if (name == inputAction.name)
                {
                    return inputAction;
                }
            }
            return null;
        }

        public static void ReplaceBindings(InputAction action, ReadOnlyArray<InputBinding> newBindings)
        {
            int n = action.bindings.m_Length;
            for (int i = 0; i < n; i++)
            {
                action.ChangeBinding(0).Erase();
            }
            foreach (var binding in newBindings)
            {
                action.AddBinding(binding.path, interactions: binding.interactions, processors: binding.processors, groups: binding.groups);
            }
        }

        public static bool SetMeasurement()
        {
            UpdateChestTransform();
            Il2CppRUMBLE.Managers.PlayerManager playerManager = Il2CppRUMBLE.Managers.PlayerManager.instance;
            PlayerMeasurement measurement = PlayerMeasurement.Create(Utils.headset, Utils.leftHand.transform, Utils.rightHand.transform, Utils.root, true);
            if (playerManager.localPlayer!=null) // check that the player is initialized
            {
                playerManager.localPlayer.Data.SetMeasurement(measurement, true);
                return true;
            }
            return false;
        }
    }
}

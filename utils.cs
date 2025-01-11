using RumbleModdingAPI;
using UnityEngine;

namespace OneHandedRumble
{
    public class Utils
    {

        //variables
        private static bool initialized = false;
        private static Transform headset = null;
        private static Transform chest = null;
        public static GameObject leftHand = null;
        public static GameObject rightHand = null;

        public static System.Exception uninitException = new System.Exception("You must first call Utils.Initialize before calling the methods");

        /**
         * <summary>
         * Updates the current position of the "chest" (ie the headset but without pitch and roll)
         * </summary>
         */
        private static void UpdateChestTransform()
        {
            Vector3 position = headset.position;
            float yaw = headset.rotation.eulerAngles.y;
            // Remove every rotation except for yaw
            Vector3 rotationEuler = new Vector3(0, yaw, 0);
            chest.SetPositionAndRotation(position, Quaternion.Euler(rotationEuler));
        }

        /**
         * <summary>
         * Initializes the internal variables that are used by all of the other methods
         * </summary>
         */
        public static void Initialize(bool isLoader)
        {
            initialized = false;
            if (isLoader)
            {
                // in the loader scene, there is no player controller yet
                Transform bootLoaderPlayerTr = GameObject.Find("BootLoaderPlayer").transform;
                headset = bootLoaderPlayerTr.GetChild(1).GetChild(0);
                leftHand = bootLoaderPlayerTr.GetChild(2).gameObject;
                rightHand = bootLoaderPlayerTr.GetChild(3).gameObject;
            }
            else
            {
                // this works for all post-loader scenes (gym, park, maps)
                Transform playerTr = Calls.Players.GetPlayerController().gameObject.transform.GetChild(1);
                headset = playerTr.GetChild(0).GetChild(0);
                leftHand = playerTr.GetChild(1).gameObject;
                rightHand = playerTr.GetChild(2).gameObject;
            }
            chest = new GameObject("ModifiedHeadset").transform;
            chest.SetParent(headset.parent);
            initialized = true;
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
    }
}

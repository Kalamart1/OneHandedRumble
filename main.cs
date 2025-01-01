using MelonLoader;
using RumbleModdingAPI;

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
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
        }

        public override void OnFixedUpdate()
        {
        }
    }
}

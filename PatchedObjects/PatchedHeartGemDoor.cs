using Microsoft.Xna.Framework;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedHeartGemDoor : IPatchable
    {

        private static IDetour hook_HeartGemDoor_get_HeartGems;

        public void Load()
        {
            On.Celeste.HeartGemDoor.ctor += ctor;
            hook_HeartGemDoor_get_HeartGems = new Hook(
                typeof(HeartGemDoor).GetProperty("HeartGems").GetGetMethod(),
                typeof(PatchedHeartGemDoor).GetMethod("Get_HeartGems", BindingFlags.NonPublic | BindingFlags.Static)
            );
        }

        public void Unload()
        {
            On.Celeste.HeartGemDoor.ctor -= ctor;
            hook_HeartGemDoor_get_HeartGems.Dispose();
        }

        private static void ctor(On.Celeste.HeartGemDoor.orig_ctor orig, HeartGemDoor self, EntityData data, Vector2 offset)
        {
            if(AreaData.Areas[10].Mode[0].MapData.Levels.Contains(data.Level))
            {
                data.Values["requires"] = 1;
            }
            orig(self, data, offset);
        }

        private delegate int orig_HeartGemDoor_get_HeartGems(HeartGemDoor self);
        private static int Get_HeartGems(orig_HeartGemDoor_get_HeartGems orig, HeartGemDoor self)
        {
            if(self.Requires != 1)
            {
                return orig(self);
            }

            return (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(new AreaKey(10, AreaMode.Normal)) ? 1 : 0);
        }

    }
}

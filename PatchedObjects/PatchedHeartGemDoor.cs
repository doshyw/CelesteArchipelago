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
            //if (ArchipelagoController.Instance.SlotData.DisableHeartGates == 1)
            //{
            //    (self.Scene as Level).Session.SetFlag("opened_heartgem_door_" + self.Requires);
            //}
            orig(self, data, offset);
        }

        private delegate int orig_HeartGemDoor_get_HeartGems(HeartGemDoor self);
        private static int Get_HeartGems(orig_HeartGemDoor_get_HeartGems orig, HeartGemDoor self)
        {
            if (SaveData.Instance.CheatMode || ArchipelagoController.Instance.SlotData.DisableHeartGates == 1)
            {
                return self.Requires;
            }

            return ArchipelagoController.Instance.ProgressionSystem.GetTotalLogically(CollectableType.HEARTGEM);
        }

    }
}

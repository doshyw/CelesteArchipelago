using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedStrawberry : IPatchable
    {
        private static IDetour hook_Strawberry_orig_OnCollect;
        private delegate void orig_Strawberry_orig_OnCollect(Strawberry self);
        public void Load()
        {
            hook_Strawberry_orig_OnCollect = new Hook(
                typeof(Strawberry).GetMethod("orig_OnCollect"),
                typeof(PatchedStrawberry).GetMethod("orig_OnCollect", BindingFlags.NonPublic | BindingFlags.Static)
            );
            On.Celeste.Strawberry.ctor += ctor;
        }

        public void Unload()
        {
            hook_Strawberry_orig_OnCollect.Dispose();
            On.Celeste.Strawberry.ctor -= ctor;
        }

        private static void ctor(On.Celeste.Strawberry.orig_ctor orig, Strawberry self, EntityData data, Microsoft.Xna.Framework.Vector2 offset, EntityID gid)
        {
            orig(self, data, offset, gid);
            DynamicData.For(self).Set("isGhostBerry", ArchipelagoController.Instance.ProgressionSystem.IsCollectedVisually(SaveData.Instance.CurrentSession_Safe.Area, CollectableType.STRAWBERRY, self.ID));
        }

        private static void orig_OnCollect(orig_Strawberry_orig_OnCollect orig, Strawberry self)
        {
            Logger.Log("CelesteArchipelago", "Entering Strawberry.OnCollect");

            DynamicData strawberry = DynamicData.For(self);
            var collected = strawberry.Get<bool>("collected");

            if (!collected)
            {
                int collectIndex = 0;
                strawberry.Set("collected", true);
                if (self.Follower.Leader != null)
                {
                    Player obj = self.Follower.Leader.Entity as Player;
                    collectIndex = obj.StrawberryCollectIndex;
                    obj.StrawberryCollectIndex++;
                    obj.StrawberryCollectResetTimer = 2.5f;
                    self.Follower.Leader.LoseFollower(self.Follower);
                }
                if (self.Moon)
                {
                    Achievements.Register(Achievement.WOW);
                }
                // SaveData.Instance.AddStrawberry(self.ID, self.Golden);
                ArchipelagoController.Instance.ProgressionSystem.OnCollectedClient(SaveData.Instance.CurrentSession_Safe.Area, CollectableType.STRAWBERRY, self.ID); // NEW
                Session session = (self.Scene as Level).Session;
                session.DoNotLoad.Add(self.ID);
                session.Strawberries.Add(self.ID);
                session.UpdateLevelStartDashes();
                self.Add(new Coroutine(strawberry.Invoke<IEnumerator>("CollectRoutine", collectIndex)));
            }
        }
    }
}

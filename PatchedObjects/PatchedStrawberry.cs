using FMOD;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public static class PatchedStrawberry
    {
        private static IDetour hook_Strawberry_orig_OnCollect;
        private delegate void orig_Strawberry_orig_OnCollect(Strawberry self);
        internal static void Load()
        {
            On.Celeste.Strawberry.CollectRoutine += CollectRoutine;
            hook_Strawberry_orig_OnCollect = new Hook(
                typeof(Strawberry).GetMethod("orig_OnCollect"),
                typeof(PatchedStrawberry).GetMethod("orig_OnCollect", BindingFlags.NonPublic | BindingFlags.Static)
            );
            On.Celeste.Strawberry.ctor += ctor;
        }

        private static void ctor(On.Celeste.Strawberry.orig_ctor orig, Strawberry self, EntityData data, Microsoft.Xna.Framework.Vector2 offset, EntityID gid)
        {
            orig(self, data, offset, gid);
            DynamicData.For(self).Set("isGhostBerry", CelesteArchipelagoSaveData.GetStrawberryOutGame(SaveData.Instance.CurrentSession_Safe.Area.ID, self.ID));
        }

        internal static void Unload()
        {
            On.Celeste.Strawberry.CollectRoutine -= CollectRoutine;
            hook_Strawberry_orig_OnCollect.Dispose();
            On.Celeste.Strawberry.ctor -= ctor;
        }

        public static IEnumerator CollectRoutine(On.Celeste.Strawberry.orig_CollectRoutine orig, Strawberry self, int collectIndex)
        {
            Logger.Log("CelesteArchipelago", "Entering Strawberry.CollectRoutine");

            DynamicData strawberry = DynamicData.For(self);
            var isGhostBerry = strawberry.Get<bool>("isGhostBerry");
            var sprite = strawberry.Get<Sprite>("sprite");

            _ = self.Scene;
            self.Tag = Tags.TransitionUpdate;
            self.Depth = -2000010;
            int num = 0;
            if (self.Moon)
            {
                Logger.Log("CelesteArchipelago", "Got Moon Berry");
                num = 3;
            }
            else if (isGhostBerry)
            {
                Logger.Log("CelesteArchipelago", "Got Ghost Berry");
                num = 1;
            }
            else if (self.Golden)
            {
                Logger.Log("CelesteArchipelago", "Got Golden Berry");
                num = 2;
            }
            Audio.Play("event:/game/general/strawberry_get", self.Position, "colour", num, "count", collectIndex);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            sprite.Play("collect");
            while (sprite.Animating)
            {
                yield return null;
            }
            // self.Scene.Add(new StrawberryPoints(self.Position, isGhostBerry, collectIndex, self.Moon));
            self.RemoveSelf();
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
                    // collectIndex = obj.StrawberryCollectIndex;
                    // obj.StrawberryCollectIndex++;
                    obj.StrawberryCollectResetTimer = 2.5f;
                    self.Follower.Leader.LoseFollower(self.Follower);
                }
                if (self.Moon)
                {
                    Achievements.Register(Achievement.WOW);
                }
                // SaveData.Instance.AddStrawberry(self.ID, self.Golden);
                CelesteArchipelagoSaveData.SetStrawberryOutGame(SaveData.Instance.CurrentSession_Safe.Area.ID, self.ID); // NEW
                Session session = (self.Scene as Level).Session;
                session.DoNotLoad.Add(self.ID);
                // session.Strawberries.Add(self.ID);
                session.UpdateLevelStartDashes();
                self.Add(new Coroutine(strawberry.Invoke<IEnumerator>("CollectRoutine", collectIndex)));
            }
        }
    }
}

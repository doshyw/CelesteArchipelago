using FMOD;
using Microsoft.Xna.Framework;
using Monocle;
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
    public static class PatchedCassette
    {
        internal static void Load()
        {
            On.Celeste.Cassette.Added += Added;
            On.Celeste.Cassette.CollectRoutine += CollectRoutine;
        }

        internal static void Unload()
        {
            On.Celeste.Cassette.Added -= Added;
            On.Celeste.Cassette.CollectRoutine -= CollectRoutine;
        }

        private static void Added(On.Celeste.Cassette.orig_Added orig, Cassette self, Scene scene)
        {
            Logger.Log("CelesteArchipelago", "Entering Cassette.Added");

            DynamicData cassette = DynamicData.For(self);

            var ptr = typeof(Entity).GetMethod("Added").MethodHandle.GetFunctionPointer();
            var entityAdded = (Action<Scene>)Activator.CreateInstance(typeof(Action<Scene>), self, ptr);
            entityAdded(scene);

            // self.IsGhost = SaveData.Instance.Areas_Safe[self.SceneAs<Level>().Session.Area.ID].Cassette;
            self.IsGhost = CelesteArchipelagoSaveData.GetCassetteOutGame(self.SceneAs<Level>().Session.Area.ID);

            cassette.Set("sprite", GFX.SpriteBank.Create(self.IsGhost ? "cassetteGhost" : "cassette"));
            var sprite = cassette.Get<Sprite>("sprite");
            self.Add(sprite);
            sprite.Play("idle");

            cassette.Set("scaleWiggler", Wiggler.Create(0.25f, 4f, delegate (float f)
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }));
            var scaleWiggler = cassette.Get<Wiggler>("scaleWiggler");
            self.Add(scaleWiggler);

            cassette.Set("bloom", new BloomPoint(0.25f, 16f));
            var bloom = cassette.Get<BloomPoint>("bloom");
            self.Add(bloom);

            cassette.Set("light", new VertexLight(Color.White, 0.4f, 32, 64));
            var light = cassette.Get<VertexLight>("light");
            self.Add(light);

            cassette.Set("hover", new SineWave(0.5f, 0f));
            var hover = cassette.Get<SineWave>("hover");
            self.Add(hover);

            hover.OnUpdate = delegate (float f)
            {
                Sprite obj = sprite;
                VertexLight vertexLight = light;
                float num2 = (bloom.Y = f * 2f);
                float y = (vertexLight.Y = num2);
                obj.Y = y;
            };
            if (self.IsGhost)
            {
                sprite.Color = Color.White * 0.8f;
            }
        }

        private static IEnumerator CollectRoutine(On.Celeste.Cassette.orig_CollectRoutine orig, Cassette self, Player player)
        {
            Logger.Log("CelesteArchipelago", "Entering Cassette.CollectRoutine");

            DynamicData cassette = DynamicData.For(self);

            cassette.Set("collecting", true);
            Level level = self.Scene as Level;
            CassetteBlockManager cbm = self.Scene.Tracker.GetEntity<CassetteBlockManager>();
            level.PauseLock = true;
            level.Frozen = true;
            self.Tag = Tags.FrozenUpdate;
            level.Session.Cassette = true;
            var nodes = cassette.Get<Vector2[]>("nodes");
            level.Session.RespawnPoint = level.GetSpawnPoint(nodes[1]);
            level.Session.UpdateLevelStartDashes();
            // SaveData.Instance.RegisterCassette(level.Session.Area);
            CelesteArchipelagoSaveData.SetCassetteOutGame(level.Session.Area.ID); // NEW
            cbm?.StopBlocks();
            self.Depth = -1000000;
            level.Shake();
            level.Flash(Color.White);
            level.Displacement.Clear();
            Vector2 camWas = level.Camera.Position;
            Vector2 camTo = Vector2.Clamp(self.Position - new Vector2(160f, 90f), new Vector2(level.Bounds.Left - 64, level.Bounds.Top - 32), new Vector2(level.Bounds.Right + 64 - 320, level.Bounds.Bottom + 32 - 180));
            level.Camera.Position = camTo;
            level.ZoomSnap(Vector2.Clamp(self.Position - level.Camera.Position, new Vector2(60f, 60f), new Vector2(260f, 120f)), 2f);
            var sprite = cassette.Get<Sprite>("sprite");
            sprite.Play("spin", restart: true);
            sprite.Rate = 2f;
            for (float p3 = 0f; p3 < 1.5f; p3 += Engine.DeltaTime)
            {
                sprite.Rate += Engine.DeltaTime * 4f;
                yield return null;
            }
            sprite.Rate = 0f;
            sprite.SetAnimationFrame(0);
            var scaleWiggler = cassette.Get<Wiggler>("scaleWiggler");
            scaleWiggler.Start();
            yield return 0.25f;
            Vector2 from = self.Position;
            Vector2 to = new Vector2(self.X, level.Camera.Top - 16f);
            float duration2 = 0.4f;
            for (float p3 = 0f; p3 < 1f; p3 += Engine.DeltaTime / duration2)
            {
                sprite.Scale.X = MathHelper.Lerp(1f, 0.1f, p3);
                sprite.Scale.Y = MathHelper.Lerp(1f, 3f, p3);
                self.Position = Vector2.Lerp(from, to, Ease.CubeIn(p3));
                yield return null;
            }
            self.Visible = false;
            cassette.Set("remixSfx", Audio.Play("event:/game/general/cassette_preview", "remix", level.Session.Area.ID));
            
            //UnlockedBSide message = new UnlockedBSide();
            //self.Scene.Add(message);
            //yield return message.EaseIn();
            //while (!Input.MenuConfirm.Pressed)
            //{
            //    yield return null;
            //}
            //var remixSfx = cassette.Get<FMOD.Studio.EventInstance>("remixSfx");
            //Audio.SetParameter(remixSfx, "end", 1f);
            //yield return message.EaseOut();
            //duration2 = 0.25f;
            //self.Add(new Coroutine(level.ZoomBack(duration2 - 0.05f)));
            //for (float p3 = 0f; p3 < 1f; p3 += Engine.DeltaTime / duration2)
            //{
            //    level.Camera.Position = Vector2.Lerp(camTo, camWas, Ease.SineInOut(p3));
            //    yield return null;
            //}
            if (!player.Dead && nodes != null && nodes.Length >= 2)
            {
                Audio.Play("event:/game/general/cassette_bubblereturn", level.Camera.Position + new Vector2(160f, 90f));
                player.StartCassetteFly(nodes[1], nodes[0]);
            }
            foreach (SandwichLava item in level.Entities.FindAll<SandwichLava>())
            {
                item.Leave();
            }
            level.Frozen = false;
            yield return 0.25f;
            cbm?.Finish();
            level.PauseLock = false;
            level.ResetZoom();
            self.RemoveSelf();
        }

    }
}

using Celeste.Mod.CelesteArchipelago.Networking;
using Celeste.Mod.CelesteArchipelago.PatchedObjects;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago.PatchedObjects
{
    public class PatchedHeartGem : IPatchable
    {
        public void Load()
        {
            On.Celeste.HeartGem.Awake += Awake;
            On.Celeste.HeartGem.RegisterAsCollected += RegisterAsCollected;
        }

        public void Unload()
        {
            On.Celeste.HeartGem.Awake -= Awake;
            On.Celeste.HeartGem.RegisterAsCollected -= RegisterAsCollected;
        }

        private static void Awake(On.Celeste.HeartGem.orig_Awake orig, HeartGem self, Scene scene)
        {
            Logger.Log("CelesteArchipelago", "Entering HeartGem.Awake");

            DynamicData heartGem = DynamicData.For(self);

            var ptr = typeof(Entity).GetMethod("Awake").MethodHandle.GetFunctionPointer();
            var entityAwake = (Action<Scene>)Activator.CreateInstance(typeof(Action<Scene>), self, ptr);
            entityAwake(scene);

            AreaKey area = (self.Scene as Level).Session.Area;
            // self.IsGhost = !self.IsFake && SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].HeartGem;
            self.IsGhost = !self.IsFake && ArchipelagoController.Instance.ProgressionSystem.IsCollectedVisually(area, CollectableType.HEARTGEM); // NEW
            string id = (self.IsFake ? "heartgem3" : ((!self.IsGhost) ? ("heartgem" + (int)area.Mode) : "heartGemGhost"));

            heartGem.Set("sprite", GFX.SpriteBank.Create(id));
            var sprite = heartGem.Get<Sprite>("sprite");
            self.Add(sprite);
            sprite.Play("spin");
            sprite.OnLoop = delegate (string anim)
            {
                if (self.Visible && anim == "spin" && heartGem.Get<bool>("autoPulse"))
                {
                    if (self.IsFake)
                    {
                        Audio.Play("event:/new_content/game/10_farewell/fakeheart_pulse", self.Position);
                    }
                    else
                    {
                        Audio.Play("event:/game/general/crystalheart_pulse", self.Position);
                    }
                    self.ScaleWiggler.Start();
                    (self.Scene as Level).Displacement.AddBurst(self.Position, 0.35f, 8f, 48f, 0.25f);
                }
            };
            if (self.IsGhost)
            {
                sprite.Color = Color.White * 0.8f;
            }
            self.Collider = new Hitbox(16f, 16f, -8f, -8f);
            self.Add(new PlayerCollider(self.OnPlayer));
            self.Add(self.ScaleWiggler = Wiggler.Create(0.5f, 4f, delegate (float f)
            {
                sprite.Scale = Vector2.One * (1f + f * 0.25f);
            }));

            heartGem.Set("bloom", new BloomPoint(0.75f, 16f));
            var bloom = heartGem.Get<BloomPoint>("bloom");
            self.Add(bloom);

            Color value;
            if (self.IsFake)
            {
                value = Calc.HexToColor("dad8cc");
                heartGem.Set("shineParticle", HeartGem.P_FakeShine);
            }
            else if (area.Mode == AreaMode.Normal)
            {
                value = Color.Aqua;
                heartGem.Set("shineParticle", HeartGem.P_BlueShine);
            }
            else if (area.Mode == AreaMode.BSide)
            {
                value = Color.Red;
                heartGem.Set("shineParticle", HeartGem.P_RedShine);
            }
            else
            {
                value = Color.Gold;
                heartGem.Set("shineParticle", HeartGem.P_GoldShine);
            }
            value = Color.Lerp(value, Color.White, 0.5f);
            heartGem.Set("light", new VertexLight(value, 1f, 32, 64));
            var light = heartGem.Get<VertexLight>("light");
            self.Add(light);
            if (self.IsFake)
            {
                bloom.Alpha = 0f;
                light.Alpha = 0f;
            }
            heartGem.Set("moveWiggler", Wiggler.Create(0.8f, 2f));
            var moveWiggler = heartGem.Get<Wiggler>("moveWiggler");
            moveWiggler.StartZero = true;
            self.Add(moveWiggler);
            if (!self.IsFake)
            {
                return;
            }
            Player entity = self.Scene.Tracker.GetEntity<Player>();
            if ((entity != null && entity.X > self.X) || (scene as Level).Session.GetFlag("fake_heart"))
            {
                self.Visible = false;
                Alarm.Set(self, 0.0001f, delegate
                {
                    heartGem.Invoke("FakeRemoveCameraTrigger");
                    self.RemoveSelf();
                });
            }
            else
            {
                heartGem.Set("fakeRightWall", new InvisibleBarrier(new Vector2(self.X + 160f, self.Y - 200f), 8f, 400f));
                var fakeRightWall = heartGem.Get<InvisibleBarrier>("fakeRightWall");
                scene.Add(fakeRightWall);
            }
        }

        private static void RegisterAsCollected(On.Celeste.HeartGem.orig_RegisterAsCollected orig, HeartGem self, Level level, string poemID)
        {
            Logger.Log("CelesteArchipelago", "Entering HeartGem.RegisterAsCollected");

            // level.Session.HeartGem = true;
            level.Session.UpdateLevelStartDashes();
            // int unlockedModes = SaveData.Instance.UnlockedModes;
            // SaveData.Instance.RegisterHeartGem(level.Session.Area);
            ArchipelagoController.Instance.ProgressionSystem.OnCollectedClient(level.Session.Area, CollectableType.HEARTGEM); // NEW
            if (!string.IsNullOrEmpty(poemID))
            {
                SaveData.Instance.RegisterPoemEntry(poemID);
            }
            //if (unlockedModes < 3 && SaveData.Instance.UnlockedModes >= 3)
            //{
            //    level.Session.UnlockedCSide = true;
            //}
            //if (SaveData.Instance.TotalHeartGemsInVanilla >= 24)
            //{
            //    Achievements.Register(Achievement.CSIDES);
            //}
        }

    }
}

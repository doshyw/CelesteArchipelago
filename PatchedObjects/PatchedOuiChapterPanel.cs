using Celeste.Mod.CelesteArchipelago.Networking;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago.PatchedObjects
{
    public class PatchedOuiChapterPanel : IPatchable
    {
        private static Color BgColor = Calc.HexToColor("3c6180");
        private static Color BgColor_Grey = Calc.HexToColor("595959");

        private static IDetour hook_OuiChapterPanel_orig_Update;
        private delegate void orig_OuiChapterPanel_orig_Added(OuiChapterPanel self);

        public void Load()
        {
            hook_OuiChapterPanel_orig_Update = new Hook(
                typeof(OuiChapterPanel).GetMethod("orig_Update"),
                typeof(PatchedOuiChapterPanel).GetMethod("orig_Update", BindingFlags.NonPublic | BindingFlags.Static)
            );
            On.Celeste.OuiChapterPanel.IsStart += IsStart;
            On.Celeste.OuiChapterPanel.Reset += Reset;
        }

        public void Unload()
        {
            hook_OuiChapterPanel_orig_Update.Dispose();
            On.Celeste.OuiChapterPanel.IsStart -= IsStart;
            On.Celeste.OuiChapterPanel.Reset -= Reset;
        }

        private static void orig_Update(orig_OuiChapterPanel_orig_Added orig, OuiChapterPanel self)
        {
            DynamicData oui = DynamicData.For(self);

            if (!oui.Get<bool>("initialized"))
            {
                return;
            }

            var ptr = typeof(Entity).GetMethod("Update").MethodHandle.GetFunctionPointer();
            var entityUpdate = (Action)Activator.CreateInstance(typeof(Action), self, ptr);
            entityUpdate();

            DynamicData options = DynamicData.For(oui.Get("options"));
            var intOption = oui.Get<int>("option");
            DynamicData option;

            for (int i = 0; i < options.Get<int>("Count"); i++)
            {
                option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { i }));
                option.Set("Pop", Calc.Approach((float)option.Get("Pop"), (intOption == i) ? 1f : 0f, Engine.DeltaTime * 4f));
                option.Set("Appear", Calc.Approach((float)option.Get("Appear"), 1f, Engine.DeltaTime * 3f));
                option.Set("CheckpointSlideOut", Calc.Approach((float)option.Get("CheckpointSlideOut"), (intOption > i) ? 1 : 0, Engine.DeltaTime * 4f));
                option.Set("Faded", Calc.Approach((float)option.Get("Faded"), (intOption != i && !(bool)option.Get("Appeared")) ? 1 : 0, Engine.DeltaTime * 4f));
                option.Invoke("SlideTowards", i, options.Get<int>("Count"), false );
            }
            if (oui.Get<bool>("selectingMode") && !oui.Get<bool>("resizing"))
            {
                oui.Set("height", Calc.Approach(oui.Get<float>("height"), oui.Invoke<int>("GetModeHeight"), Engine.DeltaTime * 1600f));
            }
            var wiggler = oui.Get<Wiggler>("wiggler");
            var selectingMode = oui.Get<bool>("selectingMode");
            if (self.Selected && self.Focused)
            {
                if (Input.MenuLeft.Pressed && intOption > 0)
                {
                    Audio.Play("event:/ui/world_map/chapter/tab_roll_left");
                    oui.Set("option", --intOption);
                    wiggler.Start();
                    if (selectingMode)
                    {
                        oui.Invoke("UpdateStats", true, null, null, null);
                        oui.Invoke("PlayExpandSfx", oui.Get<float>("height"), (float)oui.Invoke<int>("GetModeHeight"));
                    }
                    else
                    {
                        Audio.Play("event:/ui/world_map/chapter/checkpoint_photo_add");
                    }
                }
                else if (Input.MenuRight.Pressed && intOption + 1 < options.Get<int>("Count"))
                {
                    Audio.Play("event:/ui/world_map/chapter/tab_roll_right");
                    oui.Set("option", ++intOption);
                    wiggler.Start();
                    if (selectingMode)
                    {
                        oui.Invoke("UpdateStats", true, null, null, null);
                        oui.Invoke("PlayExpandSfx", oui.Get<float>("height"), (float)oui.Invoke<int>("GetModeHeight"));
                    }
                    else
                    {
                        Audio.Play("event:/ui/world_map/chapter/checkpoint_photo_remove");
                    }
                }
                else if (Input.MenuConfirm.Pressed)
                {
                    if (selectingMode)
                    {
                        if (!SaveData.Instance.FoundAnyCheckpoints(self.Area))
                        {
                            if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(self.Area))
                            {
                                self.Start();
                            }
                            else
                            {
                                Audio.Play("event:/ui/main/button_invalid");
                            }
                        }
                        else
                        {
                            Audio.Play("event:/ui/world_map/chapter/level_select");
                            oui.Invoke("Swap");
                        }
                    }
                    else
                    {
                        if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(self.Area))
                        {
                            option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { intOption }));
                            self.Start(option.Get<string>("CheckpointLevelName"));
                        }
                        else
                        {
                            Audio.Play("event:/ui/main/button_invalid");
                        }
                    }
                }
                else if (Input.MenuCancel.Pressed)
                {
                    if (selectingMode)
                    {
                        Audio.Play("event:/ui/world_map/chapter/back");
                        self.Overworld.Goto<OuiChapterSelect>();
                    }
                    else
                    {
                        Audio.Play("event:/ui/world_map/chapter/checkpoint_back");
                        oui.Invoke("Swap");
                    }
                }
            }
            oui.Invoke("SetStatsPosition", true);
        }

        private static bool IsStart(On.Celeste.OuiChapterPanel.orig_IsStart orig, OuiChapterPanel self, Overworld overworld, Overworld.StartMode start)
        {
            DynamicData panel = DynamicData.For(self);

            if (SaveData.Instance != null && SaveData.Instance.LastArea_Safe.ID == AreaKey.None.ID)
            {
                SaveData.Instance.LastArea_Safe = AreaKey.Default;
                panel.Set("instantClose", true);
            }
            if (start == Overworld.StartMode.AreaComplete || start == Overworld.StartMode.AreaQuit)
            {
                AreaData areaData = AreaData.Get(SaveData.Instance.LastArea_Safe.ID);
                areaData = AreaDataExt.Get(areaData?.GetMeta()?.Parent) ?? areaData;
                if (areaData != null)
                {
                    SaveData.Instance.LastArea_Safe.ID = areaData.ID;
                }
            }
            bool num = panel.Invoke<bool>("orig_IsStart", overworld, start);

            DynamicData options = DynamicData.For(panel.Get("options"));
            var intOption = panel.Get<int>("option");
            DynamicData option;

            if (num && intOption >= options.Get<int>("Count") && options.Get<int>("Count") == 1)
            {
                object newOption = Activator.CreateInstance(options.TargetType.GetGenericArguments()[0]);
                option = DynamicData.For(newOption);
                option.Set("Label", Dialog.Clean("overworld_remix"));
                option.Set("ID", "B");
                option.Set("Bg", GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/tab")]);
                if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(new AreaKey(self.Data.ID, AreaMode.BSide)))
                {
                    option.Set("Icon", GFX.Gui[panel.Invoke<string>("_ModMenuTexture", "menu/remix")]);
                    option.Set("BgColor", BgColor);
                }
                else
                {
                    option.Set("Icon", GFX.Gui["archipelago/" + panel.Invoke<string>("_ModMenuTexture", "menu/remix") + "_grey"]);
                    option.Set("BgColor", BgColor_Grey);
                }
                options.Invoke("Add", newOption);
            }
            if (num && intOption >= options.Get<int>("Count") && options.Get<int>("Count") == 2)
            {
                object newOption = Activator.CreateInstance(options.TargetType.GetGenericArguments()[0]);
                option = DynamicData.For(newOption);
                option.Set("Label", Dialog.Clean("overworld_remix2"));
                option.Set("ID", "C");
                option.Set("Bg", GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/tab")]);
                if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(new AreaKey(self.Data.ID, AreaMode.CSide)))
                {
                    option.Set("Icon", GFX.Gui[panel.Invoke<string>("_ModMenuTexture", "menu/rmx2")]);
                    option.Set("BgColor", BgColor);
                }
                else
                {
                    option.Set("Icon", GFX.Gui["archipelago/" + panel.Invoke<string>("_ModMenuTexture", "menu/rmx2") + "_grey"]);
                    option.Set("BgColor", BgColor_Grey);
                }
                options.Invoke("Add", newOption);
            }
            return num;
        }

        private static void Reset(On.Celeste.OuiChapterPanel.orig_Reset orig, OuiChapterPanel self)
        {
            DynamicData panel = DynamicData.For(self);
            DynamicData options = DynamicData.For(panel.Get("modes"));
            var intOption = panel.Get<int>("option");
            DynamicData option;

            self.Area = SaveData.Instance.LastArea_Safe;
            self.Data = AreaData.Areas[self.Area.ID];
            self.RealStats = SaveData.Instance.Areas_Safe[self.Area.ID];
            if (SaveData.Instance.CurrentSession_Safe != null && SaveData.Instance.CurrentSession_Safe.OldStats != null && SaveData.Instance.CurrentSession_Safe.Area.ID == self.Area.ID)
            {
                self.DisplayedStats = SaveData.Instance.CurrentSession_Safe.OldStats;
                SaveData.Instance.CurrentSession_Safe = null;
            }
            else
            {
                self.DisplayedStats = self.RealStats;
            }
            panel.Set("height", (float)panel.Invoke<int>("GetModeHeight"));
            options.Invoke("Clear");
            bool flag = false;
            if (!self.Data.Interlude_Safe && self.Data.HasMode(AreaMode.BSide) && (self.DisplayedStats.Cassette || ((SaveData.Instance.DebugMode || SaveData.Instance.CheatMode) && self.DisplayedStats.Cassette == self.RealStats.Cassette)))
            {
                flag = true;
            }
            bool num = !self.Data.Interlude_Safe && self.Data.HasMode(AreaMode.CSide) && SaveData.Instance.UnlockedModes >= 3 && Celeste.PlayMode != Celeste.PlayModes.Event;

            object newOption = Activator.CreateInstance(options.TargetType.GetGenericArguments()[0]);
            option = DynamicData.For(newOption);
            option.Set("Label", Dialog.Clean(self.Data.Interlude_Safe ? "FILE_BEGIN" : "overworld_normal").ToUpper());
            option.Set("ID", "A");
            option.Set("Bg", GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/tab")]);
            if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(new AreaKey(self.Data.ID, AreaMode.Normal)))
            {
                option.Set("Icon", GFX.Gui[panel.Invoke<string>("_ModMenuTexture", "menu/play")]);
                option.Set("BgColor", BgColor);
            }
            else
            {
                option.Set("Icon", GFX.Gui["archipelago/" + panel.Invoke<string>("_ModMenuTexture", "menu/play") + "_grey"]);
                option.Set("BgColor", BgColor_Grey);
            }
            options.Invoke("Add", newOption);

            if (flag)
            {
                newOption = Activator.CreateInstance(options.TargetType.GetGenericArguments()[0]);
                option = DynamicData.For(newOption);
                option.Set("Label", Dialog.Clean("overworld_remix"));
                option.Set("ID", "B");
                option.Set("Bg", GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/tab")]);
                if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(new AreaKey(self.Data.ID, AreaMode.BSide)))
                {
                    option.Set("Icon", GFX.Gui[panel.Invoke<string>("_ModMenuTexture", "menu/remix")]);
                    option.Set("BgColor", BgColor);
                }
                else
                {
                    option.Set("Icon", GFX.Gui["archipelago/" + panel.Invoke<string>("_ModMenuTexture", "menu/remix") + "_grey"]);
                    option.Set("BgColor", BgColor_Grey);
                }
                options.Invoke("Add", newOption);
            }
            if (num)
            {
                newOption = Activator.CreateInstance(options.TargetType.GetGenericArguments()[0]);
                option = DynamicData.For(newOption);
                option.Set("Label", Dialog.Clean("overworld_remix2"));
                option.Set("ID", "C");
                option.Set("Bg", GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/tab")]);
                if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleSide(new AreaKey(self.Data.ID, AreaMode.CSide)))
                {
                    option.Set("Icon", GFX.Gui[panel.Invoke<string>("_ModMenuTexture", "menu/rmx2")]);
                    option.Set("BgColor", BgColor);
                }
                else
                {
                    option.Set("Icon", GFX.Gui["archipelago/" + panel.Invoke<string>("_ModMenuTexture", "menu/rmx2") + "_grey"]);
                    option.Set("BgColor", BgColor_Grey);
                }
                options.Invoke("Add", newOption);
            }
            panel.Set("selectingMode", true);
            panel.Invoke("UpdateStats", false, null, null, null);
            panel.Invoke("SetStatsPosition", false);

            options = DynamicData.For(panel.Get("options"));
            for (int i = 0; i < options.Get<int>("Count"); i++)
            {
                option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { i }));
                option.Invoke("SlideTowards", i, options.Get<int>("Count"), true);
            }
            panel.Set("chapter", Dialog.Get("area_chapter").Replace("{x}", self.Area.ChapterIndex.ToString().PadLeft(2)));
            panel.Set("contentOffset", new Vector2(440f, 120f));
            panel.Set("initialized", true);
        }

        private static void Render(On.Celeste.OuiChapterPanel.orig_Render orig, OuiChapterPanel self)
        {
            var panel = DynamicData.For(self);

            if (!panel.Get<bool>("initialized"))
            {
                return;
            }
            Vector2 optionsRenderPosition = panel.Get<Vector2>("OptionsRenderPosition");
            DynamicData options = DynamicData.For(panel.Get("options"));
            DynamicData option;
            var intOption = panel.Get<int>("option");
            var wiggler = panel.Get<Wiggler>("wiggler");
            var modeAppearWiggler = panel.Get<Wiggler>("modeAppearWiggler");
            for (int i = 0; i < options.Get<int>("Count"); i++)
            {
                option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { i }));
                if (!option.Get<bool>("OnTopOfUI"))
                {
                    option.Invoke("Render", optionsRenderPosition, intOption == i, wiggler, modeAppearWiggler);
                }
            }
            bool flag = false;
            if (self.RealStats.Modes[(int)self.Area.Mode].Completed)
            {
                int mode = (int)self.Area.Mode;
                foreach (EntityData goldenberry in AreaData.Areas[self.Area.ID].Mode[mode].MapData.Goldenberries)
                {
                    EntityID item = new EntityID(goldenberry.Level.Name, goldenberry.ID);
                    if (self.RealStats.Modes[mode].Strawberries.Contains(item))
                    {
                        flag = true;
                        break;
                    }
                }
            }
            MTexture mTexture = GFX.Gui[(!flag) ? panel.Invoke<string>("_ModAreaselectTexture", "areaselect/cardtop") : panel.Invoke<string>("_ModAreaselectTexture", "areaselect/cardtop_golden")];
            mTexture.Draw(self.Position + new Vector2(0f, -32f));
            MTexture mTexture2 = GFX.Gui[(!flag) ? panel.Invoke<string>("_ModAreaselectTexture", "areaselect/card") : panel.Invoke<string>("_ModAreaselectTexture", "areaselect/card_golden")];
            panel.Set("card", mTexture2.GetSubtexture(0, mTexture2.Height - (int)panel.Get<float>("height"), mTexture2.Width, (int)panel.Get<float>("height"), panel.Get<MTexture>("card")));
            panel.Get<MTexture>("card").Draw(self.Position + new Vector2(0f, -32 + mTexture.Height));
            for (int j = 0; j < options.Get<int>("Count"); j++)
            {
                option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { j }));
                if (option.Get<bool>("OnTopOfUI"))
                {
                    option.Invoke("Render", optionsRenderPosition, intOption == j, wiggler, modeAppearWiggler);
                }
            }
            option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { intOption }));
            ActiveFont.Draw(option.Get<string>("Label"), optionsRenderPosition + new Vector2(0f, -140f), Vector2.One * 0.5f, Vector2.One * (1f + wiggler.Value * 0.1f), Color.Black * 0.8f);
            var contentOffset = panel.Get<Vector2>("contentOffset");
            if (panel.Get<bool>("selectingMode"))
            {
                panel.Get<StrawberriesCounter>("strawberries").Position = contentOffset + new Vector2(0f, 170f) + panel.Get<Vector2>("strawberriesOffset");
                panel.Get<DeathsCounter>("deaths").Position = contentOffset + new Vector2(0f, 170f) + panel.Get<Vector2>("deathsOffset");
                panel.Get<HeartGemDisplay>("heart").Position = contentOffset + new Vector2(0f, 170f) + panel.Get<Vector2>("heartOffset");

                var ptr = typeof(Entity).GetMethod("Render").MethodHandle.GetFunctionPointer();
                var entityRender = (Action)Activator.CreateInstance(typeof(Action), self, ptr);
                entityRender();
            }
            else
            {
                Vector2 center = self.Position + new Vector2(contentOffset.X, 340f);
                for (int num = options.Get<int>("Count") - 1; num >= 0; num--)
                {
                    option = DynamicData.For(options.TargetType.GetProperty("Item").GetValue(options.Target, new object[] { num }));
                    panel.Invoke("DrawCheckpoint", center, option.Target, num);
                }
            }
            GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/title")].Draw(self.Position + new Vector2(panel.Invoke<float>("_FixTitleLength", -60f), 0f), Vector2.Zero, self.Data.TitleBaseColor);
            GFX.Gui[panel.Invoke<string>("_ModAreaselectTexture", "areaselect/accent")].Draw(self.Position + new Vector2(panel.Invoke<float>("_FixTitleLength", -60f), 0f), Vector2.Zero, self.Data.TitleAccentColor);
            string text = Dialog.Clean(AreaData.Get(self.Area).Name);
            if (self.Data.Interlude_Safe)
            {
                ActiveFont.Draw(text, self.Position + self.IconOffset + new Vector2(-100f, 0f), new Vector2(1f, 0.5f), Vector2.One * 1f, self.Data.TitleTextColor * 0.8f);
            }
            else
            {
                ActiveFont.Draw(panel.Get<string>("chapter"), self.Position + self.IconOffset + new Vector2(-100f, -2f), new Vector2(1f, 1f), Vector2.One * 0.6f, self.Data.TitleAccentColor * 0.8f);
                ActiveFont.Draw(text, self.Position + self.IconOffset + new Vector2(-100f, -18f), new Vector2(1f, 0f), Vector2.One * 1f, self.Data.TitleTextColor * 0.8f);
            }
            if (panel.Get<float>("spotlightAlpha") > 0f)
            {
                HiresRenderer.EndRender();
                SpotlightWipe.DrawSpotlight(panel.Get<Vector2>("spotlightPosition"), panel.Get<float>("spotlightRadius"), Color.Black * panel.Get<float>("spotlightAlpha"));
                HiresRenderer.BeginRender();
            }
        }

    }
}

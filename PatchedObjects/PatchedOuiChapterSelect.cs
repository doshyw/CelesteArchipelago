using Celeste.Mod.CelesteArchipelago.Networking;
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
    public class PatchedOuiChapterSelect : IPatchable
    {
        public static bool HasChanges = false;
        private static IDetour hook_OuiChapterSelect_orig_Added;
        private static IDetour hook_OuiChapterSelect_orig_Update;
        private delegate void orig_OuiChapterSelect_orig_Added(OuiChapterSelect self, Scene scene);
        private delegate void orig_OuiChapterSelect_orig_Update(OuiChapterSelect self);

        public void Load()
        {
            hook_OuiChapterSelect_orig_Added = new Hook(
                typeof(OuiChapterSelect).GetMethod("orig_Added"),
                typeof(PatchedOuiChapterSelect).GetMethod("orig_Added", BindingFlags.NonPublic | BindingFlags.Static)
            );
            hook_OuiChapterSelect_orig_Update = new Hook(
                typeof(OuiChapterSelect).GetMethod("orig_Update"),
                typeof(PatchedOuiChapterSelect).GetMethod("orig_Update", BindingFlags.NonPublic | BindingFlags.Static)
            );
        }

        public void Unload()
        {
            hook_OuiChapterSelect_orig_Added.Dispose();
            hook_OuiChapterSelect_orig_Update.Dispose();
        }

        private static void orig_Added(orig_OuiChapterSelect_orig_Added orig, OuiChapterSelect self, Scene scene)
        {
            var ptr = typeof(Entity).GetMethod("Added").MethodHandle.GetFunctionPointer();
            var entityAdded = (Action<Scene>)Activator.CreateInstance(typeof(Action<Scene>), self, ptr);
            entityAdded(scene);

            DynamicData oui = DynamicData.For(self);
            var icons = oui.Get<List<OuiChapterSelectIcon>>("icons");
            var scarf = oui.Get<MTexture>("scarf");
            var indexToSnap = oui.Get<int>("indexToSnap");

            int count = AreaData.Areas.Count;
            for (int i = 0; i < count; i++)
            {
                // MTexture front = GFX.Gui[AreaData.Areas[i].Icon];
                // MTexture back = (GFX.Gui.Has(AreaData.Areas[i].Icon + "_back") ? GFX.Gui[AreaData.Areas[i].Icon + "_back"] : front);

                // NEW: Start
                MTexture front, back;
                if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleLevel(new AreaKey(i)))
                {
                    front = GFX.Gui[AreaData.Areas[i].Icon];
                    back = (GFX.Gui.Has(AreaData.Areas[i].Icon + "_back") ? GFX.Gui[AreaData.Areas[i].Icon + "_back"] : front);
                }
                else
                {
                    front = GFX.Gui["archipelago/" + AreaData.Areas[i].Icon + "_grey"];
                    back = (GFX.Gui.Has("archipelago/" + AreaData.Areas[i].Icon + "_back_grey") ? GFX.Gui["archipelago/" + AreaData.Areas[i].Icon + "_back_grey"] : front);
                }
                // NEW: End

                icons.Add(new OuiChapterSelectIcon(i, front, back));
                self.Scene.Add(icons[i]);
            }
            oui.Set("scarfSegments", new MTexture[scarf.Height / 2]);
            var scarfSegments = oui.Get<MTexture[]>("scarfSegments");
            for (int j = 0; j < scarfSegments.Length; j++)
            {
                scarfSegments[j] = scarf.GetSubtexture(0, j * 2, scarf.Width, 2);
            }
            if (indexToSnap >= 0)
            {
                oui.Set("area", indexToSnap);
                icons[indexToSnap].SnapToSelected();
            }
            self.Depth = -20;
        }

        private static void orig_Update(orig_OuiChapterSelect_orig_Update orig, OuiChapterSelect self)
        {
            if (HasChanges)
            {
                var iconList = DynamicData.For(self).Get<List<OuiChapterSelectIcon>>("icons");
                int count = AreaData.Areas.Count;
                for (int i = 0; i < count; i++)
                {
                    if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleLevel(new AreaKey(i)))
                    {
                        DynamicData.For(iconList[i]).Set("front", GFX.Gui[AreaData.Areas[i].Icon]);
                        DynamicData.For(iconList[i]).Set("back", (GFX.Gui.Has(AreaData.Areas[i].Icon + "_back") ? GFX.Gui[AreaData.Areas[i].Icon + "_back"] : DynamicData.For(iconList[i]).Get<MTexture>("front")));
                    }
                    else
                    {
                        DynamicData.For(iconList[i]).Set("front", GFX.Gui["archipelago/" + AreaData.Areas[i].Icon + "_grey"]);
                        DynamicData.For(iconList[i]).Set("back", (GFX.Gui.Has("archipelago/" + AreaData.Areas[i].Icon + "_back_grey") ? GFX.Gui["archipelago/" + AreaData.Areas[i].Icon + "_back_grey"] : DynamicData.For(iconList[i]).Get<MTexture>("front")));
                    }
                }
                HasChanges = false;
            }

            DynamicData oui = DynamicData.For(self);
            var journalEnabled = oui.Get<bool>("journalEnabled");
            var icons = oui.Get<List<OuiChapterSelectIcon>>("icons");

            if (self.Focused && !oui.Get<bool>("disableInput"))
            {
                oui.Set("inputDelay", oui.Get<float>("inputDelay") - Engine.DeltaTime);
                var area = oui.Get<int>("area");
                if (area >= 0 && area < AreaData.Areas.Count)
                {
                    Input.SetLightbarColor(AreaData.Get(area).TitleBaseColor);
                }
                if (Input.MenuCancel.Pressed)
                {
                    Audio.Play("event:/ui/main/button_back");
                    self.Overworld.Goto<OuiMainMenu>();
                    self.Overworld.Maddy.Hide();
                }
                else if (Input.MenuJournal.Pressed && journalEnabled)
                {
                    Audio.Play("event:/ui/world_map/journal/select");
                    self.Overworld.Goto<OuiJournal>();
                }
                else if (oui.Get<float>("inputDelay") <= 0f)
                {
                    if (area > 0 && Input.MenuLeft.Pressed)
                    {
                        Audio.Play("event:/ui/world_map/icon/roll_left");
                        oui.Set("inputDelay", 0.15f);
                        oui.Set("area", --area);
                        icons[area].Hovered(-1);
                        oui.Invoke("EaseCamera");
                        self.Overworld.Maddy.Hide();
                    }
                    else if (Input.MenuRight.Pressed)
                    {
                        bool flag = SaveData.Instance.AssistMode && area == SaveData.Instance.UnlockedAreas_Safe && area < SaveData.Instance.MaxAssistArea;
                        if (area < SaveData.Instance.UnlockedAreas_Safe || flag)
                        {
                            Audio.Play("event:/ui/world_map/icon/roll_right");
                            oui.Set("inputDelay", 0.15f);
                            oui.Set("area", ++area);
                            icons[area].Hovered(1);
                            if (area <= SaveData.Instance.UnlockedAreas_Safe)
                            {
                                oui.Invoke("EaseCamera");
                            }
                            self.Overworld.Maddy.Hide();
                        }
                    }
                    else if (Input.MenuConfirm.Pressed)
                    {
                        if (icons[area].AssistModeUnlockable)
                        {
                            Audio.Play("event:/ui/world_map/icon/assist_skip");
                            self.Focused = false;
                            self.Overworld.ShowInputUI = false;
                            icons[area].AssistModeUnlock(delegate
                            {
                                self.Focused = true;
                                self.Overworld.ShowInputUI = true;
                                oui.Invoke("EaseCamera");
                                if (area == 10)
                                {
                                    SaveData.Instance.RevealedChapter9 = true;
                                }
                                if (area < SaveData.Instance.MaxAssistArea)
                                {
                                    OuiChapterSelectIcon ouiChapterSelectIcon = icons[area + 1];
                                    ouiChapterSelectIcon.AssistModeUnlockable = true;
                                    ouiChapterSelectIcon.Position = ouiChapterSelectIcon.HiddenPosition;
                                    ouiChapterSelectIcon.Show();
                                }
                            });
                        }
                        else
                        {
                            //Audio.Play("event:/ui/world_map/icon/select");
                            //SaveData.Instance.LastArea_Safe.Mode = AreaMode.Normal;
                            //self.Overworld.Goto<OuiChapterPanel>();

                            // NEW: Start
                            if (ArchipelagoController.Instance.ProgressionSystem.IsAccessibleLevel(new AreaKey(area)))
                            {
                                Audio.Play("event:/ui/world_map/icon/select");
                                SaveData.Instance.LastArea_Safe.Mode = AreaMode.Normal;
                                self.Overworld.Goto<OuiChapterPanel>();
                            }
                            else
                            {
                                Audio.Play("event:/ui/main/button_invalid");
                            }
                            // NEW: End
                        }
                    }
                }
            }
            oui.Set("ease", Calc.Approach(oui.Get<float>("ease"), oui.Get<bool>("display") ? 1f : 0f, Engine.DeltaTime * 3f));
            oui.Set("journalEase", Calc.Approach(oui.Get<float>("journalEase"), (oui.Get<bool>("display") && !oui.Get<bool>("disableInput") && self.Focused && journalEnabled) ? 1f : 0f, Engine.DeltaTime * 4f));

            var ptr = typeof(Entity).GetMethod("Update").MethodHandle.GetFunctionPointer();
            var entityUpdate = (Action)Activator.CreateInstance(typeof(Action), self, ptr);
            entityUpdate();
        }
    }
}

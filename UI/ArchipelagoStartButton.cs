using Celeste.Mod.CelesteArchipelago.PatchedObjects;
using Monocle;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class ArchipelagoStartButton : TextMenu.Button
    {
        public const int saveID = 999;

        public ArchipelagoStartButton(string label, Oui entryOui) : base(label)
        {
            Pressed(() => OnPress(entryOui));
        }

        private string GetModSaveFilePath(int index)
        {
            return SaveData.GetFilename(index) + "-modsave-" + CelesteArchipelagoModule.Instance.Metadata.Name;
        }

        private void OnPress(Oui entryOui)
        {
            Logger.Log("CelesteArchipelago", "Entered ArchipelagoStartButton.OnPress");
            SaveData saveData = null;
            if (UserIO.Open(UserIO.Mode.Read))
            {
                if (UserIO.Exists(GetModSaveFilePath(saveID)))
                {
                    Logger.Log("CelesteArchipelago", "Mod file exists.");
                    SaveData.LoadModSaveData(saveID);
                    var modData = CelesteArchipelagoModule.SaveData;
                    if (modData.UUID == CelesteArchipelagoModule.Settings.UUID && UserIO.Exists(SaveData.GetFilename(saveID)))
                    {
                        Logger.Log("CelesteArchipelago", "Using existing save file.");
                        saveData = UserIO.Load<SaveData>(SaveData.GetFilename(saveID), backup: false);
                        if (saveData != null)
                        {
                            saveData.AfterInitialize();
                        }
                    }
                    else
                    {
                        Logger.Log("CelesteArchipelago", "Deleting old save file.");
                        if (!SaveData.TryDelete(saveID))
                        {
                            SaveData.TryDeleteModSaveData(saveID);
                        }
                    }
                }
                UserIO.Close();
            }

            if (saveData == null)
            {
                Logger.Log("CelesteArchipelago", "Creating new save file.");
                SaveData.TryDeleteModSaveData(saveID);
                saveData = new SaveData
                {
                    Name = CelesteArchipelagoModule.Settings.Name,
                    AssistMode = false,
                    VariantMode = false,
                };
            }

            if(saveData == null)
            {
                return;
            }

            Audio.Play("event:/ui/main/savefile_begin");
            SaveData.Start(saveData, saveID);
            CelesteArchipelagoModule.SaveData.UUID = CelesteArchipelagoModule.Settings.UUID;
            PatchedOuiChapterSelect.HasChanges = true;
            SaveData.Instance.AssistModeChecks();

            CelesteArchipelagoModule.Instance.chatHandler = new ChatHandler(Celeste.Instance);
            Celeste.Instance.Components.Add(CelesteArchipelagoModule.Instance.chatHandler);
            CelesteArchipelagoModule.Instance.chatHandler.Init();

            new ArchipelagoConnection();

            if (SaveData.Instance.CurrentSession_Safe != null && SaveData.Instance.CurrentSession_Safe.InArea)
            {
                Logger.Log("CelesteArchipelago", "Entering existing level.");
                Audio.SetMusic(null);
                Audio.SetAmbience(null);
                entryOui.Overworld.ShowInputUI = false;
                new FadeWipe(entryOui.Scene, wipeIn: false, delegate
                {
                    LevelEnter.Go(SaveData.Instance.CurrentSession_Safe, fromSaveData: true);
                });
            }
            else if (SaveData.Instance.Areas_Safe[0].Modes[0].Completed || SaveData.Instance.CheatMode)
            {
                Logger.Log("CelesteArchipelago", "Going to overworld.");
                if (SaveData.Instance.CurrentSession_Safe != null && SaveData.Instance.CurrentSession_Safe.ShouldAdvance)
                {
                    SaveData.Instance.LastArea_Safe.ID = SaveData.Instance.UnlockedAreas_Safe;
                }
                SaveData.Instance.CurrentSession_Safe = null;

                (entryOui.Scene as Overworld).Goto<OuiChapterSelect>();
            }
            else
            {
                Logger.Log("CelesteArchipelago", "Starting tutorial.");
                Audio.SetMusic(null);
                Audio.SetAmbience(null);
                SaveData.Instance.UnlockedAreas_Safe = SaveData.Instance.MaxArea;
                for (int i = 1; i < SaveData.Instance.MaxArea; i++) 
                {
                    if (AreaData.Areas[i].HasMode(AreaMode.BSide))
                    {
                        SaveData.Instance.Areas_Safe[i].Cassette = true;
                    }
                }
                entryOui.Add(new Coroutine(EnterFirstAreaRoutine(entryOui)));
            }
            Logger.Log("CelesteArchipelago", "Leaving ArchipelagoStartButton.OnPress.");
        }

        private IEnumerator EnterFirstAreaRoutine(Oui entryOui)
        {
            Overworld overworld = entryOui.Overworld;
            AreaData area = AreaData.Areas[SaveData.Instance.LastArea_Safe.ID];
            if (area.GetLevelSet() != "Celeste")
            {
                LevelSetStats levelSetStatsFor = SaveDataExt.GetLevelSetStatsFor(SaveData.Instance, "Celeste");
                levelSetStatsFor.UnlockedAreas = 1;
                levelSetStatsFor.AreasIncludingCeleste[0].Modes[0].Completed = true;
            }
            yield return entryOui.Leave(null);
            overworld.Mountain.Model.EaseState(area.MountainState);
            yield return overworld.Mountain.EaseCamera(0, area.MountainIdle, null, true, false);
            yield return 0.3f;
            overworld.Mountain.EaseCamera(0, area.MountainZoom, 1f, true, false);
            yield return 0.4f;
            area.Wipe(overworld, arg2: false, null);
            RendererListExt.UpdateLists(overworld.RendererList);
            overworld.RendererList.MoveToFront(overworld.Snow);
            yield return 0.5f;
            LevelEnter.Go(new Session(SaveData.Instance.LastArea_Safe), fromSaveData: false);
        }
    }
}

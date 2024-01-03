using Archipelago.MultiClient.Net;
using Monocle;
using System.Collections;

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
                    Logger.Log("CelesteArchipelago", "Deleting existing Archipelago save.");
                    SaveData.TryDelete(saveID);
                }
                UserIO.Close();
            }

            Logger.Log("CelesteArchipelago", "Creating new save file.");
            saveData = new SaveData
            {
                Name = CelesteArchipelagoModule.Settings.Name,
                AssistMode = false,
                VariantMode = false,
            };
            
            ((OuiArchipelago)entryOui).SetFocusedMenu(false);
            ArchipelagoController.Instance.StartSession((result) => OnConnectionAttempt(result, entryOui, saveData));
        }

        private void OnConnectionAttempt(LoginResult result, Oui entryOui, SaveData saveData)
        {
            ((OuiArchipelago)entryOui).SetFocusedMenu(true);
            if (!result.Successful)
            {
                return;
            }

            // Create new savefile.
            Audio.Play("event:/ui/main/savefile_begin");
            SaveData.Start(saveData, saveID);
            PatchedOuiChapterSelect.HasChanges = true;
            SaveData.Instance.AssistModeChecks();

            // Retrieve previous play state from server.
            var state = ArchipelagoController.Instance.PlayState;

            // Set base unlock data.
            SaveData.Instance.UnlockedAreas_Safe = SaveData.Instance.MaxArea;
            for (int i = 1; i < SaveData.Instance.MaxArea; i++)
            {
                if (AreaData.Areas[i].HasMode(AreaMode.BSide))
                {
                    SaveData.Instance.Areas_Safe[i].Cassette = true;
                }
            }

            // Retrieve current items from server.
            ArchipelagoController.Instance.BlockMessages = true;
            ArchipelagoController.Instance.ReceiveItemCallback(ArchipelagoController.Instance.Session.Items);
            ArchipelagoController.Instance.ReplayClientCollected();
            ArchipelagoController.Instance.CheckpointState.ApplyCheckpoints();
            ArchipelagoController.Instance.BlockMessages = false;

            // Enter tutorial if a new player.
            if (state.DoTutorial())
            {
                Logger.Log("CelesteArchipelago", "Starting tutorial.");
                Audio.SetMusic(null);
                Audio.SetAmbience(null);
                entryOui.Add(new Coroutine(EnterFirstAreaRoutine(entryOui)));
            }
            // Return to previously selected overworld level, where relevant.
            else if(state.IsOverworld)
            {
                Logger.Log("CelesteArchipelago", "Going to overworld.");
                ArchipelagoController.Instance.ProgressionSystem.OnCollectedClient(new AreaKey(0), CollectableType.COMPLETION);
                ArchipelagoController.Instance.ProgressionSystem.OnCollectedServer(new AreaKey(0), CollectableType.COMPLETION);
                SaveData.Instance.LastArea_Safe.ID = state.AreaKey.ID;
                (entryOui.Scene as Overworld).Goto<OuiChapterSelect>();
            }
            // Return to currently playing level, where relevant.
            else
            {
                Logger.Log("CelesteArchipelago", "Entering existing level.");
                Audio.SetMusic(null);
                Audio.SetAmbience(null);

                ArchipelagoController.Instance.ProgressionSystem.OnCollectedClient(new AreaKey(0), CollectableType.COMPLETION);
                ArchipelagoController.Instance.ProgressionSystem.OnCollectedServer(new AreaKey(0), CollectableType.COMPLETION);
                SaveData.Instance.LastArea_Safe = state.AreaKey;
                Session session = new Session(state.AreaKey);
                if (session.MapData.Get(state.Room) != null)
                {
                    if (AreaData.GetCheckpoint(state.AreaKey, state.Room) != null)
                    {
                        session = new Session(state.AreaKey, state.Room)
                        {
                            StartCheckpoint = null
                        };
                    }
                    else
                    {
                        session.Level = state.Room;
                    }
                    session.StartedFromBeginning = (session.FirstLevel = state.Room == session.MapData.StartLevel().Name);
                }
                Engine.Scene = new LevelLoader(session);
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

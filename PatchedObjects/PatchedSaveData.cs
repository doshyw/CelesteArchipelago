namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedSaveData : IPatchable
    {
        public void Load()
        {
            On.Celeste.SaveData.RegisterCompletion += RegisterCompletion;
            On.Celeste.SaveData.CheckStrawberry_AreaKey_EntityID += CheckStrawberry;
            On.Celeste.SaveData.SetCheckpoint += SetCheckpoint;
        }

        public void Unload()
        {
            On.Celeste.SaveData.RegisterCompletion -= RegisterCompletion;
            On.Celeste.SaveData.CheckStrawberry_AreaKey_EntityID -= CheckStrawberry;
        }

        private static void RegisterCompletion(On.Celeste.SaveData.orig_RegisterCompletion orig, SaveData self, Session session)
        {
            AreaKey area = session.Area;
            AreaModeStats areaModeStats = self.Areas_Safe[area.ID].Modes[(int)area.Mode];
            if (session.GrabbedGolden)
            {
                areaModeStats.BestDeaths = 0;
            }
            if (session.StartedFromBeginning)
            {
                areaModeStats.SingleRunCompleted = true;
                if (areaModeStats.BestTime <= 0 || session.Deaths < areaModeStats.BestDeaths)
                {
                    areaModeStats.BestDeaths = session.Deaths;
                }
                if (areaModeStats.BestTime <= 0 || session.Dashes < areaModeStats.BestDashes)
                {
                    areaModeStats.BestDashes = session.Dashes;
                }
                if (areaModeStats.BestTime <= 0 || session.Time < areaModeStats.BestTime)
                {
                    if (areaModeStats.BestTime > 0)
                    {
                        session.BeatBestTime = true;
                    }
                    areaModeStats.BestTime = session.Time;
                }
                if (area.Mode == AreaMode.Normal && session.FullClear)
                {
                    areaModeStats.FullClear = true;
                    if (session.StartedFromBeginning && (areaModeStats.BestFullClearTime <= 0 || session.Time < areaModeStats.BestFullClearTime))
                    {
                        areaModeStats.BestFullClearTime = session.Time;
                    }
                }
            }
            //if (area.ID + 1 > self.UnlockedAreas_Safe && area.ID < self.MaxArea)
            //{
            //    self.UnlockedAreas_Safe = area.ID + 1;
            //}
            //areaModeStats.Completed = true;
            if (area.ID == 0)
            {
                areaModeStats.Completed = true;
                ArchipelagoController.Instance.ProgressionSystem.OnCollectedServer(area, CollectableType.COMPLETION);
            }
            ArchipelagoController.Instance.ProgressionSystem.OnCollectedClient(area, CollectableType.COMPLETION); // NEW
            session.InArea = false;
        }

        private static bool CheckStrawberry(On.Celeste.SaveData.orig_CheckStrawberry_AreaKey_EntityID orig, SaveData self, AreaKey area, EntityID strawberry)
        {
            return ArchipelagoController.Instance.ProgressionSystem.IsCollectedVisually(area, CollectableType.STRAWBERRY, strawberry);
        }

        private static bool SetCheckpoint(On.Celeste.SaveData.orig_SetCheckpoint orig, SaveData self, AreaKey area, string level)
        {
            Logger.Log("CelesteArchipelago", $"Set checkpoint at level {level}");
            ArchipelagoController.Instance.CheckpointState.MarkCheckpoint(area, level);
            return orig(self, area, level);
        }

    }
}
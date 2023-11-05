using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public static class PatchedSaveData
    {
        internal static void Load()
        {
            On.Celeste.SaveData.RegisterCompletion += RegisterCompletion;
        }

        internal static void Unload()
        {
            On.Celeste.SaveData.RegisterCompletion -= RegisterCompletion;
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
                CelesteArchipelagoSaveData.SetCompletionInGame(0, 0);
            }
            CelesteArchipelagoSaveData.SetCompletionOutGame((int)area.Mode, area.ID); // NEW
            session.InArea = false;
        }

    }
}
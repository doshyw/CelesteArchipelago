using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class CelesteArchipelagoSaveData : EverestModuleSaveData
    {
        public Dictionary<int, HashSet<EntityID>> Strawberries { get; set; } = new Dictionary<int, HashSet<EntityID>>{
            { 0, new HashSet<EntityID>() },
            { 1, new HashSet<EntityID>() },
            { 2, new HashSet<EntityID>() },
            { 3, new HashSet<EntityID>() },
            { 4, new HashSet<EntityID>() },
            { 5, new HashSet<EntityID>() },
            { 6, new HashSet<EntityID>() },
            { 7, new HashSet<EntityID>() },
            { 8, new HashSet<EntityID>() },
            { 9, new HashSet<EntityID>() },
        };
        public HashSet<int> CassettesInside { get; set; } = new HashSet<int>();
        public HashSet<int> CassettesOutside { get; set; } = new HashSet<int>();
        public Dictionary<int, HashSet<int>> HeartGems { get; set; } = new Dictionary<int, HashSet<int>>{
            { (int)AreaMode.Normal, new HashSet<int>() },
            { (int)AreaMode.BSide, new HashSet<int>() },
            { (int)AreaMode.CSide, new HashSet<int>() },
        };
        public HashSet<string> Poems { get; set; } = new HashSet<string>();
        public Dictionary<int, HashSet<int>> Completions { get; set; } = new Dictionary<int, HashSet<int>>{
            { (int)AreaMode.Normal, new HashSet<int>() },
            { (int)AreaMode.BSide, new HashSet<int>() },
            { (int)AreaMode.CSide, new HashSet<int>() },
        };
        public string UUID { get; set; } = "";

        public static bool IsAccessible(int area)
        {
            return IsAccessible((int)AreaMode.Normal, area)
                || IsAccessible((int)AreaMode.BSide, area)
                || IsAccessible((int)AreaMode.CSide, area);
        }

        public static bool IsAccessible(int mode, int area)
        {
            if (area == 0) return true;
            if (CelesteArchipelagoModule.SaveData == null || SaveData.Instance == null) return false;

            if ((AreaMode)mode == AreaMode.Normal)
            {
                return GetCompletionInGame((int)AreaMode.Normal, area - 1)
                    || GetCompletionInGame((int)AreaMode.BSide, area - 1)
                    || GetCompletionInGame((int)AreaMode.CSide, area - 1);
            }
            else if ((AreaMode) mode == AreaMode.BSide)
            {
                return GetCassetteInGame(area);
            }
            else if ((AreaMode) mode == AreaMode.CSide)
            {
                return GetHeartGemInGame((int)AreaMode.Normal, area)
                    && GetHeartGemInGame((int)AreaMode.BSide, area);
            }
            return false;
        }

        #region Cassettes
        public static void SetCassetteInGame(int area, bool state = true)
        {
            var set = CelesteArchipelagoModule.SaveData.CassettesInside;
            if (state)
            {
                set.Add(area);
            }
            else if (set.Contains(area))
            {
                set.Remove(area);
            }
        }

        public static void SetCassetteOutGame(int area, bool state = true)
        {
            var set = CelesteArchipelagoModule.SaveData.CassettesOutside;
            if (state)
            {
                set.Add(area);
            }
            else if (set.Contains(area))
            {
                set.Remove(area);
            }
        }

        public static bool GetCassetteInGame(int area)
        {
            return CelesteArchipelagoModule.SaveData.CassettesInside.Contains(area);
        }

        public static bool GetCassetteOutGame(int area)
        {
            return CelesteArchipelagoModule.SaveData.CassettesOutside.Contains(area);
        }
        #endregion

        #region Completions
        public static void SetCompletionInGame(int mode, int area, bool state = true)
        {
            SaveData.Instance.Areas_Safe[area].Modes[mode].Completed = state;
        }

        public static void SetCompletionOutGame(int mode, int area, bool state = true)
        {
            var set = CelesteArchipelagoModule.SaveData.Completions[mode];
            if (state)
            {
                set.Add(area);
            }
            else if (set.Contains(area))
            {
                set.Remove(area);
            }
        }

        public static bool GetCompletionInGame(int mode, int area)
        {
            return SaveData.Instance.Areas_Safe[area].Modes[mode].Completed;
        }

        public static bool GetCompletionOutGame(int mode,int area)
        {
            return CelesteArchipelagoModule.SaveData.Completions[mode].Contains(area);
        }
        #endregion

        #region HeartGem
        public static void SetHeartGemInGame(int mode, int area, bool state = true)
        {
            SaveData.Instance.Areas_Safe[area].Modes[mode].HeartGem = state;
        }

        public static void SetHeartGemOutGame(int mode, int area, bool state = true)
        {
            var set = CelesteArchipelagoModule.SaveData.HeartGems[mode];
            if (state)
            {
                set.Add(area);
            }
            else if (set.Contains(area))
            {
                set.Remove(area);
            }
        }

        public static bool GetHeartGemInGame(int mode, int area)
        {
            return SaveData.Instance.Areas_Safe[area].Modes[mode].HeartGem;
        }

        public static bool GetHeartGemOutGame(int mode, int area)
        {
            return CelesteArchipelagoModule.SaveData.HeartGems[mode].Contains(area);
        }
        #endregion

        #region Strawberries
        public static void SetStrawberryInGame(int area, EntityID berry, bool state = true)
        {
            if (state)
            {
                SaveData.Instance.AddStrawberry(new AreaKey(area), berry, false);
            }
            else
            {
                AreaModeStats areaModeStats = SaveData.Instance.Areas_Safe[area].Modes[0];
                if (areaModeStats.Strawberries.Contains(berry))
                {
                    areaModeStats.Strawberries.Remove(berry);
                    areaModeStats.TotalStrawberries--;
                    SaveData.Instance.TotalStrawberries_Safe--;
                }
            }
        }

        public static void SetStrawberryOutGame(int area, EntityID berry, bool state = true)
        {
            var set = CelesteArchipelagoModule.SaveData.Strawberries[area];
            if (state)
            {
                set.Add(berry);
            }
            else if (set.Contains(berry))
            {
                set.Remove(berry);
            }
        }

        public static bool GetStrawberryInGame(int area, EntityID berry)
        {
            return SaveData.Instance.CheckStrawberry(new AreaKey(area), berry);
        }

        public static bool GetStrawberryOutGame(int area, EntityID berry)
        {
            return CelesteArchipelagoModule.SaveData.Strawberries[area].Contains(berry);
        }
        #endregion
    }
}

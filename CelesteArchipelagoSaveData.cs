using Newtonsoft.Json.Serialization;
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

        public static bool IsHeartGemDoorOpenable()
        {
            var slotData = ArchipelagoConnection.Instance.slotData;

            //Logger.Log("CelesteArchipelago", $"TotalHeartGemsInGame: {TotalHeartGemsInGame()}");
            //Logger.Log("CelesteArchipelago", $"TotalStrawberriesInGame: {TotalStrawberriesInGame()}");
            //Logger.Log("CelesteArchipelago", $"TotalCompletionsInGame: {TotalCompletionsInGame()}");
            //Logger.Log("CelesteArchipelago", $"TotalCassettesInGame: {TotalCassettesInGame()}");
            //Logger.Log("CelesteArchipelago", $"slotData.HeartsRequired: {slotData.HeartsRequired}");
            //Logger.Log("CelesteArchipelago", $"slotData.BerriesRequired: {slotData.BerriesRequired}");
            //Logger.Log("CelesteArchipelago", $"slotData.LevelsRequired: {slotData.LevelsRequired}");
            //Logger.Log("CelesteArchipelago", $"slotData.CassettesRequired: {slotData.CassettesRequired}");

            return TotalHeartGemsInGame() >= slotData.HeartsRequired
                && TotalStrawberriesInGame() >= slotData.BerriesRequired
                && TotalCompletionsInGame() >= slotData.LevelsRequired
                && TotalCassettesInGame() >= slotData.CassettesRequired;
        }

        #region Cassettes
        public static void SetCassetteInGame(int area)
        {
            CelesteArchipelagoModule.SaveData.CassettesInside.Add(area);
        }

        public static void SetCassetteOutGame(int area)
        {
            CelesteArchipelagoModule.SaveData.CassettesOutside.Add(area);
            ArchipelagoConnection.Instance.CheckLocation(new ArchipelagoNetworkItem(ItemType.CASSETTE, area, 0));
        }

        public static bool GetCassetteInGame(int area)
        {
            return CelesteArchipelagoModule.SaveData.CassettesInside.Contains(area);
        }

        public static bool GetCassetteOutGame(int area)
        {
            return CelesteArchipelagoModule.SaveData.CassettesOutside.Contains(area);
        }

        public static int TotalCassettesInGame()
        {
            return Enumerable.Range(1, SaveData.Instance.MaxArea)
                .Count(area => GetCassetteInGame(area));
        }

        public static int TotalCassettesOutGame()
        {
            return Enumerable.Range(1, SaveData.Instance.MaxArea)
                .Count(area => GetCassetteOutGame(area));
        }

        #endregion

        #region Completions
        public static void SetCompletionInGame(int mode, int area)
        {
            SaveData.Instance.Areas_Safe[area].Modes[mode].Completed = true;
        }

        public static void SetCompletionOutGame(int mode, int area)
        {
            CelesteArchipelagoModule.SaveData.Completions[mode].Add(area);
            ArchipelagoConnection.Instance.CheckLocation(new ArchipelagoNetworkItem(ItemType.COMPLETION, area, mode));
        }

        public static bool GetCompletionInGame(int mode, int area)
        {
            return SaveData.Instance.Areas_Safe[area].Modes[mode].Completed;
        }

        public static bool GetCompletionOutGame(int mode, int area)
        {
            return CelesteArchipelagoModule.SaveData.Completions[mode].Contains(area);
        }

        public static int TotalCompletionsInGame()
        {
            return Enumerable.Range(1, SaveData.Instance.MaxArea)
                .Select(area => SaveData.Instance.Areas_Safe[area].Modes)
                .SelectMany(modeArr => modeArr.Select(mode => mode.Completed))
                .Count(val => val);
        }

        public static int TotalCompletionsOutGame()
        {
            return CelesteArchipelagoModule.SaveData.Completions[0].Count()
                + CelesteArchipelagoModule.SaveData.Completions[1].Count()
                + CelesteArchipelagoModule.SaveData.Completions[2].Count();
        }
        #endregion

        #region HeartGem
        public static void SetHeartGemInGame(int mode, int area)
        {
            SaveData.Instance.Areas_Safe[area].Modes[mode].HeartGem = true;
        }

        public static void SetHeartGemOutGame(int mode, int area)
        {
            CelesteArchipelagoModule.SaveData.HeartGems[mode].Add(area);
            ArchipelagoConnection.Instance.CheckLocation(new ArchipelagoNetworkItem(ItemType.GEMHEART, area, mode));
        }

        public static bool GetHeartGemInGame(int mode, int area)
        {
            return SaveData.Instance.Areas_Safe[area].Modes[mode].HeartGem;
        }

        public static bool GetHeartGemOutGame(int mode, int area)
        {
            return CelesteArchipelagoModule.SaveData.HeartGems[mode].Contains(area);
        }

        public static int TotalHeartGemsInGame()
        {
            return Enumerable.Range(1, SaveData.Instance.MaxArea)
                .Select(area => SaveData.Instance.Areas_Safe[area].Modes)
                .SelectMany(modeArr => modeArr.Select(mode => mode.HeartGem))
                .Count(val => val);
        }

        public static int TotalHeartGemsOutGame()
        {
            return CelesteArchipelagoModule.SaveData.HeartGems[0].Count()
                + CelesteArchipelagoModule.SaveData.HeartGems[1].Count()
                + CelesteArchipelagoModule.SaveData.HeartGems[2].Count();
        }
        #endregion

        #region Strawberries
        public static void SetStrawberryInGame(int area, EntityID berry, bool state = true)
        {
            SaveData.Instance.AddStrawberry(new AreaKey(area), berry, false);
            //AreaModeStats areaModeStats = SaveData.Instance.Areas_Safe[area].Modes[0];
            //if (areaModeStats.Strawberries.Contains(berry))
            //{
            //    areaModeStats.Strawberries.Remove(berry);
            //    areaModeStats.TotalStrawberries--;
            //    SaveData.Instance.TotalStrawberries_Safe--;
            //}
        }

        public static void SetStrawberryOutGame(int area, EntityID berry)
        {
            CelesteArchipelagoModule.SaveData.Strawberries[area].Add(berry);
            ArchipelagoConnection.Instance.CheckLocation(new ArchipelagoNetworkItem(ItemType.STRAWBERRY, area, 0, berry));
        }

        public static bool GetStrawberryInGame(int area, EntityID berry)
        {
            return SaveData.Instance.CheckStrawberry(new AreaKey(area), berry);
        }

        public static bool GetStrawberryOutGame(int area, EntityID berry)
        {
            return CelesteArchipelagoModule.SaveData.Strawberries[area].Contains(berry);
        }

        public static int TotalStrawberriesInGame()
        {
            return Enumerable.Range(1, SaveData.Instance.MaxArea)
                .Select(area => SaveData.Instance.Areas_Safe[area].Modes)
                .SelectMany(modeArr => modeArr.Select(mode => mode.Strawberries.Count()))
                .Sum();
        }

        public static int TotalStrawberriesOutGame()
        {
            return Enumerable.Range(1, SaveData.Instance.MaxArea)
                .Select(area => CelesteArchipelagoModule.SaveData.Strawberries[area].Count())
                .Sum();
        }
        #endregion

    }
}

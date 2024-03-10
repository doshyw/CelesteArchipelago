using System;
using System.Linq;

namespace Celeste.Mod.CelesteArchipelago
{
    public class DefaultProgression : IProgressionSystem
    {
        private int StrawberryCount { get; set; } = 0;
        private FlagStorage CassettesVisual { get; set; } = new FlagStorage();
        private FlagStorage CassettesLogical { get; set; } = new FlagStorage();
        private FlagStorage HeartGems { get; set; } = new FlagStorage();
        private FlagStorage Completions { get; set; } = new FlagStorage();
        private ArchipelagoSlotData SlotData;

        public DefaultProgression(ArchipelagoSlotData data)
        {
            SlotData = data;
        }

        public bool IsAccessibleLevel(AreaKey area)
        {
            return IsAccessibleSide(new AreaKey(area.ID, AreaMode.Normal))
                || IsAccessibleSide(new AreaKey(area.ID, AreaMode.BSide))
                || IsAccessibleSide(new AreaKey(area.ID, AreaMode.CSide));
        }

        public bool IsAccessibleSide(AreaKey area)
        {
            if (area.ID == 0) return true;
            if (SaveData.Instance == null) return false;

            var goalLevel = GetGoalLevel();
            bool externalCheck = !(area == goalLevel) || IsGoalLevelAccessible();

            if (LevelIsBefore(goalLevel, area))
            {
                return false;
            }

            if (area.Mode == AreaMode.Normal)
            {
                return externalCheck && (
                    IsCollectedLogically(new AreaKey(area.ID - 1, AreaMode.Normal), CollectableType.COMPLETION)
                    || IsCollectedLogically(new AreaKey(area.ID - 1, AreaMode.BSide), CollectableType.COMPLETION)
                    || IsCollectedLogically(new AreaKey(area.ID - 1, AreaMode.CSide), CollectableType.COMPLETION)
                );
            }
            else if (area.Mode == AreaMode.BSide)
            {
                return externalCheck && IsCollectedLogically(new AreaKey(area.ID, AreaMode.Normal), CollectableType.CASSETTE);
            }
            else if (area.Mode == AreaMode.CSide)
            {
                return externalCheck &&
                    IsCollectedLogically(new AreaKey(area.ID, AreaMode.Normal), CollectableType.HEARTGEM)
                    && IsCollectedLogically(new AreaKey(area.ID, AreaMode.BSide), CollectableType.HEARTGEM);
            }
            return false;
        }

        public bool IsCollectedVisually(AreaKey area, CollectableType collectable, EntityID? entity = null)
        {
            switch (collectable)
            {
                case CollectableType.CASSETTE:
                    return CassettesVisual.IsFlagged(area);
                case CollectableType.COMPLETION:
                    return SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].Completed;
                case CollectableType.HEARTGEM:
                    return SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].HeartGem;
                case CollectableType.STRAWBERRY:
                    return entity != null && SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].Strawberries.Contains(entity.Value);
                default:
                    throw new ArgumentOutOfRangeException($"CollectableType {collectable} not implemented.");
            }
        }

        public bool IsCollectedLogically(AreaKey area, CollectableType collectable, EntityID? entity = null)
        {
            switch (collectable)
            {
                case CollectableType.CASSETTE:
                    return CassettesLogical.IsFlagged(area);
                case CollectableType.COMPLETION:
                    return Completions.IsFlagged(area);
                case CollectableType.HEARTGEM:
                    return HeartGems.IsFlagged(area);
                case CollectableType.STRAWBERRY:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException($"CollectableType {collectable} not implemented.");
            }
        }

        public void OnCollectedClient(AreaKey area, CollectableType collectable, EntityID? entity = null, bool isReplay = false)
        {
            switch (collectable)
            {
                case CollectableType.CASSETTE:
                    CassettesVisual.Flag(area);
                    break;
                case CollectableType.COMPLETION:
                    SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].Completed = true;
                    break;
                case CollectableType.HEARTGEM:
                    SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode].HeartGem = true;
                    break;
                case CollectableType.STRAWBERRY:
                    if (entity == null) return;
                    SaveData.Instance.AddStrawberry(area, entity.Value, false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"CollectableType {collectable} not implemented.");
            }
            if(!isReplay)
            {
                ArchipelagoController.Instance.SendLocationCallback(new ArchipelagoNetworkItem(collectable, area, entity));
            }
        }

        public void OnCollectedServer(AreaKey area, CollectableType collectable, EntityID? entity = null)
        {
            switch (collectable)
            {
                case CollectableType.CASSETTE:
                    CassettesLogical.Flag(area);
                    break;
                case CollectableType.COMPLETION:
                    Completions.Flag(area);
                    break;
                case CollectableType.HEARTGEM:
                    HeartGems.Flag(area);
                    break;
                case CollectableType.STRAWBERRY:
                    StrawberryCount += 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"CollectableType {collectable} not implemented.");
            }
        }

        public int GetTotalVisually(CollectableType collectable)
        {
            
            switch (collectable)
            {
                case CollectableType.CASSETTE:
                    return CassettesVisual.GetTotal();
                case CollectableType.COMPLETION:
                    return Enumerable.Range(1, SaveData.Instance.MaxArea)
                       .Select(area => SaveData.Instance.Areas_Safe[area].Modes)
                       .SelectMany(modeArr => modeArr.Select(mode => mode.Completed))
                       .Count(val => val);
                case CollectableType.HEARTGEM:
                    return SaveData.Instance.TotalHeartGems;
                case CollectableType.STRAWBERRY:
                    return SaveData.Instance.TotalStrawberries_Safe;
                default:
                    throw new ArgumentOutOfRangeException($"CollectableType {collectable} not implemented.");
            }
        }

        public int GetTotalLogically(CollectableType collectable)
        {
            switch (collectable)
            {
                case CollectableType.CASSETTE:
                    return CassettesLogical.GetTotal();
                case CollectableType.COMPLETION:
                    return Completions.GetTotal();
                case CollectableType.HEARTGEM:
                    return HeartGems.GetTotal();
                case CollectableType.STRAWBERRY:
                    return StrawberryCount;
                default:
                    throw new ArgumentOutOfRangeException($"CollectableType {collectable} not implemented.");
            }
        }

        private bool LevelIsBefore(AreaKey area1, AreaKey area2)
        {
            int hash1 = area1.ID * 100 + (int)area1.Mode;
            int hash2 = area2.ID * 100 + (int)area2.Mode;
            return hash1 < hash2;
        }

        public AreaKey GetGoalLevel()
        {
            var option = (VictoryConditionOptions)SlotData.VictoryCondition;
            switch (option)
            {
                case VictoryConditionOptions.CHAPTER_7_SUMMIT_A:
                    return new AreaKey(7, AreaMode.Normal);
                case VictoryConditionOptions.CHAPTER_7_SUMMIT_B:
                    return new AreaKey(7, AreaMode.BSide);
                case VictoryConditionOptions.CHAPTER_7_SUMMIT_C:
                    return new AreaKey(7, AreaMode.CSide);
                case VictoryConditionOptions.CHAPTER_8_CORE_A:
                    return new AreaKey(9, AreaMode.Normal);
                case VictoryConditionOptions.CHAPTER_8_CORE_B:
                    return new AreaKey(9, AreaMode.BSide);
                case VictoryConditionOptions.CHAPTER_8_CORE_C:
                    return new AreaKey(9, AreaMode.CSide);
                case VictoryConditionOptions.CHAPTER_9_FAREWELL_A:
                    return new AreaKey(10, AreaMode.Normal);
                default:
                    throw new ArgumentOutOfRangeException($"Victory Condition {option} not implemented.");
            }
        }

        private bool IsGoalLevelAccessible()
        {
            return GetTotalLogically(CollectableType.CASSETTE) >= SlotData.CassettesRequired
                && GetTotalLogically(CollectableType.COMPLETION) >= SlotData.LevelsRequired
                && GetTotalLogically(CollectableType.HEARTGEM) >= SlotData.HeartsRequired
                && GetTotalLogically(CollectableType.STRAWBERRY) >= SlotData.BerriesRequired;
        }
    }
}

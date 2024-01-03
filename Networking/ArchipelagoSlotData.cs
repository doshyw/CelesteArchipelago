using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ArchipelagoSlotData
    {
        public long BerriesRequired { get; set; } = 0;
        public long CassettesRequired { get; set; } = 0;
        public long HeartsRequired { get; set; } = 15;
        public long LevelsRequired { get; set; } = 0;
        public long VictoryCondition { get; set; } = 0;

        private Dictionary<string, PropertyInfo> keyPropertyMap = new Dictionary<string, PropertyInfo>
        {
            { "berries_required", typeof(ArchipelagoSlotData).GetProperty("BerriesRequired") },
            { "cassettes_required", typeof(ArchipelagoSlotData).GetProperty("CassettesRequired") },
            { "hearts_required", typeof(ArchipelagoSlotData).GetProperty("HeartsRequired") },
            { "levels_required", typeof(ArchipelagoSlotData).GetProperty("LevelsRequired") },
            { "victory_condition", typeof(ArchipelagoSlotData).GetProperty("VictoryCondition") },
        };

        public ArchipelagoSlotData(Dictionary<string, object> slotData)
        {
            foreach (var keyValuePair in slotData)
            {
                SetSlotDataFromPython(keyValuePair.Key, keyValuePair.Value);
            }
        }

        public void SetSlotDataFromPython(string key, object data)
        {
            if (!keyPropertyMap.ContainsKey(key))
            {
                Logger.Log("CelesteArchipelago", $"Failed to get slot data with key {key}");
                return;
            }
            var property = keyPropertyMap[key];

            if (property.PropertyType != data.GetType())
            {
                Logger.Log("CelesteArchipelago", $"Slot data type of {property.PropertyType} for key {key} does not match communicated object type of {data.GetType()}");
                return;
            }
            property.SetValue(this, data);
            Logger.Log("CelesteArchipelago", $"Slot data for key {key} set to {property.GetValue(this)}");
        }
    }

    internal enum VictoryConditionOptions
    {
        CHAPTER_7_SUMMIT = 0,
        CHAPTER_8_CORE = 1,
        CHAPTER_9_FAREWELL = 2,
    }
}

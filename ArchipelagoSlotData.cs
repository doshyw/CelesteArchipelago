using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class ArchipelagoSlotData
    {
        public int BerriesRequired { get; set; } = 0;
        public int HeartsRequired { get; set; } = 15;
        public int LevelsRequired { get; set; } = 0;

        private Dictionary<string, PropertyInfo> pythonCSharpMap = new Dictionary<string, PropertyInfo>
        {
            { "berries_required", typeof(ArchipelagoSlotData).GetProperty("BerriesRequired") },
            { "hearts_required", typeof(ArchipelagoSlotData).GetProperty("HeartsRequired") },
            { "levels_required", typeof(ArchipelagoSlotData).GetProperty("LevelsRequired") },
        };

        public ArchipelagoSlotData(Dictionary<string, object> slotData)
        {
            foreach(var keyValuePair in slotData)
            {
                SetSlotDataFromPython(keyValuePair.Key, keyValuePair.Value);
            }
        }

        public void SetSlotDataFromPython(string pythonSlot, object data)
        {
            var property = pythonCSharpMap[pythonSlot];

            if(property.PropertyType == data.GetType())
            {
                property.SetValue(this, data);
            }
        }

    }
}

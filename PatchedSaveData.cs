using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class PatchedSaveData
    {
        // Used to check if a Strawberry should be treated as a Ghost Berry (i.e., has been physically obtained).
        public static bool CheckStrawberry_AreaKey_EntityID(On.Celeste.SaveData.orig_CheckStrawberry_AreaKey_EntityID orig, SaveData self, AreaKey area, EntityID strawberry)
        {
            return CelesteArchipelagoModule.SaveData.Strawberries.Contains(strawberry);
        }

        public static bool CheckStrawberry_EntityID(On.Celeste.SaveData.orig_CheckStrawberry_EntityID orig, SaveData self, EntityID strawberry)
        {
            return CelesteArchipelagoModule.SaveData.Strawberries.Contains(strawberry);
        }
    }
}
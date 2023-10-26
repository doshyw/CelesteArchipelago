using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class CelesteArchipelagoSaveData : EverestModuleSaveData
    {
        public HashSet<EntityID> Strawberries { get; set; } = new HashSet<EntityID>();
    }
}

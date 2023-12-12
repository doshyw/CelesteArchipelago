using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.CelesteArchipelago.Networking;

namespace Celeste.Mod.CelesteArchipelago.Progression.Interfaces
{
    public interface IProgressionSystem
    {
        public bool IsAccessibleLevel(AreaKey area);
        public bool IsAccessibleSide(AreaKey area);

        public void OnCollectedClient(AreaKey area, CollectableType collectable, EntityID? entity = null);
        public void OnCollectedServer(AreaKey area, CollectableType collectable, EntityID? entity = null);

        public bool IsCollectedVisually(AreaKey area, CollectableType collectable, EntityID? entity = null);
        public bool IsCollectedLogically(AreaKey area, CollectableType collectable, EntityID? entity = null);

        public int GetTotalVisually(CollectableType collectable);
        public int GetTotalLogically(CollectableType collectable);

    }
}

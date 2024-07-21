using System;

namespace Celeste.Mod.CelesteArchipelago
{
    public class NullProgression : IProgressionSystem
    {

        public NullProgression()
        {
        }

        public int GetTotalLogically(CollectableType collectable)
        {
            return 0;
        }

        public int GetTotalVisually(CollectableType collectable)
        {
            return 0;
        }

        public bool IsAccessibleLevel(AreaKey area)
        {
            return false;
        }

        public bool IsAccessibleSide(AreaKey area)
        {
            return false;
        }

        public bool IsCollectedLogically(AreaKey area, CollectableType collectable, EntityID? entity = null)
        {
            return false;
        }

        public bool IsCollectedVisually(AreaKey area, CollectableType collectable, EntityID? entity = null)
        {
            return false;
        }

        public void OnCollectedClient(AreaKey area, CollectableType collectable, EntityID? entity = null, bool isReplay = false)
        {
        }

        public void OnCollectedServer(AreaKey area, CollectableType collectable, EntityID? entity = null)
        {
        }

        public AreaKey GetGoalLevel()
        {
            throw new NotImplementedException();
        }
    }
}

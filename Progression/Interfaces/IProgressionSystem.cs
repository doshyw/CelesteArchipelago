namespace Celeste.Mod.CelesteArchipelago
{
    public interface IProgressionSystem
    {
        public bool IsAccessibleLevel(AreaKey area);
        public bool IsAccessibleSide(AreaKey area);

        public void OnCollectedClient(AreaKey area, CollectableType collectable, EntityID? entity = null, bool isReplay = false);
        public void OnCollectedServer(AreaKey area, CollectableType collectable, EntityID? entity = null);

        public bool IsCollectedVisually(AreaKey area, CollectableType collectable, EntityID? entity = null);
        public bool IsCollectedLogically(AreaKey area, CollectableType collectable, EntityID? entity = null);

        public int GetTotalVisually(CollectableType collectable);
        public int GetTotalLogically(CollectableType collectable);

    }
}

using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.CelesteArchipelago
{

    public class PatchedPlayer : IPatchable
    {
        public void Load()
        {
            On.Celeste.Player.Update += Update;
            Everest.Events.Player.OnSpawn += OnSpawn;
            Everest.Events.Player.OnDie += OnDie;
        }

        public void Unload()
        {
            On.Celeste.Player.Update -= Update;
            Everest.Events.Player.OnSpawn -= OnSpawn;
            Everest.Events.Player.OnDie -= OnDie;
        }

        private static void Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            if (ArchipelagoController.Instance.DeathLinkStatus == DeathLinkStatus.Pending)
            {
                self.Die(Vector2.Zero, true);
            }
            orig.Invoke(self);
        }

        private static void OnSpawn(Player player)
        {
            if (ArchipelagoController.Instance.DeathLinkStatus == DeathLinkStatus.Dying)
            {
                ArchipelagoController.Instance.DeathLinkStatus = DeathLinkStatus.None;
            }
        }

        private static void OnDie(Player player)
        {
            ArchipelagoController.Instance.SendDeathLinkCallback();
            ArchipelagoController.Instance.DeathLinkStatus = DeathLinkStatus.Dying;
            ArchipelagoController.Instance.isLocalDeath = true;
        }
    }
}
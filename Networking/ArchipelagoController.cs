using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Celeste.Mod.CelesteArchipelago.PatchedObjects;
using Celeste.Mod.CelesteArchipelago.Progression.Interfaces;
using FMOD;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago.Networking
{
    public class ArchipelagoController : DrawableGameComponent
    {
        public static ArchipelagoController Instance { get; private set; }
        public IProgressionSystem ProgressionSystem { get; set; }
        public ArchipelagoSession Session
        {
            get
            {
                return Connection.Session;
            }
        }
        public ArchipelagoSlotData SlotData
        {
            get
            {
                return Connection.SlotData;
            }
        }

        private ChatHandler ChatHandler { get; set; }
        private Connection Connection { get; set; }
        private List<IPatchable> patchObjects = new List<IPatchable>
        {
        };

        public ArchipelagoController(Game game) : base(game)
        {
            UpdateOrder = 9000;
            DrawOrder = 9000;
            Instance = this;
            game.Components.Add(this);
        }

        public override void Update(GameTime gameTime)
        {

        }

        public void StartSession(Action<LoginResult> onLogin)
        {
            var parameters = new ConnectionParameters(
                game:     "Celeste",
                server:   CelesteArchipelagoModule.Settings.Server,
                port:     CelesteArchipelagoModule.Settings.Port,
                name:     CelesteArchipelagoModule.Settings.Name,
                flags:    ItemsHandlingFlags.AllItems,
                version:  new Version(0, 4, 3),
                tags:     null,
                uuid:     null,
                password: CelesteArchipelagoModule.Settings.Password,
                slotData: true
            );

            Connection = new Connection(Celeste.Instance, parameters, onLogin);
            Connection.Disposed += (sender, args) =>
            {
                Connection = null;
            };
        }

        protected override void Dispose(bool disposing)
        {

        }

    }
}

using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Packets;
using FMOD;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ArchipelagoConnectionAttempt : GameComponent
    {
        private ArchipelagoSession session;
        private Action<LoginResult> callback;
        private Task<RoomInfoPacket> connectTask;
        private Task<LoginResult> loginTask;
        private Func<Task<LoginResult>> loginTaskCreator;

        public ArchipelagoConnectionAttempt(Game game, Action<LoginResult> callback, ArchipelagoSession session, string archGame, string name, ItemsHandlingFlags itemsHandlingFlags, Version version = null, string[] tags = null, string uuid = null, string password = null, bool requestSlotData = true) : base(game)
        {
            UpdateOrder = 9999;
            Enabled = true;
            this.session = session;
            this.callback = callback;
            loginTaskCreator = () => session.LoginAsync(archGame, name, itemsHandlingFlags, version, tags, uuid, password, requestSlotData);
        }

        public override void Update(GameTime gameTime)
        {
            if(connectTask == null)
            {
                Logger.Log("CelesteArchipelago", "Attempting to open connection to Archipelago server.");
                connectTask = session.ConnectAsync();
                return;
            }

            if(connectTask.IsCompleted && loginTask == null)
            {
                if (connectTask.IsCanceled)
                {
                    Logger.Log("CelesteArchipelago", "Connection to Archipelago server failed.");
                    callback(new LoginFailure("Connection timed out."));
                    Dispose(true);
                    return;
                }
                Logger.Log("CelesteArchipelago", "Connection to Archipelago server successful.");
                loginTask = loginTaskCreator();
            }

            if(loginTask != null && loginTask.IsCompleted)
            {
                if(loginTask.Result.Successful)
                {
                    Logger.Log("CelesteArchipelago", "Login to Archipelago server successful.");
                }
                else
                {
                    Logger.Log("CelesteArchipelago", "Login to Archipelago server failed.");
                }
                callback(loginTask.Result);
                Dispose(true);
                return;
            }
        }

        protected override void Dispose(bool disposing)
        {
            Enabled = false;
            base.Dispose(disposing);
        }
    }
}

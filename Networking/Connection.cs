using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net;
using Microsoft.Xna.Framework;
using System;
using System.Threading.Tasks;
using MonoMod.Utils;
using System.Net.WebSockets;
using System.Threading;
using Archipelago.MultiClient.Net.Enums;
using System.Collections.Generic;

namespace Celeste.Mod.CelesteArchipelago.Networking
{
    public class Connection : GameComponent
    {
        public ArchipelagoSession Session { get; private set; }
        public ArchipelagoSlotData SlotData { get; private set; }

        private Action<LoginResult> callback;
        private ConnectionParameters parameters;
        private ConnectionState connectionState;
        private Task<RoomInfoPacket> connectTask;
        private Task<LoginResult> loginTask;

        public Connection(Game game, ConnectionParameters parameters, Action<LoginResult> callback) : base(game)
        {
            UpdateOrder = 9999;
            connectionState = ConnectionState.UNCONNECTED;
            this.parameters = parameters;
            this.callback = callback;
            Session = ArchipelagoSessionFactory.CreateSession($"{parameters.server}:{parameters.port}");
        }

        public override void Update(GameTime gameTime)
        {
            switch(connectionState)
            {
                case ConnectionState.UNCONNECTED:
                    BeginConnect();
                    break;
                case ConnectionState.CONNECTING:
                    WaitConnect();
                    break;
                case ConnectionState.CONNECTED:
                    BeginLogin();
                    break;
                case ConnectionState.LOGGING_IN:
                    WaitLogin();
                    break;
                case ConnectionState.LOGGED_IN:
                    CheckConnection();
                    break;
                default:
                    break;
            }
        }

        private void BeginConnect()
        {
            Logger.Log("CelesteArchipelago", "Attempting to open connection to Archipelago server.");
            connectionState = ConnectionState.CONNECTING;
            connectTask = Session.ConnectAsync();
        }

        private void WaitConnect()
        {
            if (connectTask.IsCompleted)
            {
                if (connectTask.IsCanceled)
                {
                    Logger.Log("CelesteArchipelago", "Connection to Archipelago server failed.");
                    HandleLoginResult(new LoginFailure("Connection timed out."));
                    Dispose();
                    return;
                }
                Logger.Log("CelesteArchipelago", "Connection to Archipelago server successful.");
                connectionState = ConnectionState.CONNECTING;
            }
        }

        private void BeginLogin()
        {
            Logger.Log("CelesteArchipelago", "Attempting login to Archipelago server.");
            connectionState = ConnectionState.LOGGING_IN;
            loginTask = parameters.DoLogin(Session);
        }

        private void WaitLogin()
        {
            if (loginTask.IsCompleted)
            {
                if (!loginTask.Result.Successful)
                {
                    Logger.Log("CelesteArchipelago", "Login to Archipelago server failed.");
                    HandleLoginResult(loginTask.Result);
                    Dispose();
                    return;
                }
                Logger.Log("CelesteArchipelago", "Login to Archipelago server successful.");
                connectionState = ConnectionState.LOGGED_IN;
                HandleLoginResult(loginTask.Result);
            }
        }

        private void HandleLoginResult(LoginResult result)
        {
            if(result.Successful)
            {
                SlotData = new ArchipelagoSlotData(((LoginSuccessful)result).SlotData);
            }
            else
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMsg = $"Failed to connect to {Session.Socket.Uri.Host}:{Session.Socket.Uri.Port} as {CelesteArchipelagoModule.Settings.Name}";
                Logger.Log("CelesteArchipelago", errorMsg);
                CelesteArchipelagoModule.Instance.chatHandler.HandleMessage(errorMsg, Color.Red);
                foreach (string error in failure.Errors)
                {
                    Logger.Log("CelesteArchipelago", $"    {error}");
                    CelesteArchipelagoModule.Instance.chatHandler.HandleMessage(error, Color.DarkRed);
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    Logger.Log("CelesteArchipelago", $"    {error}");
                    CelesteArchipelagoModule.Instance.chatHandler.HandleMessage(error.ToString(), Color.DarkRed);
                }
            }

            callback(result);
        }

        private void CheckConnection()
        {
            if(!Session.Socket.Connected)
            {
                Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            connectionState = ConnectionState.UNCONNECTED;
            if (connectTask != null && !connectTask.IsCompleted) connectTask.Wait();
            if (loginTask != null && !loginTask.IsCompleted) loginTask.Wait();
            if (Session.Socket.Connected)
            {
                Logger.Log("CelesteArchipelago", "Disconnecting socket.");
                var rawSocket = DynamicData.For(Session.Socket).Get<ClientWebSocket>("webSocket");
                rawSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close requested by client", CancellationToken.None).Wait();
                DynamicData.For(Session.Socket).Invoke("OnSocketClosed");
            }
            base.Dispose(disposing);
        }

        private enum ConnectionState
        {
            DEFAULT = 0,
            UNCONNECTED = 1,
            CONNECTING = 2,
            CONNECTED = 3,
            LOGGING_IN = 4,
            LOGGED_IN = 5
        }
    }
}

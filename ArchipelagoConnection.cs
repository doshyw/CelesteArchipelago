using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class ArchipelagoConnection
    {
        public static ArchipelagoConnection Instance;

        private ArchipelagoSession session;
        public ArchipelagoSlotData slotData;
        public LoginSuccessful login;
        public VictoryCondition victoryCondition;

        public ArchipelagoConnection(Action<LoginResult> onLogin)
        {
            Instance = this;
            session = ArchipelagoSessionFactory.CreateSession($"{CelesteArchipelagoModule.Settings.Server}:{CelesteArchipelagoModule.Settings.Port}");
            session.MessageLog.OnMessageReceived += OnMessageReceived;
            AsyncConnect(onLogin);
        }

        private void AsyncConnect(Action<LoginResult> onLogin)
        {
            string uuid = null;
            if (CelesteArchipelagoModule.Settings.UUID != "")
            {
                uuid = CelesteArchipelagoModule.Settings.UUID;
            }

            Celeste.Instance.Components.Add(new ArchipelagoConnectionAttempt(
                Celeste.Instance,
                (result) => ConnectCallback(onLogin, result),
                session,
                "Celeste",
                CelesteArchipelagoModule.Settings.Name,
                ItemsHandlingFlags.AllItems,
                new Version(0, 4, 3),
                null,
                uuid,
                CelesteArchipelagoModule.Settings.Password,
                true
            ));
        }

        private void ConnectCallback(Action<LoginResult> onLogin, LoginResult result)
        {
            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                string errorMsg = $"Failed to connect to {session.Socket.Uri.Host}:{session.Socket.Uri.Port} as {CelesteArchipelagoModule.Settings.Name}";
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

                Disconnect();
                login = null;
            }
            else
            {
                login = (LoginSuccessful)result;
            }

            onLogin(result);
        }

        public void Disconnect()
        {
            if(session.Socket.Connected)
            {
                Logger.Log("CelesteArchipelago", "Disconnecting socket.");
                session.Socket.DisconnectAsync().Wait();
            }
            Instance = null;
        }

        public void Init()
        {
            slotData = new ArchipelagoSlotData(login.SlotData);
            victoryCondition = (VictoryCondition)slotData.VictoryCondition;
            session.Items.ItemReceived += AddItemCallback;
            AddItemCallback(session.Items);
        }

        private void AddItemCallback(ReceivedItemsHelper receivedItemsHelper)
        {
            while(receivedItemsHelper.Any())
            {
                Logger.Log("CelesteArchipelago", $"Received item {receivedItemsHelper.PeekItemName()} with ID {receivedItemsHelper.PeekItem().Item}");

                var itemID = receivedItemsHelper.PeekItem().Item;
                ArchipelagoNetworkItem item = new ArchipelagoNetworkItem(itemID);

                switch (item.type)
                {
                    case ItemType.CASSETTE:
                        CelesteArchipelagoSaveData.SetCassetteInGame(item.area);
                        break;
                    case ItemType.COMPLETION:
                        CelesteArchipelagoSaveData.SetCompletionInGame(item.mode, item.area);
                        break;
                    case ItemType.GEMHEART:
                        CelesteArchipelagoSaveData.SetHeartGemInGame(item.mode, item.area);
                        break;
                    case ItemType.STRAWBERRY:
                        CelesteArchipelagoSaveData.SetStrawberryInGame(item.area, item.strawberry.Value);
                        break;
                    default: break;
                }

                receivedItemsHelper.DequeueItem();
            }
        }

        public void CheckLocation(ArchipelagoNetworkItem location)
        {
            Logger.Log("CelesteArchipelago", $"Checking location {session.Locations.GetLocationNameFromId(location.ID) ?? location.ID.ToString()}");
            session.Locations.CompleteLocationChecks(location.ID);

            Logger.Log("CelesteArchipelago", $"Area: {location.area}");
            Logger.Log("CelesteArchipelago", $"Mode: {location.mode}");
            Logger.Log("CelesteArchipelago", $"VictoryCondition: {victoryCondition}");
            Logger.Log("CelesteArchipelago", $"Type: {location.type}");

            bool isVictory = location.type == ItemType.COMPLETION && ( 
                (location.area == 10 && location.mode == 0 && victoryCondition == VictoryCondition.CHAPTER_9_FAREWELL)
                || (location.area == 9 && location.mode == 0 && victoryCondition == VictoryCondition.CHAPTER_8_CORE)
                || (location.area == 7 && location.mode == 0 && victoryCondition == VictoryCondition.CHAPTER_7_SUMMIT)
            );

            if (isVictory)
            {
                OnVictory();
            }
        }

        static void OnMessageReceived(LogMessage message)
        {
            CelesteArchipelagoModule.Instance.chatHandler.HandleMessage(message);
        }

        private void OnVictory()
        {
            Logger.Log("CelesteArchipelago", "Sending Victory Condition.");
            var statusUpdatePacket = new StatusUpdatePacket();
            statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
            session.Socket.SendPacket(statusUpdatePacket);
        }

    }
}

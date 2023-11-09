using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
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

        public ArchipelagoConnection()
        {
            Instance = this;
            session = ArchipelagoSessionFactory.CreateSession($"{CelesteArchipelagoModule.Settings.Server}:{CelesteArchipelagoModule.Settings.Port}");
            session.MessageLog.OnMessageReceived += OnMessageReceived;
            login = Connect();
            if(login == null )
            {
                return;
            }
            slotData = new ArchipelagoSlotData(login.SlotData);
            AddItemCallbackHandler();
        }

        private LoginSuccessful Connect()
        {
            LoginResult result;

            try
            {
                string uuid = null;
                if (CelesteArchipelagoModule.Settings.UUID != "")
                {
                    uuid = CelesteArchipelagoModule.Settings.UUID;
                }

                result = session.TryConnectAndLogin(
                    "Celeste",
                    CelesteArchipelagoModule.Settings.Name,
                    ItemsHandlingFlags.AllItems,
                    new Version(0, 4, 3),
                    null,
                    uuid,
                    CelesteArchipelagoModule.Settings.Password,
                    true
                );
            }
            catch (Exception e)
            {
                result = new LoginFailure(e.GetBaseException().Message);
            }

            if (!result.Successful)
            {
                LoginFailure failure = (LoginFailure)result;
                Logger.Log("CelesteArchipelago", $"Failed to Connect to {session.Socket.Uri} as {CelesteArchipelagoModule.Settings.Name}");
                foreach (string error in failure.Errors)
                {
                    Logger.Log("CelesteArchipelago", $"    {error}");
                }
                foreach (ConnectionRefusedError error in failure.ErrorCodes)
                {
                    Logger.Log("CelesteArchipelago", $"    {error}");
                }

                return null;
            }

            return (LoginSuccessful)result;
        }

        private void AddItemCallbackHandler()
        {
            session.Items.ItemReceived += (receivedItemsHelper) =>
            {
                Logger.Log("CelesteArchipelago", $"Received item {receivedItemsHelper.PeekItemName()}");

                var itemID = receivedItemsHelper.PeekItem().Item;
                ArchipelagoNetworkItem item = new ArchipelagoNetworkItem(itemID);

                switch(item.type)
                {
                    case ItemType.CASSETTE:
                        CelesteArchipelagoSaveData.SetCassetteInGame(item.area);
                        break;
                    case ItemType.COMPLETION:
                        CelesteArchipelagoSaveData.SetCompletionInGame(item.area, item.mode);
                        break;
                    case ItemType.GEMHEART:
                        CelesteArchipelagoSaveData.SetHeartGemInGame(item.area, item.mode);
                        break;
                    case ItemType.STRAWBERRY:
                        CelesteArchipelagoSaveData.SetStrawberryInGame(item.area, item.strawberry.Value);
                        break;
                    default: break;
                }

                receivedItemsHelper.DequeueItem();
            };
        }

        public void CheckLocation(ArchipelagoNetworkItem location)
        {
            Logger.Log("CelesteArchipelago", $"Checking location {session.Locations.GetLocationNameFromId(location.ID) ?? location.ID.ToString()}");
            session.Locations.CompleteLocationChecks(location.ID);
        }

        static void OnMessageReceived(LogMessage message)
        {
            CelesteArchipelagoModule.Instance.chatHandler.HandleMessage(message);
        }

    }
}

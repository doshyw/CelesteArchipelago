using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
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
            login = Connect();
            if(login == null )
            {
                return;
            }
            slotData = new ArchipelagoSlotData(login.SlotData);
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
                Logger.Log("CelesteArchipelago", $"Failed to Connect to {session.Socket.Uri} as {CelesteArchipelagoModule.Settings.Name}:");
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
                var itemReceivedName = receivedItemsHelper.PeekItemName() ?? $"Item: {receivedItemsHelper.Index}";

                // Implement handler code here

                receivedItemsHelper.DequeueItem();
            };
        }

    }
}

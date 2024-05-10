using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Celeste.Mod.CelesteArchipelago
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
        public PlayState PlayState
        {
            get
            {
                return new PlayState(Session.DataStorage[Scope.Slot, "CelestePlayState"]);
            }
            set
            {
                Session.DataStorage[Scope.Slot, "CelestePlayState"] = value.ToString();
            }
        
        }

        private CheckpointState _checkpointState;
        public CheckpointState CheckpointState
        {
            get
            {
                if(_checkpointState == null)
                {
                    _checkpointState = new CheckpointState(unchecked((ulong)(Session.DataStorage[Scope.Slot, "CelesteCheckpointState"].To<long>() - long.MinValue)), Session.DataStorage);
                }
                return _checkpointState;
            }
        }
        public bool BlockMessages { get; set; } = false;
        public bool IsConnected
        {
            get
            {
                return Connection != null && Connection.IsConnected;
            }
        }

        private ChatHandler ChatHandler { get; set; }
        private Connection Connection { get; set; }
        private VictoryConditionOptions VictoryCondition
        {
            get { return (VictoryConditionOptions)SlotData.VictoryCondition; }
        }
        private List<IPatchable> patchObjects = new List<IPatchable>
        {
            new PatchedCassette(),
            new PatchedHeartGem(),
            new PatchedHeartGemDoor(),
            new PatchedLevel(),
            new PatchedLevelSetStats(),
            new PatchedOuiChapterPanel(),
            new PatchedOuiChapterSelect(),
            new PatchedOuiMainMenu(),
            new PatchedOuiJournal(),
            new PatchedSaveData(),
            new PatchedStrawberry(),
        };

        public ArchipelagoController(Game game) : base(game)
        {
            UpdateOrder = 9000;
            DrawOrder = 9000;
            Enabled = false;
            Instance = this;
            game.Components.Add(this);
            ChatHandler = new ChatHandler(Game);
            game.Components.Add(ChatHandler);
            ProgressionSystem = new NullProgression();
        }

        public void Init()
        {
            Enabled = true;
            ChatHandler.Init();
            //for (int i = 0; i < 10; i++)
            //{
            //    ChatHandler.HandleMessage("Hello, my name is tester testington, and I am a message!", Color.White);
            //}
        }

        public override void Update(GameTime gameTime)
        {

        }

        public void LoadPatches()
        {
            foreach (var patch in patchObjects)
            {
                patch.Load();
            }
        }

        public void UnloadPatches()
        {
            foreach (var patch in patchObjects)
            {
                patch.Unload();
            }
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

            Connection = new Connection(Celeste.Instance, parameters, (loginResult) => {
                if(loginResult.Successful)
                {
                    Session.Items.ItemReceived += ReceiveItemCallback;
                    if (SlotData.ProgressionSystem == (int)ProgressionSystemOptions.DEFAULT_PROGRESSION)
                    {
                        ProgressionSystem = new DefaultProgression(SlotData);
                    }
                    Session.DataStorage[Scope.Slot, "CelestePlayState"].Initialize("1;0;0;dotutorial");
                    Session.DataStorage[Scope.Slot, "CelesteCheckpointState"].Initialize(long.MinValue);

                    Connection.Disposed += (sender, args) =>
                    {
                        Session.MessageLog.OnMessageReceived -= HandleMessage;
                        Session.Items.ItemReceived -= ReceiveItemCallback;
                        ProgressionSystem = new NullProgression();
                    };
                }
                else
                {
                    Connection?.Dispose();
                }
                onLogin(loginResult);
            });
        }

        public void DisconnectSession()
        {
            Connection?.Dispose();
        }

        public void ReceiveItemCallback(ReceivedItemsHelper receivedItemsHelper)
        {
            while(receivedItemsHelper.Any())
            {
                // Receive latest uncollected item
                Logger.Log("CelesteArchipelago", $"Received item {receivedItemsHelper.PeekItemName()} with ID {receivedItemsHelper.PeekItem().Item}");
                var itemID = receivedItemsHelper.PeekItem().Item;
                ArchipelagoNetworkItem item = new ArchipelagoNetworkItem(itemID);

                // Collect received item via chosen progression system
                ProgressionSystem.OnCollectedServer(item.areaKey, item.type, item.strawberry);
                receivedItemsHelper.DequeueItem();
            }
        }

        public void SendLocationCallback(ArchipelagoNetworkItem location)
        {
            Logger.Log("CelesteArchipelago", $"Checking location {Session.Locations.GetLocationNameFromId(location.ID) ?? location.ID.ToString()}");
            Session.Locations.CompleteLocationChecks(location.ID);

            bool isVictory = location.type == CollectableType.COMPLETION && (
                location.area == 10 && location.mode == 0 && VictoryCondition == VictoryConditionOptions.CHAPTER_9_FAREWELL
                || location.area == 9 && location.mode == 0 && VictoryCondition == VictoryConditionOptions.CHAPTER_8_CORE
                || location.area == 7 && location.mode == 0 && VictoryCondition == VictoryConditionOptions.CHAPTER_7_SUMMIT
            );

            if (isVictory)
            {
                Logger.Log("CelesteArchipelago", "Sending Victory Condition.");
                var statusUpdatePacket = new StatusUpdatePacket();
                statusUpdatePacket.Status = ArchipelagoClientState.ClientGoal;
                Session.Socket.SendPacket(statusUpdatePacket);
            }
        }

        public void ReplayClientCollected()
        {
            ArchipelagoNetworkItem item;
            foreach (var loc in Session.Locations.AllLocationsChecked)
            {
                Logger.Log("CelesteArchipelago", $"Replaying location {Session.Locations.GetLocationNameFromId(loc) ?? loc.ToString()}");
                item = new ArchipelagoNetworkItem(loc);
                ProgressionSystem.OnCollectedClient(item.areaKey, item.type, item.strawberry, true);
            }
        }

        public void HandleMessage(LogMessage message)
        {
            if (!BlockMessages) ChatHandler.HandleMessage(message);
        }

        public void HandleMessage(string text, Color color)
        {
            if (!BlockMessages) ChatHandler.HandleMessage(text, color);
        }

        protected override void Dispose(bool disposing)
        {
            Connection?.Dispose();
            ChatHandler?.Dispose();
            base.Dispose(disposing);
        }

    }
}

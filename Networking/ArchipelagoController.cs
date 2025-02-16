﻿using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Packets;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Parts;

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

        public DeathLinkService DeathLinkService { get; private set; }
        public DeathLinkStatus DeathLinkStatus { get; set; } = DeathLinkStatus.None;
        public bool isLocalDeath = true;
        private long DeathAmnestyCount = 0;
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
            new PatchedPlayer(),
            new PatchedStrawberry(),
            new PatchedBerryCounter(),
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
        }

        public void DeInit()
        {
            ChatHandler.DeInit();
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
                version:  new Version(0, 5, 1), // Needs hotfix aswell
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
                    Session.DataStorage[Scope.Slot, "CelesteDeathAmnestyState"].Initialize(0);

                    CelesteArchipelagoModule.Settings.DeathLink = SlotData.DeathLink == 1;
                    DeathLinkService = Session.CreateDeathLinkService();
                    DeathLinkService.OnDeathLinkReceived += ReceiveDeathLinkCallback;

                    DeathAmnestyCount = Session.DataStorage["CelesteDeathAmnestyState"];

                    if (CelesteArchipelagoModule.Settings.DeathLink)
                    {
                        DeathLinkService.EnableDeathLink();
                    }
                    else
                    {
                        DeathLinkService.DisableDeathLink();
                    }

                    Connection.Disposed += (sender, args) =>
                    {
                        Session.MessageLog.OnMessageReceived -= HandleMessage;
                        Session.Items.ItemReceived -= ReceiveItemCallback;
                        ProgressionSystem = new NullProgression();
                        DeathLinkService = null;
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

        public void ReceiveItemCallback(IReceivedItemsHelper receivedItemsHelper)
        {
            while(receivedItemsHelper.Any())
            {
                // Receive latest uncollected item
                Logger.Log("CelesteArchipelago", $"Received item {receivedItemsHelper.PeekItem().ItemName} with ID {receivedItemsHelper.PeekItem().ItemId}");
                if (receivedItemsHelper.PeekItem().ItemName == "Victory (Celeste)")
                {
                    receivedItemsHelper.DequeueItem();
                    continue;
                }
                var itemID = receivedItemsHelper.PeekItem().ItemId;
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

            var goalLevel = ProgressionSystem.GetGoalLevel();
            bool isVictory = location.type == CollectableType.COMPLETION
                && location.area == goalLevel.ID
                && location.mode == (int)goalLevel.Mode;

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

        public void ReceiveDeathLinkCallback(DeathLink deathLink)
        {
            string completeMessage;
            if (string.IsNullOrEmpty(deathLink.Cause))
            {
                completeMessage = $"DeathLink: {deathLink.Source} died";
            }
            else
            {
                completeMessage = $"DeathLink from {deathLink.Source}: {deathLink.Cause}";
            }

            ChatHandler.HandleMessage(completeMessage, Color.PaleVioletRed);

            if (DeathLinkStatus == DeathLinkStatus.None && CelesteArchipelagoModule.Settings.DeathLink)
            {
                // wait for Madeline to die
                DeathLinkStatus = DeathLinkStatus.Pending;
                isLocalDeath = false;
            }
        }

        private string setDeathCause(string chapter, string player)
        {
            switch (chapter)
            {
                case "Celeste/0-Intro": // Prologue
                    return $"Granny Laughs at {player}"; // "Died to a driveway"
                case "Celeste/1-ForsakenCity":
                    return $"{player} died in the Forsaken City";
                case "Celeste/2-OldSite":
                    return $"{player} kept dreaming";
                case "Celeste/3-CelestialResort":
                    return $"{player} couldn't handle the dust bunnies"; // or Oshiro reference
                case "Celeste/4-GoldenRidge":
                    return $"{player} was taken away by the wind"; // or snowball reference
                case "Celeste/5-MirrorTemple":
                    return $"{player} got lost in the temple"; // or seeker reference
                case "Celeste/6-Reflection":
                    return $"{player} tried to get rid of a part of themself"; // "You think a feather can beat me?" or "Kevin had something to say"
                case "Celeste/7-Summit":
                    return $"{player} fell on the climb to the top of celeste mountain";
                case "Celeste/8-Epilogue":
                    return $"{player}'s strawberry cake was a lie";
                case "Celeste/9-Core":
                    return $"{player} couldn't reach the core";
                case "Celeste/LostLevels": // Farewell
                    return $"{player} says Farewell";
                default:
                    Logger.Log(LogLevel.Debug, "CelesteArchipelago", $"Could not find cause {chapter}");
                    return $"{player} died in Celeste";
            }

            // Could implement many messages per chapter
            // Could get specific item player died to
            // Feel free to change messages if you believe that your message is better
        }

        public void SendDeathLinkCallback()
        {
            if (!CelesteArchipelagoModule.Settings.DeathLink)
            {
                return;
            }

            if (DeathLinkStatus == DeathLinkStatus.None && DeathAmnestyCount >= Instance.SlotData.DeathAmnestyMax - 1)
            {
                ChatHandler.HandleMessage("Death Sent", Color.PaleVioletRed);
                string sourcePlayer = Session.Players.GetPlayerAlias(Session.ConnectionInfo.Slot);
                DeathLink deathLink = new DeathLink(sourcePlayer, setDeathCause(PlayState.AreaKey.GetSID(), sourcePlayer));
                DeathLinkService.SendDeathLink(deathLink);

                DeathAmnestyCount = 0;
            }
            else if (isLocalDeath)
            {
                DeathAmnestyCount++;
            }

            Session.DataStorage["CelesteDeathAmnestyState"] = DeathAmnestyCount;
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
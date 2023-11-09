using System;
using System.Collections;
using Microsoft.Xna.Framework;
using Archipelago.MultiClient.Net;
using Monocle;
using Celeste.Mod.CelesteArchipelago.PatchedObjects;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.CelesteArchipelago {
    public class CelesteArchipelagoModule : EverestModule {
        public static CelesteArchipelagoModule Instance { get; private set; }

        public override Type SettingsType => typeof(CelesteArchipelagoModuleSettings);
        public static CelesteArchipelagoModuleSettings Settings => (CelesteArchipelagoModuleSettings) Instance._Settings;

        // If you need to store save data:
        public override Type SaveDataType => typeof(CelesteArchipelagoSaveData);
        public static CelesteArchipelagoSaveData SaveData => (CelesteArchipelagoSaveData)Instance._SaveData;

        public override Type SessionType => typeof(CelesteArchipelagoModuleSession);
        public static CelesteArchipelagoModuleSession Session => (CelesteArchipelagoModuleSession) Instance._Session;

        public ChatHandler chatHandler;

        public CelesteArchipelagoModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(CelesteArchipelagoModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(CelesteArchipelagoModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            // TODO: apply any hooks that should always be active
            PatchedCassette.Load();
            PatchedHeartGem.Load();
            PatchedLevelSetStats.Load();
            PatchedOuiChapterPanel.Load();
            PatchedOuiChapterSelect.Load();
            PatchedOuiJournal.Load();
            PatchedSaveData.Load();
            PatchedStrawberry.Load();

            Everest.Events.MainMenu.OnCreateButtons += ArchipelagoUI.ReplaceClimbButton;
        }

        public override void Initialize()
        {
            
            // Logger.Log("CelesteArchipelago", AreaData.Areas.ToString());
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
            PatchedCassette.Unload();
            PatchedHeartGem.Unload();
            PatchedLevelSetStats.Unload();
            PatchedOuiChapterPanel.Unload();
            PatchedOuiChapterSelect.Unload();
            PatchedOuiJournal.Unload();
            PatchedSaveData.Unload();
            PatchedStrawberry.Unload();

            Everest.Events.MainMenu.OnCreateButtons -= ArchipelagoUI.ReplaceClimbButton;
        }

    }
}
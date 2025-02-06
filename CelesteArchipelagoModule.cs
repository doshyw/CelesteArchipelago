using System;

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

        public CelesteArchipelagoModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel("CelesteArchipelago", LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel("CelesteArchipelago", LogLevel.Info);
#endif
        }

        public override void Load() {
            new ArchipelagoController(Celeste.Instance);
            ArchipelagoController.Instance.LoadPatches();

            Everest.Events.MainMenu.OnCreateButtons += ArchipelagoUI.ReplaceClimbButton;
        }

        public override void Unload() {
            ArchipelagoController.Instance.UnloadPatches();
            ArchipelagoController.Instance.Dispose();

            Everest.Events.MainMenu.OnCreateButtons -= ArchipelagoUI.ReplaceClimbButton;
        }

    }
}
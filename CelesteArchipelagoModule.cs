using System;
using System.Collections;
using Microsoft.Xna.Framework;

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
            Logger.SetLogLevel(nameof(CelesteArchipelagoModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(CelesteArchipelagoModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            // TODO: apply any hooks that should always be active
            On.Celeste.Strawberry.OnCollect += PatchedStrawberry.OnCollect;
            On.Celeste.Strawberry.CollectRoutine += PatchedStrawberry.CollectRoutine;
            On.Celeste.SaveData.CheckStrawberry_AreaKey_EntityID += PatchedSaveData.CheckStrawberry_AreaKey_EntityID;
            On.Celeste.SaveData.CheckStrawberry_EntityID += PatchedSaveData.CheckStrawberry_EntityID;
        }

        public override void Unload() {
            // TODO: unapply any hooks applied in Load()
            On.Celeste.Strawberry.OnCollect -= PatchedStrawberry.OnCollect;
            On.Celeste.Strawberry.CollectRoutine -= PatchedStrawberry.CollectRoutine;
            On.Celeste.SaveData.CheckStrawberry_AreaKey_EntityID -= PatchedSaveData.CheckStrawberry_AreaKey_EntityID;
            On.Celeste.SaveData.CheckStrawberry_EntityID -= PatchedSaveData.CheckStrawberry_EntityID;
        }
    }
}
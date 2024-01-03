﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedLevelSetStats : IPatchable
    {
        private static IDetour hook_LevelSetStats_get_UnlockedModes;

        public void Load()
        {
            hook_LevelSetStats_get_UnlockedModes = new Hook(
                typeof(LevelSetStats).GetProperty("UnlockedModes").GetGetMethod(),
                typeof(PatchedLevelSetStats).GetMethod("Get_UnlockedModes", BindingFlags.NonPublic | BindingFlags.Static)
            );
        }

        public void Unload()
        {
            hook_LevelSetStats_get_UnlockedModes.Dispose();
        }

        private delegate int orig_LevelSetStats_get_UnlockedModes(LevelSetStats self);
        private static int Get_UnlockedModes(orig_LevelSetStats_get_UnlockedModes orig, LevelSetStats self)
        {
            return 3;
        }
    }
}

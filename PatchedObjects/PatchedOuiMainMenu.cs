using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Utils;
using System.Collections.Generic;

namespace Celeste.Mod.CelesteArchipelago.PatchedObjects
{
    public static class PatchedOuiMainMenu
    {

        internal static void Load()
        {
            On.Celeste.OuiMainMenu.Enter += Enter;
        }

        internal static void Unload()
        {
            On.Celeste.OuiMainMenu.Enter -= Enter;
        }

        private static IEnumerator Enter(On.Celeste.OuiMainMenu.orig_Enter orig, OuiMainMenu self, Oui from)
        {
            if(from is OuiChapterSelect)
            {
                if(ArchipelagoConnection.Instance != null)
                {
                    ArchipelagoConnection.Instance.Disconnect();
                }
                if(CelesteArchipelagoModule.Instance.chatHandler != null)
                {
                    Celeste.Instance.Components.Remove(CelesteArchipelagoModule.Instance.chatHandler);
                    CelesteArchipelagoModule.Instance.chatHandler.DeInit();
                }
            }

            yield return new SwapImmediately(orig(self, from));
        }

    }
}

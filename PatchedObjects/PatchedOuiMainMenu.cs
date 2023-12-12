using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Utils;
using System.Collections.Generic;
using Celeste.Mod.CelesteArchipelago.Networking;

namespace Celeste.Mod.CelesteArchipelago.PatchedObjects
{
    public class PatchedOuiMainMenu : IPatchable
    {

        public void Load()
        {
            On.Celeste.OuiMainMenu.Enter += Enter;
        }

        public void Unload()
        {
            On.Celeste.OuiMainMenu.Enter -= Enter;
        }

        private static IEnumerator Enter(On.Celeste.OuiMainMenu.orig_Enter orig, OuiMainMenu self, Oui from)
        {
            if (from is OuiChapterSelect)
            {
                if (ArchipelagoConnection.Instance != null)
                {
                    ArchipelagoConnection.Instance.Disconnect();
                }
                ArchipelagoController.Instance.ChatHandler
                if (CelesteArchipelagoModule.Instance.chatHandler != null)
                {
                    Celeste.Instance.Components.Remove(CelesteArchipelagoModule.Instance.chatHandler);
                    CelesteArchipelagoModule.Instance.chatHandler.DeInit();
                }
            }

            yield return new SwapImmediately(orig(self, from));
        }

    }
}

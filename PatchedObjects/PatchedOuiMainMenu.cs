using Microsoft.Xna.Framework;
using System.Collections;

namespace Celeste.Mod.CelesteArchipelago
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
            if (ArchipelagoController.Instance != null)
            {
                if (!ArchipelagoController.Instance.Enabled)
                {
                    ArchipelagoController.Instance.Init();
                }

                if (ArchipelagoController.Instance.IsConnected)
                {
                    ArchipelagoController.Instance.DisconnectSession();
                }
            }

            yield return new SwapImmediately(orig(self, from));
        }

    }
}

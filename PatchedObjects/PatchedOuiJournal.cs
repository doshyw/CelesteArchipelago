using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedOuiJournal : IPatchable
    {
        public void Load()
        {
            On.Celeste.OuiJournal.Enter += EnterPatch;
        }

        public void Unload()
        {
            On.Celeste.OuiJournal.Enter -= EnterPatch;
        }

        private static IEnumerator EnterPatch(On.Celeste.OuiJournal.orig_Enter orig, OuiJournal self, Oui from)
        {
            yield return new SwapImmediately(orig(self, from));

            var page = self.Pages.Find((page) => page is OuiJournalProgress);
            var index = self.Pages.IndexOf(page);
            self.Pages.RemoveAt(index);
            self.Pages.Insert(index, new ReplacementOuiJournalProgress(self));
        }

    }
}

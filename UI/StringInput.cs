using Celeste.Mod.UI;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CelesteArchipelago
{
    public class StringInput : TextMenu.Button
    {
		public StringInput(PropertyInfo setting, TextMenu menu) : base($"{setting.Name}: {setting.GetValue(CelesteArchipelagoModule.Settings)}")
		{
            var value = setting.GetValue(CelesteArchipelagoModule.Settings);
            var maxLen = 64;
            var minLen = 0;

			Pressed(() =>
            {
                Audio.Play(SFX.ui_main_savefile_rename_start);
                menu.SceneAs<Overworld>().Goto<SavingStringEditor>().Init<OuiArchipelago>(
                    (string)value,
                    v => setting.SetValue(CelesteArchipelagoModule.Settings, v),
                    maxLen,
                    minLen);
            });
		}
    }

    public class SavingStringEditor : OuiModOptionString
    {
        public override IEnumerator Leave(Oui next)
        {
            yield return base.Leave(next);
            yield return Everest.SaveSettings();
        }
    }
}

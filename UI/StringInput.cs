using Celeste.Mod.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class StringInput : TextMenu.Button
    {
		public StringInput(PropertyInfo setting, TextMenu menu) : base($"{setting.Name}: {setting.GetValue(CelesteArchipelagoModule.Settings)}")
		{
            var value = setting.GetValue(CelesteArchipelagoModule.Settings);

			this.Pressed(delegate
            {
                Audio.Play("event:/ui/main/savefile_rename_start");
                menu.SceneAs<Overworld>().Goto<OuiModOptionString>().Init<OuiArchipelago>((string)value, delegate (string v)
                {
                   setting.SetValue(CelesteArchipelagoModule.Settings, v);
                }, 64, 0);
            });
		}
    }
}

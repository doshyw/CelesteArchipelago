using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Xml.Serialization;

namespace Celeste.Mod.CelesteArchipelago {
    public class CelesteArchipelagoModuleSettings : EverestModuleSettings
    {
        public string Name { get; set; } = "Madeline";
        public string Password { get; set; } = "";
        public string Server { get; set; } = "";
        public string Port { get; set; } = "38281";

        [DefaultButtonBinding(Buttons.BigButton, Keys.T)]
        public ButtonBinding ToggleChat { get; set; }
        [DefaultButtonBinding(Buttons.DPadUp, Keys.Q)]
        public ButtonBinding ScrollChatUp { get; set; }
        [DefaultButtonBinding(Buttons.DPadDown, Keys.Z)]
        public ButtonBinding ScrollChatDown { get; set; }

    }
}

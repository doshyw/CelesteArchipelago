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

        [DefaultButtonBinding(Buttons.Back, Keys.T)]
        public ButtonBinding ToggleChat { get; set; }
        [DefaultButtonBinding(Buttons.RightThumbstickUp, Keys.Q)]
        public ButtonBinding ScrollChatUp { get; set; }
        [DefaultButtonBinding(Buttons.RightThumbstickDown, Keys.Z)]
        public ButtonBinding ScrollChatDown { get; set; }
    }
}

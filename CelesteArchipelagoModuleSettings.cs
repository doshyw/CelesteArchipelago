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
        public string UUID { get; set; } = "";

        #region Bindings
        [SettingSubHeader("modoptions_celestenetclient_subheading_other")]
        [DefaultButtonBinding(0, Keys.T)]
        public ButtonBinding ButtonChat { get; set; }
        [SettingSubHeader("modoptions_celestenetclient_binds_chat")]
        [DefaultButtonBinding(0, Keys.Enter)]
        public ButtonBinding ButtonChatSend { get; set; }

        [DefaultButtonBinding(0, Keys.Escape)]
        public ButtonBinding ButtonChatClose { get; set; }

        [DefaultButtonBinding(Buttons.LeftThumbstickUp, Keys.PageUp)]
        public ButtonBinding ButtonChatScrollUp { get; set; }

        [DefaultButtonBinding(Buttons.LeftThumbstickDown, Keys.PageDown)]
        public ButtonBinding ButtonChatScrollDown { get; set; }
        #endregion

    }
}

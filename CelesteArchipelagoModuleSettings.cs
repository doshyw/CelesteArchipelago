using Microsoft.Xna.Framework.Input;
using Monocle;
using System.Xml.Serialization;

namespace Celeste.Mod.CelesteArchipelago {
    public class CelesteArchipelagoModuleSettings : EverestModuleSettings
    {
        [SettingMaxLength(30)]
        public string Name { get; set; } = "Madeline";
        public string Password { get; set; } = "";
        [SettingMaxLength(30)]
        public string Server { get; set; } = "archipelago.gg";
        public string Port { get; set; } = "38281";
        [SettingInGame(true)]
        public bool DeathLink
        {
            get => _deathLink;
            set
            {
                
                if (ArchipelagoController.Instance.DeathLinkService is not null)
                {
                    if (!_deathLink) {
                        ArchipelagoController.Instance.DeathLinkService.EnableDeathLink();
                    }
                    else {
                        ArchipelagoController.Instance.DeathLinkService.DisableDeathLink();
                    }
                }
                _deathLink = value;
            }
        }
        private bool _deathLink = false;
        private bool _Chat = false;
        public bool Chat { 
            get => _Chat; 
            set {
                _Chat = value;
                if (value) {
                    ArchipelagoController.Instance.Init();
                } else {
                    ArchipelagoController.Instance.DeInit();
                }
            } 
        }


        [DefaultButtonBinding(Buttons.Back, Keys.T)]
        public ButtonBinding ToggleChat { get; set; }
        [DefaultButtonBinding(Buttons.RightThumbstickUp, Keys.Q)]
        public ButtonBinding ScrollChatUp { get; set; }
        [DefaultButtonBinding(Buttons.RightThumbstickDown, Keys.Z)]
        public ButtonBinding ScrollChatDown { get; set; }
    }
}
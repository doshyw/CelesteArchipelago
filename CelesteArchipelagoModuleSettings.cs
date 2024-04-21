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

        public bool DeathLink
        {
            get => _deathLink;
            set
            {
                if (ArchipelagoController.Instance is not null &&
                    ArchipelagoController.Instance.DeathLinkService is not null)
                {
                    switch (value)
                    {
                        case true when !_deathLink:
                            ArchipelagoController.Instance.DeathLinkService.EnableDeathLink();
                            break;
                        case false when _deathLink:
                            ArchipelagoController.Instance.DeathLinkService.DisableDeathLink();
                            break;
                    }
                }
                _deathLink = value;
            }
        }

        private bool _deathLink = false;
    }
}

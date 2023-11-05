using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    class MainMenuArchipelagoButton : MainMenuClimb
    {
        public MainMenuArchipelagoButton(Oui oui, Vector2 targetPosition, Vector2 tweenFrom, Action onConfirm)
            : base(oui, targetPosition, tweenFrom, onConfirm)
        {
            DynamicData Button = DynamicData.For(this);
            Button.Set("label", Dialog.Clean("archipelago_menu_begin"));
            Button.Set("icon", GFX.Gui["archipelago/menu/start"]);
        }
    }
}
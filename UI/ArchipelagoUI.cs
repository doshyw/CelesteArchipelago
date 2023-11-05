using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class ArchipelagoUI
    {
        private static void OnBegin(OuiMainMenu menu)
        {
            Logger.Log("CelesteArchipelago", "Entering ArchipelagoUI.OnBegin");
            Audio.Play("event:/ui/main/whoosh_list_out");
            Audio.Play("event:/ui/main/button_climb");
            menu.Overworld.Goto<OuiArchipelago>();
        }

        public static void ReplaceClimbButton(OuiMainMenu menu, List<MenuButton> buttons)
        {
            Logger.Log("CelesteArchipelago", "Entering ArchipelagoUI.ReplaceClimbButton");

            var Menu = DynamicData.For(menu);

            // Only replace the original "Start" button if it hasn't already been replaced.
            var originalClimbButton = Menu.Get<MainMenuClimb>("climbButton");
            if(originalClimbButton.GetType() != typeof(MainMenuArchipelagoButton))
            {
                // Get the new "Start" button.
                var newClimbButton = new MainMenuArchipelagoButton(menu, originalClimbButton.TargetPosition, originalClimbButton.TweenFrom, () => OnBegin(menu));

                // Replace the old button with the new one in OuiMainMenu.buttons.
                int index = buttons.IndexOf(originalClimbButton);
                buttons.RemoveAt(index);
                buttons.Insert(index, newClimbButton);

                // Relink the "Up" and "Down" actions for the list.
                if (index > 0)
                {
                    buttons[index - 1].UpButton = newClimbButton;
                    newClimbButton.DownButton = buttons[index - 1];
                }
                if (index < buttons.Count - 1)
                {
                    buttons[index + 1].DownButton = newClimbButton;
                    newClimbButton.UpButton = buttons[index + 1];
                }

                // Replace the old button with the new one in OuiMainMenu.climbButton.
                Menu.Set("climbButton", newClimbButton);

                // Make the new button start selected by default.
                if (!Menu.Get<bool>("startOnOptions"))
                {
                    newClimbButton.StartSelected();
                }

                // Destroy the old climb button
                originalClimbButton._Selected = false;
                originalClimbButton.RemoveSelf();
            }

            Logger.Log("CelesteArchipelago", "Leaving ArchipelagoUI.ReplaceClimbButton");
        }

    }
}

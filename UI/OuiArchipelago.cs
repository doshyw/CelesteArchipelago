using Celeste.Mod.CelesteArchipelago.UI;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class OuiArchipelago : Oui
    {

        public static OuiArchipelago Instance;

        private TextMenu menu;

        private const float onScreenX = 960f;

        private const float offScreenX = 2880f;

        private float alpha;

        private int savedMenuIndex = -1;

        public OuiArchipelago()
        {
            Instance = this;
        }

        public static TextMenu CreateMenu(bool inGame, FMOD.Studio.EventInstance snapshot, Oui entryOui)
        {
            var textMenu = new TextMenuWithRenderCallback();
            textMenu.CompactWidthMode = true;
            textMenu.BatchMode = true;
            textMenu.Add(new TextMenuExt.HeaderImage("archipelago/menu/start")
            {
                ImageColor = Color.White,
                ImageOutline = true,
                ImageScale = 0.5f
            });

            var settingsType = typeof(CelesteArchipelagoModuleSettings);

            textMenu.Add(new StringInput(settingsType.GetProperty("Name"), textMenu));
            textMenu.Add(new StringInput(settingsType.GetProperty("Server"), textMenu));
            textMenu.Add(new StringInput(settingsType.GetProperty("Port"), textMenu));
            textMenu.Add(new StringInput(settingsType.GetProperty("Password"), textMenu));
            textMenu.Add(new EmptyPadding(24));
            textMenu.Add(new PassiveDisplay(settingsType.GetProperty("UUID"), false));
            textMenu.Add(new UUIDRegenerator(textMenu));
            textMenu.Add(new EmptyPadding(48));
            textMenu.Add(new ArchipelagoStartButton("Connect to Session", entryOui));

            if (textMenu.Height > textMenu.ScrollableMinSize)
            {
                textMenu.Position.Y = textMenu.ScrollTargetY;
            }
            textMenu.BatchMode = false;
            return textMenu;
        }

        private void ReloadMenu()
        {
            Vector2 position = Vector2.Zero;
            int num = -1;
            if (menu != null)
            {
                position = menu.Position;
                num = menu.Selection;
                Scene.Remove(menu);
            }
            menu = CreateMenu(inGame: false, null, this);
            if (num >= 0)
            {
                menu.Selection = num;
                menu.Position = position;
            }
            Scene.Add(menu);
        }

        public void SetFocusedMenu(bool focused)
        {
            menu.Focused = focused;
        }

        public override IEnumerator Enter(Oui from)
        {
            ReloadMenu();
            //if (savedMenuIndex != -1 && typeof(ISubmenu).IsAssignableFrom(from.GetType()))
            //{
            //    menu.Selection = Math.Min(savedMenuIndex, menu.LastPossibleSelection);
            //    menu.Position.Y = menu.ScrollTargetY;
            //}
            TextMenu textMenu = menu;
            OuiArchipelago ouiArchipelago = this;
            bool visible = true;
            ouiArchipelago.Visible = true;
            textMenu.Visible = visible;
            menu.Focused = false;
            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f)
            {
                menu.X = 2880f + -1920f * Ease.CubeOut(p);
                alpha = Ease.CubeOut(p);
                yield return null;
            }
            menu.Focused = true;
        }

        public override IEnumerator Leave(Oui next)
        {
            Audio.Play("event:/ui/main/whoosh_large_out");
            menu.Focused = false;
            savedMenuIndex = menu.Selection;
            yield return Everest.SaveSettings();
            for (float p = 0f; p < 1f; p += Engine.DeltaTime * 4f)
            {
                menu.X = 960f + 1920f * Ease.CubeIn(p);
                alpha = 1f - Ease.CubeIn(p);
                yield return null;
            }
            TextMenu textMenu = menu;
            OuiArchipelago ouiArchipelago = this;
            bool visible = false;
            ouiArchipelago.Visible = false;
            textMenu.Visible = visible;
            menu.RemoveSelf();
            menu = null;
        }

        public override void Update()
        {
            if (menu != null && menu.Focused && base.Selected && Input.MenuCancel.Pressed)
            {
                Audio.Play("event:/ui/main/button_back");
                base.Overworld.Goto<OuiMainMenu>();
            }
            base.Update();
        }

        public override void Render()
        {
            if (alpha > 0f)
            {
                Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * alpha * 0.4f);
            }
            base.Render();
        }
    }
}


using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago.UI
{
    internal class UUIDRegenerator : TextMenu.Button
    {
        private bool deleting;
        private int deleteIndex;
        private float deletingEase;
        private float inputDelay;
        private Wiggler wiggler;
        private TextMenu menu;

        public UUIDRegenerator(TextMenuWithRenderCallback menu) : base("Regenerate UUID")
        {
            var value = CelesteArchipelagoModule.Settings.UUID;
            deleting = false;
            this.menu = menu;

            menu.Add(wiggler = Wiggler.Create(0.4f, 4f));

            this.Pressed(delegate
            {
                Audio.Play("event:/ui/main/savefile_rename_start");
                deleteIndex = 1;
                inputDelay = 0.1f;
                deleting = true;
                menu.Focused = false;
                wiggler.Start();
            });

            menu.AddRender(delegate
            {
                float num2 = wiggler.Value * 8f;
                if (deletingEase > 0f)
                {
                    float num12 = Ease.CubeOut(deletingEase);
                    Vector2 vector6 = new Vector2(960f, 540f);
                    float lineHeight2 = ActiveFont.LineHeight;
                    Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * num12 * 0.9f);
                    ActiveFont.Draw(Dialog.Clean("archipelago_menu_regenuuid_really"), vector6 + new Vector2(0f, -16f - 64f * (1f - num12)), new Vector2(0.5f, 1f), Vector2.One, Color.White * num12);
                    ActiveFont.DrawOutline(Dialog.Clean("archipelago_menu_regenuuid_yes"), vector6 + new Vector2(((deleting && deleteIndex == 0) ? num2 : 0f) * 1.2f * num12, 16f + 64f * (1f - num12)), new Vector2(0.5f, 0f), Vector2.One * 0.8f, deleting ? SelectionColor(deleteIndex == 0) : Color.Gray, 2f, Color.Black * num12);
                    ActiveFont.DrawOutline(Dialog.Clean("archipelago_menu_regenuuid_no"), vector6 + new Vector2(((deleting && deleteIndex == 1) ? num2 : 0f) * 1.2f * num12, 16f + lineHeight2 + 64f * (1f - num12)), new Vector2(0.5f, 0f), Vector2.One * 0.8f, deleting ? SelectionColor(deleteIndex == 1) : Color.Gray, 2f, Color.Black * num12);
                }
            });
        }

        private void RegenerateUUIDSetting()
        {
            CelesteArchipelagoModule.Settings.UUID = Guid.NewGuid().ToString().ToUpper();
        }

        public Color SelectionColor(bool selected)
        {
            if (selected)
            {
                if (!Settings.Instance.DisableFlashes && !menu.Scene.BetweenInterval(0.1f))
                {
                    return TextMenu.HighlightColorB;
                }
                return TextMenu.HighlightColorA;
            }
            return Color.White;
        }

        public override void Update()
        {
            inputDelay -= Engine.DeltaTime;
            if (inputDelay > 0f)
            {
                return;
            }

            if (deleting)
            {
                if (Input.MenuCancel.Pressed)
                {
                    deleting = false;
                    wiggler.Start();
                    Audio.Play("event:/ui/main/button_back");
                    menu.Focused = true;
                }
                else if (Input.MenuUp.Pressed && deleteIndex > 0)
                {
                    deleteIndex = 0;
                    wiggler.Start();
                    Audio.Play("event:/ui/main/rollover_up");
                }
                else if (Input.MenuDown.Pressed && deleteIndex < 1)
                {
                    deleteIndex = 1;
                    wiggler.Start();
                    Audio.Play("event:/ui/main/rollover_down");
                }
                else if (Input.MenuConfirm.Pressed)
                {
                    if (deleteIndex == 1)
                    {
                        deleting = false;
                        wiggler.Start();
                        Audio.Play("event:/ui/main/button_back");
                    }
                    else
                    {
                        RegenerateUUIDSetting();
                        deleting = false;
                        deletingEase = 0f;
                        Audio.Play("event:/ui/main/savefile_delete");
                    }
                    menu.Focused = true;
                }
            }
            deletingEase = Calc.Approach(deletingEase, deleting ? 1f : 0f, Engine.DeltaTime * 4f);
            base.Update();
        }
    }
}

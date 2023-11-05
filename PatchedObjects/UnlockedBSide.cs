using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class UnlockedBSide : Entity
    {
        private float alpha;

        private string text;

        private bool waitForKeyPress;

        private float timer;

        public override void Added(Scene scene)
        {
            base.Added(scene);
            base.Tag = (int)Tags.HUD | (int)Tags.PauseUpdate;
            text = ActiveFont.FontSize.AutoNewline(Dialog.Clean("UI_REMIX_UNLOCKED"), 900);
            base.Depth = -10000;
        }

        public IEnumerator EaseIn()
        {
            _ = Scene;
            while ((alpha += Engine.DeltaTime / 0.5f) < 1f)
            {
                yield return null;
            }
            alpha = 1f;
            yield return 1.5f;
            waitForKeyPress = true;
        }

        public IEnumerator EaseOut()
        {
            waitForKeyPress = false;
            while ((alpha -= Engine.DeltaTime / 0.5f) > 0f)
            {
                yield return null;
            }
            alpha = 0f;
            RemoveSelf();
        }

        public override void Update()
        {
            timer += Engine.DeltaTime;
            base.Update();
        }

        public override void Render()
        {
            float num = Ease.CubeOut(alpha);
            Vector2 vector = Celeste.TargetCenter + new Vector2(0f, 64f);
            Vector2 vector2 = Vector2.UnitY * 64f * (1f - num);
            Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * num * 0.8f);
            GFX.Gui["collectables/cassette"].DrawJustified(vector - vector2 + new Vector2(0f, 32f), new Vector2(0.5f, 1f), Color.White * num);
            ActiveFont.Draw(text, vector + vector2, new Vector2(0.5f, 0f), Vector2.One, Color.White * num);
            if (waitForKeyPress)
            {
                GFX.Gui["textboxbutton"].DrawCentered(new Vector2(1824f, 984 + ((timer % 1f < 0.25f) ? 6 : 0)));
            }
        }
    }
}

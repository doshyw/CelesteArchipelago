using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenu;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class PassiveDisplay : SubHeader
    {
        private PropertyInfo setting;
        private object displayedObject;

        public PassiveDisplay(PropertyInfo setting, bool topPadding = true) : base($"{setting.Name}: {setting.GetValue(CelesteArchipelagoModule.Settings)}", topPadding)
        {
            this.setting = setting;
            displayedObject = (string)setting.GetValue(CelesteArchipelagoModule.Settings);
        }

        public override float LeftWidth()
        {
            return ActiveFont.Measure(Title).X;
        }

        public override float Height()
        {
            return ((Title.Length > 0) ? (ActiveFont.HeightOf(Title)) : 0f) + (float)(TopPadding ? 80 : 0);
        }

        public override void Render(Vector2 position, bool highlighted)
        {
            if (Title.Length > 0)
            {
                float alpha = Container.Alpha;
                Color strokeColor = Color.Black * (alpha * alpha * alpha);
                int num = (TopPadding ? 32 : 0);
                Vector2 position2 = position + ((Container.InnerContent == InnerContentMode.TwoColumn) ? new Vector2(0f, num) : new Vector2(Container.Width * 0.5f, num));
                Vector2 justify = new Vector2((Container.InnerContent == InnerContentMode.TwoColumn) ? 0f : 0.5f, 0.5f);
                ActiveFont.DrawOutline(Title, position2, justify, Vector2.One, Color.Gray * alpha, 2f, strokeColor);
            }
        }

        public override void Update()
        {
            base.Update();
            if(displayedObject != setting.GetValue(CelesteArchipelagoModule.Settings))
            {
                displayedObject = setting.GetValue(CelesteArchipelagoModule.Settings);
                Title = $"{setting.Name}: {displayedObject}";
            }
        }

    }
}

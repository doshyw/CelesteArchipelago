using System;
using System.Drawing;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Celeste.Mod.CelesteArchipelago.Graphics
{
    public static class RectangleExt
    {
        public static Rectangle Round(this RectangleF rect)
        {
            var R = int (float f) => (int)Math.Round(f);
            return new(R(rect.X), R(rect.Y), R(rect.Width), R(rect.Height));
        }
    }
}

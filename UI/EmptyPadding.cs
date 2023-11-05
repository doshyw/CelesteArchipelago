using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenu;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class EmptyPadding : Item
    {
        private int space;
        public EmptyPadding(int space)
        {
            this.space = space;
        }

        public override float LeftWidth()
        {
            return 0;
        }

        public override float Height()
        {
            return space;
        }

    }
}

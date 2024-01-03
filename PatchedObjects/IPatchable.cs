using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    internal interface IPatchable
    {
        public void Load();
        public void Unload();
    }
}

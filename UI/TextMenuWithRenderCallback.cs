using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago.UI
{
    internal class TextMenuWithRenderCallback : TextMenu
    {

        private List<Action> RenderList { get; set; } = new List<Action>();

        public TextMenuWithRenderCallback() : base()
        {
        }

        public void AddRender(Action action)
        {
            RenderList.Add(action);
        }

        public void RemoveRender(Action action)
        {
            RenderList.Remove(action);
        }

        public override void Render()
        {
            base.Render();
            foreach(var action in RenderList)
            {
                action();
            }
        }

    }
}

using Microsoft.Xna.Framework;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using System.Linq;
using System;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatLine
    {
        public ChatLineElement[] Elements;

        public DateTime createdTime = DateTime.Now;

        public static ChatLine TestLine
        {
            get
            {
                var arr = new ChatLineElement[]
                {
                    new("I ", Color.Red),
                    new("am ", Color.Orange),
                    new("running ", Color.Yellow),
                    new("a ", Color.Green),
                    new("small ", Color.Blue),
                    new("test. ", Color.Purple),
                    new("But now I wish to be on multiple lines. ", Color.Yellow),
                    new("And here on the 2nd line I may drag on to the 3rd. Well now I should be", Color.White),
                };
                return new(arr);
            }
        }

        public ChatLine(string text, Color color) : this(new[] { new ChatLineElement(text, color) })
        {
        }

        public ChatLine(ChatLineElement[] elements)
        {
            Elements = elements;
        }

        public ChatLine(LogMessage msg) : this(msg.Parts.Select(part => new ChatLineElement(part)).ToArray())
        {
        }

    }

    public class ChatLineElement
    {
        public string text;
        public Color color;

        public ChatLineElement(string text, Color color)
        {
            this.text = text;
            this.color = color;
        }
        public ChatLineElement(MessagePart part)
        {
            text = part.Text;
            color = new Color(part.Color.R, part.Color.G, part.Color.B);
        }

    }

}

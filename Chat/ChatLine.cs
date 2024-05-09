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

        public DateTime createdTime;

        public float MaxTextHeight;

        public float TotalTextWidth;

        public static ChatLine TestLine
        {
            get
            {
                ChatLineElement[] arr = new ChatLineElement[9];
                arr[0] = new ChatLineElement("I ", Color.Red);
                arr[1] = new ChatLineElement("am ", Color.Orange);
                arr[2] = new ChatLineElement("running ", Color.Yellow);
                arr[3] = new ChatLineElement("a ", Color.Green);
                arr[4] = new ChatLineElement("small ", Color.Blue);
                arr[5] = new ChatLineElement("test. ", Color.Purple);
                arr[6] = new ChatLineElement("test. ", Color.Purple);
                arr[7] = new ChatLineElement("test. ", Color.Purple);
                arr[8] = new ChatLineElement("test.", Color.Purple);
                return new ChatLine(arr);
            }
        }

        public ChatLine(string text, Color color)
        {
            Elements = new ChatLineElement[1];
            Elements[0] = new ChatLineElement(text, color);

            Vector2 measure = CelesteNetClientFont.Measure(text);
            TotalTextWidth = measure.X;
            MaxTextHeight = measure.Y;

            createdTime = DateTime.Now;
        }

        public ChatLine(string text, Color color, DateTime createdTime)
        {
            Elements = new ChatLineElement[1];
            Elements[0] = new ChatLineElement(text, color);

            Vector2 measure = CelesteNetClientFont.Measure(text);
            TotalTextWidth = measure.X;
            MaxTextHeight = measure.Y;

            this.createdTime = createdTime;
        }

        public ChatLine(ChatLineElement[] elements)
        {
            TotalTextWidth = 0;
            MaxTextHeight = 0;
            int count = elements.Length;
            Vector2 measure;
            Elements = elements;
            for (int i = 0; i < count; i++)
            {
                measure = CelesteNetClientFont.Measure(Elements[i].text);
                TotalTextWidth += measure.X;
                if (measure.Y > MaxTextHeight) MaxTextHeight = measure.Y;
            }

            createdTime = DateTime.Now;
        }

        public ChatLine(LogMessage msg) : this(msg.Parts.Select(part => new ChatLineElement(part.Text, part.Color)).ToArray())
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

        public ChatLineElement(string text, Archipelago.MultiClient.Net.Models.Color color)
        {
            this.text = text;
            this.color = new Color(color.R, color.G, color.B);
        }

    }

}

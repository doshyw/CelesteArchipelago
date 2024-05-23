﻿using Microsoft.Xna.Framework;
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
                var arr = new ChatLineElement[]
                {
                    new("I ", Color.Red),
                    new("am ", Color.Orange),
                    new("running ", Color.Yellow),
                    new("a ", Color.Green),
                    new("small ", Color.Blue),
                    new("test. ", Color.Purple),
                    new("But now I wish to be on multiple lines.", Color.Yellow),
                    new("ASDF", Color.White),
                };
                return new(arr);
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

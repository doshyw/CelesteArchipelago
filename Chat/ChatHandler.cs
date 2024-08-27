using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Celeste.Mod.CelesteArchipelago.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Monocle;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatHandler : DrawableGameComponent
    {
        private List<ChatLine> messages = new();

        private const float TextHeightProportion = 0.03f;
        private const float ScrollBarWidth = 12;
        private const float MessageRightPadding = 5;
        private const float MessageLeftPadding = 5;
        private static readonly Color ThumbColor = Color.LightBlue;
        private static readonly Color TimestampColor = Color.LightBlue;
        private static readonly TimeSpan MessagePreviewDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan MessageFadeDuration = TimeSpan.FromSeconds(1);

        private int scrollIndex;
        private bool isScrolledToTop;
        private bool isToggled;
        private DateTime? lastReceivedMessage;

        private Rectangle ViewPort => GraphicsDevice.Viewport.Bounds;
        private float TargetTextHeight => ViewPort.Height * TextHeightProportion;
        private float XOffset => ViewPort.Height * TextHeightProportion;
        private float YOffset => ViewPort.Height - (ViewPort.Height * 0.3f);
        private TimeSpan TimeSinceLastMessage
        {
            get
            {
                if (lastReceivedMessage == null) return TimeSpan.MaxValue;
                return DateTime.Now - lastReceivedMessage.Value;
            }
        }
        private bool IsVisible =>
            isToggled ||
            TimeSinceLastMessage < MessagePreviewDuration;
        private float Alpha {
            get
            {
                if (isToggled) return 1;
                var timeRemaining = MessagePreviewDuration - TimeSinceLastMessage;
                if (timeRemaining <= MessageFadeDuration)
                {
                    return Ease.CubeOut((float)timeRemaining.TotalSeconds);
                }
                return 1;
            }
        }
        private Color ChatBoxColor => Color.Black * 0.3f * Alpha;
        private RectangleF Bounds => new RectangleF(0, 0, (ViewPort.Width * 0.4f), (ViewPort.Height * 0.3f) - XOffset);
        private RectangleF TextDrawArea => new RectangleF(MessageLeftPadding, 0,
            Bounds.Width - (MessageLeftPadding + ScrollBarWidth + MessageRightPadding),
            Bounds.Height);

        public ChatHandler(Game game) : base(game)
        {
            UpdateOrder = 10000;
            DrawOrder = 10000;
            Enabled = false;
        }

        public void Init()
        {
            CelesteArchipelagoModule.Settings.ScrollChatDown.SetRepeat(0.15f);
            CelesteArchipelagoModule.Settings.ScrollChatUp.SetRepeat(0.15f);
            if (CelesteArchipelagoModule.Settings.Chat == true){
                Enabled = true;
            }
        }

        public void DeInit()
        {
            Enabled = false;
            messages = new();
        }

        public void HandleMessage(LogMessage message)
        {
            if (Enabled)
            {
                lock (messages)
                {
                    lastReceivedMessage = DateTime.Now;
                    scrollIndex = 0;
                    messages.Add(new ChatLine(message));
                }
            }
        }

        public void HandleMessage(string text, Color color)
        {
            if (Enabled)
            {
                lock (messages)
                {
                    lastReceivedMessage = DateTime.Now;
                    scrollIndex = 0;
                    messages.Add(new ChatLine(text, color));
                }
            }
        }

        public override void Update(GameTime gameTime)
        {
            HandleInputs();
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Enabled || !IsVisible) return;

            var clipRect = new RectangleF(XOffset, YOffset, Bounds.Width, Bounds.Height).Round();
            var sb = Monocle.Draw.SpriteBatch;

            var rs = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true };
            sb.GraphicsDevice.ScissorRectangle = clipRect;
            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                rs,
                null,
                Matrix.CreateTranslation(XOffset, YOffset, 0)
            );
            Render();
            sb.End();
        }

        private void Render()
        {
            lock (messages)
            {
                RenderRectF(Bounds, ChatBoxColor);

                var scalingFactor = TargetTextHeight / CelesteNetClientFont.LineHeight;
                var cursor = new ScaledCursor(scalingFactor, TextDrawArea);
                cursor.SetVerticalScroll(scrollIndex);
                
                foreach (var message in messages.AsEnumerable().Reverse())
                {
                    cursor.PrintMessage(message);
                }

                var textHiddenAbove = Math.Max(-cursor.Top, 0);

                // This prevents scrolling up without a scrollbar or when there's not enough text
                isScrolledToTop = textHiddenAbove <= 0;
                if (!cursor.IsScrollable) return;

                var bgRect = new RectangleF(
                    TextDrawArea.Right + MessageRightPadding,
                    TextDrawArea.Top,
                    ScrollBarWidth,
                    TextDrawArea.Height
                );
                RenderRectF(bgRect, ChatBoxColor * 1.3f);

                var thumbYOffset = (textHiddenAbove * bgRect.Height) / cursor.TotalTextHeight;
                var thumbRect = new RectangleF(
                    bgRect.X,
                    bgRect.Y + thumbYOffset,
                    bgRect.Width,
                    (TextDrawArea.Height * bgRect.Height) / cursor.TotalTextHeight
                );
                RenderRectF(thumbRect, ThumbColor);

            }
        }

        private void HandleInputs()
        {
            var settings = CelesteArchipelagoModule.Settings;
            
            if (settings.ToggleChat.Pressed)
            {
                isToggled = !isToggled;
            }

            if (!Enabled || !IsVisible) return;

            if (!isScrolledToTop && settings.ScrollChatUp.Pressed)
            {
                scrollIndex++;
            }

            if (settings.ScrollChatDown.Pressed)
            {
                if (scrollIndex > 0)
                {
                    scrollIndex--;
                }
            }
        }

        private void RenderRectF(RectangleF rect, Color color)
        {
            Monocle.Draw.Rect(rect.X, rect.Y, rect.Width, rect.Height, color);
        }

        private class ScaledCursor
        {
            public float Top => location.Y + yScrollOffset;
            public float TotalTextHeight => drawingArea.Bottom - location.Y;
            public bool IsScrollable => location.Y < 0;

            private Vector2 location = new();
            private readonly float scalingFactor;
            private readonly Vector2 scaler;
            private readonly RectangleF drawingArea;
            private float yScrollOffset = 0;

            public ScaledCursor(float scalingFactor, RectangleF drawingArea)
            {
                this.scalingFactor = scalingFactor;
                this.drawingArea = drawingArea;
                scaler = Vector2.One * scalingFactor;
                
                location.X = drawingArea.Left;
                location.Y = drawingArea.Bottom;
            }

            public void PrintMessage(ChatLine message)
            {
                var timeStamp = message.createdTime.ToString("[HH:mm:ss]") + " ";
                var fullString = timeStamp + string.Join(" ", message.Elements.Select(x => x.text.Trim(' ')));
                var lc = CountLines(fullString, drawingArea.Width);

                location.X = drawingArea.Left;
                Up(lc);

                var tmpOffset = PrintWord(timeStamp, TimestampColor);

                foreach (var element in message.Elements)
                {
                    // Split on space but include it in the output, so "Hello " -> ["Hello", " "]
                    var wordArray = Regex.Split(element.text, @"(?<=[ ])");

                    foreach (var word in wordArray)
                    {
                        tmpOffset += PrintWord(word, element.color);
                    }
                }

                Up(tmpOffset);
            }

            private int PrintWord(string word, Color color)
            {
                var tmpOffset = 0;
                
                var width = CelesteNetClientFont.Measure(word).X * scalingFactor;
                if (location.X + width > drawingArea.Width)
                {
                    tmpOffset++;
                    Newline();
                }

                var position = new Vector2(location.X, Top);
                CelesteNetClientFont.Draw(word, position, Vector2.Zero, scaler, color);
                location.X += width;

                return tmpOffset;
            }

            private void Newline()
            {
                location.X = drawingArea.Left;
                location.Y += CelesteNetClientFont.LineHeight * scalingFactor;
            }

            private void Up(int lineCount)
            {
                location.Y -= lineCount * CelesteNetClientFont.LineHeight * scalingFactor;
            }

            public void SetVerticalScroll(int lineCount)
            {
                yScrollOffset = lineCount * scalingFactor * CelesteNetClientFont.LineHeight;
            }
            private int CountLines(string text, float width)
            {
                var line = string.Empty;
                var returnString = string.Empty;
                var wordArray = text.Split(' ');
                var lineCount = 1;

                foreach (var word in wordArray)
                {
                    if (CelesteNetClientFont.Measure(line + word).X > width * (1 / scalingFactor))
                    {
                        returnString = returnString + line + '\n';
                        line = string.Empty;
                        lineCount++;
                    }

                    line = line + word + ' ';
                }
                
                return lineCount;
            }
        }
    }
}

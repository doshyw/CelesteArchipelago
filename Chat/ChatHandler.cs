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
using System.Drawing.Printing;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatHandler : DrawableGameComponent
    {
        public ChatLog Log = new ChatLog();

        private const float TextHeightProportion = 0.03f;
        private const float scrollBarWidth = 20;
        private const float messageRightPadding = 5;
        private const float messageLeftPadding = 5;
        private static readonly TimeSpan messagePreviewDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan messageFadeDuration = TimeSpan.FromSeconds(1);

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
            TimeSinceLastMessage < messagePreviewDuration;
        private float Alpha {
            get
            {
                if (isToggled) return 1;
                var timeRemaining = messagePreviewDuration - TimeSinceLastMessage;
                if (timeRemaining <= messageFadeDuration)
                {
                    return Ease.CubeOut((float) timeRemaining.TotalSeconds);
                }
                return 1;
            }
        }
        private Color ChatBoxColor => Color.Black * 0.3f * Alpha;
        private RectangleF Bounds => new RectangleF(0, 0, (ViewPort.Width * 0.4f), (ViewPort.Height * 0.3f) - XOffset);
        private RectangleF TextDrawArea => new RectangleF(messageLeftPadding, 0,
                            Bounds.Width - (messageLeftPadding + scrollBarWidth + messageRightPadding),
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
            Enabled = true;
        }

        public void DeInit()
        {
            Enabled = false;
            Log = new ChatLog();
        }

        public void HandleMessage(LogMessage message)
        {
            if (Enabled)
            {
                lock (Log)
                {
                    lastReceivedMessage = DateTime.Now;
                    scrollIndex = 0;
                    Log.Add(new ChatLine(message));
                }
            }
        }

        public void HandleMessage(string text, Color color)
        {
            if (Enabled)
            {
                lock (Log)
                {
                    lastReceivedMessage = DateTime.Now;
                    scrollIndex = 0;
                    Log.Add(new ChatLine(text, color));
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

            var rs = new RasterizerState() { CullMode = CullMode.None};
            //var rs = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true };
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
            lock (Log)
            {
                RenderRectF(Bounds, ChatBoxColor);
                float totalTextHeight = 0;

                float scalingFactor = TargetTextHeight / CelesteNetClientFont.LineHeight;
                var cursor = new ScaledCursor(scalingFactor, TextDrawArea);

                var m1 = "Hello, my name is matthieu, and I am a test for a longer line. ";
                var m2 = "Hopefully(I can be of service).";
                var m = m1 + " " + m2;

                int lc = LimitLineLength(m, scalingFactor, TextDrawArea.Width).Count(x => x == '\n') + 1;
                cursor.StartMessage(lc * CelesteNetClientFont.LineHeight * scalingFactor);
                cursor.Type(m1, Color.White);
                cursor.Type(m2, Color.White);

                // Skipping lines is easier and requires less math to figure out where to draw lines and ensures we always show full messages
                // However, it makes the scrolling chunkier and will have issues if the line to show is incredibly long
                float nextLineYOffset = 0;
                int index = 0;
                foreach (var message in Log.GetLog().Reverse())
                {
                    var fullString = message.Elements.Select(x => x.text);
                    var a  = string.Join(" ", fullString);

                    foreach (var element in message.Elements)
                    {
                        var parsedText = LimitLineLength(element.text, scalingFactor, TextDrawArea.Width);
                        var textHeight = CelesteNetClientFont.HeightOf(parsedText) * scalingFactor;

                        // The idea here is to skip the elements, but calculate their height for the scrollbar
                        totalTextHeight += textHeight;
                        if (index < scrollIndex)
                        {
                            break;
                        }

                        nextLineYOffset += textHeight;
                        var position = new Vector2(TextDrawArea.X, TextDrawArea.Bottom - nextLineYOffset);
                        CelesteNetClientFont.Draw(
                            parsedText,
                            position,
                            Vector2.One * scalingFactor,
                            element.color * Alpha
                        );
                    }

                    index++;
                }

                var textHiddenAbove = Math.Max(nextLineYOffset - Bounds.Height, 0);

                isScrolledToTop = textHiddenAbove <= 0;
                if (totalTextHeight < Bounds.Height) return;

                var bgRect = new RectangleF(
                    TextDrawArea.Right + messageRightPadding,
                    TextDrawArea.Top,
                    scrollBarWidth,
                    TextDrawArea.Height
                );
                RenderRectF(bgRect, ChatBoxColor * 1.3f);

                var thumbYOffset = (textHiddenAbove * bgRect.Height) / totalTextHeight;
                var thumbRect = new RectangleF(
                    bgRect.X,
                    bgRect.Y + (thumbYOffset),
                    bgRect.Width,
                    (TextDrawArea.Height * bgRect.Height) / totalTextHeight
                );
                RenderRectF(thumbRect, Color.Aqua);
            }
        }

        private void HandleInputs()
        {
            var settings = CelesteArchipelagoModule.Settings;
            
            if (settings.ToggleChat.Pressed)
            {
                isToggled = !isToggled;
            }


            if (settings.AddMessages.Pressed)
            {
                lastReceivedMessage = DateTime.Now;
                scrollIndex = 0;
                Log.Add(ChatLine.TestLine);
                //HandleMessage("Testsetse tste set set sets lkjasd falksdjf aksd jflaksjfd lakjsfd lkajsfd lkja", Color.White);
            }

            if (!Enabled || !IsVisible) return;

            if (!isScrolledToTop && settings.ScrollChatUp.Pressed)
            {
                if (scrollIndex < Log.Length() - 1)
                {
                    scrollIndex++;
                }
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

        private string LimitLineLength(string text, float scalingFactor, float maxWidth)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (CelesteNetClientFont.Measure(line + word).X > maxWidth * (1 / scalingFactor))
                {
                    returnString = returnString + line + '\n';
                    line = string.Empty;
                }

                line = line + word + ' ';
            }

            return returnString + line;
        }

        private class ScaledCursor
        {
            private Vector2 location = new();
            public float X => location.X;
            public float Y => location.Y;

            private readonly float scalingFactor;
            private readonly Vector2 scaler;
            private readonly RectangleF drawingArea;

            public ScaledCursor(float scalingFactor, RectangleF drawingArea)
            {
                this.scalingFactor = scalingFactor;
                this.drawingArea = drawingArea;
                scaler = Vector2.One * scalingFactor;
                
                location.X = drawingArea.Left;
                location.Y = drawingArea.Bottom;
            }

            public void Type(string text, Color color)
            {
                string[] wordArray = text.Split(' ');

                foreach (var word in wordArray)
                {
                    var width = CelesteNetClientFont.Measure(word).X * scalingFactor;
                    if (location.X + width > drawingArea.Width)
                    {
                        Newline();
                    }
                    CelesteNetClientFont.Draw(word + ' ', location, Vector2.Zero, scaler, color);
                    location.X += width + CelesteNetClientFont.Measure(' ').X * scalingFactor;
                }
            }

            public void StartMessage(float height)
            {
                location.X = drawingArea.Left;
                location.Y -= height;
            }

            private void Newline()
            {
                location.Y += CelesteNetClientFont.LineHeight * scalingFactor;
                location.X = drawingArea.Left;
            }

            public static implicit operator Vector2(ScaledCursor c) => c.location;
        }
    }
}

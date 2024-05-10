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
using Archipelago.MultiClient.Net.Models;

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

                float scalingFactor = TargetTextHeight / CelesteNetClientFont.LineHeight;
                var cursor = new ScaledCursor(scalingFactor, TextDrawArea);
                cursor.SetYLineOffset(scrollIndex);
                
                foreach (var message in Log.GetLog().Reverse())
                {
                    var fullString = string.Join(" ", message.Elements.Select(x => x.text));
                    int lc = LimitLineLength(fullString, scalingFactor, TextDrawArea.Width).Count(x => x == '\n') + 1;
                    cursor.StartMessage(lc * CelesteNetClientFont.LineHeight * scalingFactor);
                    CelesteNetClientFont.Draw(lc.ToString(), new Vector2(cursor.Location.X + 500, cursor.Location.Y),
                        Color.White);
                    foreach (var element in message.Elements)
                    {
                        cursor.Type(element.text, element.color);
                    }
                }

                CelesteNetClientFont.Draw($"{cursor.Location.Y}; {cursor.Location.X}", new(0, -400), Color.White);
                CelesteNetClientFont.Draw($"{CelesteNetClientFont.LineHeight * scalingFactor}", new(0, -300),
                    Color.White);

                var textHiddenAbove = Math.Max(-cursor.Y, 0);

                CelesteNetClientFont.Draw($"{cursor.Y}; {cursor.TotalTextHeight}; {textHiddenAbove}", new(0, -500), Color.White);
                isScrolledToTop = textHiddenAbove <= 0;
                if (!cursor.IsScrollable) return;

                var bgRect = new RectangleF(
                    TextDrawArea.Right + messageRightPadding,
                    TextDrawArea.Top,
                    scrollBarWidth,
                    TextDrawArea.Height
                );
                RenderRectF(bgRect, ChatBoxColor * 1.3f);

                var thumbYOffset = (textHiddenAbove * bgRect.Height) / cursor.TotalTextHeight;
                var thumbRect = new RectangleF(
                    bgRect.X,
                    bgRect.Y + (thumbYOffset),
                    bgRect.Width,
                    (TextDrawArea.Height * bgRect.Height) / cursor.TotalTextHeight
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
                //Log.Add(ChatLine.TestLine);
                HandleMessage("Testsetse tste set set sets lkjasd falksdjf aksd jflaksjfd lakjsfd lkajsfd lkja", Color.White);
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
            public float Y => location.Y + yScrollOffset;
            public float TotalTextHeight => drawingArea.Bottom - location.Y;
            public bool IsScrollable => location.Y < 0;
            public Vector2 Location => location;
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

            public void Type(string text, Color color)
            {
                string[] wordArray = text.Split(' ');
                int tmpOffset = 0;

                foreach (var word in wordArray)
                {
                    var width = CelesteNetClientFont.Measure(word).X * scalingFactor;
                    if (location.X + width > drawingArea.Width)
                    {
                        tmpOffset++;
                        Newline();
                    }

                    var position = new Vector2(location.X, Y);
                    CelesteNetClientFont.Draw(word + ' ', position, Vector2.Zero, scaler, color);
                    location.X += width + CelesteNetClientFont.Measure(' ').X * scalingFactor;
                }

                location.Y -= tmpOffset * scalingFactor * CelesteNetClientFont.LineHeight;
            }

            public void StartMessage(float height)
            {
                location.X = drawingArea.Left;
                location.Y -= height;
            }

            private void Newline()
            {
                location.X = drawingArea.Left;
                location.Y += CelesteNetClientFont.LineHeight * scalingFactor;
            }

            public void SetYLineOffset(int lineCount)
            {
                yScrollOffset = lineCount * scalingFactor * CelesteNetClientFont.LineHeight;
            }

            public static implicit operator Vector2(ScaledCursor c) => c.location;
        }
    }
}

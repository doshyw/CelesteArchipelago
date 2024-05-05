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

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatHandler : DrawableGameComponent
    {
        public ChatLog Log = new ChatLog();

        private const float TextHeightProportion = 0.03f;
        private const float scrollBarWidth = 30;
        private static readonly TimeSpan messagePreviewDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan messageFadeDuration = TimeSpan.FromSeconds(1);

        private int scrollIndex;
        private float messageHeight;
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
        private RectangleF bounds => new RectangleF(0, 0, (ViewPort.Width * 0.4f), (ViewPort.Height * 0.3f));

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

            var clipRect = new RectangleF(XOffset, YOffset, bounds.Width, bounds.Height).Round();
            var sb = Monocle.Draw.SpriteBatch;

            // Testing
            sb.Begin();
            var debugString = $"{bounds.X};{bounds.Y};{bounds.Width};{bounds.Height}";
            CelesteNetClientFont.Draw(debugString, new Vector2(0, 0), Color.White);
            CelesteNetClientFont.Draw(Alpha.ToString(), new Vector2(0, 50), Color.White);
            Monocle.Draw.HollowRect(clipRect, Color.AliceBlue);
            //Monocle.Draw.Rect(0, 0, ViewPort.Width, ViewPort.Height, Color.Aqua * 0.3f);
            sb.End();

            //var rs = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true };
            var rs = new RasterizerState() { CullMode = CullMode.None };
            //sb.GraphicsDevice.ScissorRectangle = rect;
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
                RenderRectF(bounds, ChatBoxColor);

                float nextLineYOffset = 0;
                float totalHeight = 0;
                // Skipping lines is easier and requires less math to figure out where to draw lines and ensures we always show full messages
                // However, it makes the scrolling chunkier and will have issues if the line to show is incredibly long
                foreach (var line in Log.GetLog().Reverse().Skip(scrollIndex))
                {
                    float scalingFactor = TargetTextHeight / line.MaxTextHeight;

                    foreach (var element in line.Elements)
                    {
                        var parsedText = LimitLineLength(element.text, scalingFactor, bounds.Width);
                        nextLineYOffset += CelesteNetClientFont.HeightOf(parsedText) * scalingFactor;
                        var position = new Vector2(0, bounds.Bottom - nextLineYOffset);
                        CelesteNetClientFont.Draw(
                            parsedText,
                            position,
                            Vector2.One * scalingFactor,
                            element.color * Alpha
                        );
                        totalHeight += position.Y;
                    }
                }

                messageHeight = totalHeight;
            }
        }

        private void HandleInputs()
        {
            var settings = CelesteArchipelagoModule.Settings;
            
            if (settings.ToggleChat.Pressed)
            {
                isToggled = !isToggled;
            }
            if (settings.ScrollChatUp.Pressed)
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
    }
}

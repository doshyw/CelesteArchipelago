using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Celeste.Mod.CelesteArchipelago.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatHandler : DrawableGameComponent
    {
        private const float TextAlpha = 1f;

        private const float TextHeightProportion = 0.03f;
        
        private static readonly Color ChatBoxColor = Color.Black * 0.3f;

        public ChatLog Log = new ChatLog();

        private Rectangle ViewPort => Game.GraphicsDevice.Viewport.Bounds;
        private float targetTextHeight => ViewPort.Height * TextHeightProportion;
        private float leftPadding => ViewPort.Height * TextHeightProportion;
        private float bottomPadding => ViewPort.Height * TextHeightProportion;

        private int scrollIndex;
        private bool isToggled;
        private DateTime? lastReceivedMessage;
        private static readonly TimeSpan messagePreviewDuration = TimeSpan.FromSeconds(5);
        private bool isVisible =>
            isToggled || lastReceivedMessage.HasValue &&
            (DateTime.Now - lastReceivedMessage.Value) < messagePreviewDuration;
        private RectangleF bounds => new RectangleF(
            leftPadding,
            ViewPort.Height + ViewPort.X - (ViewPort.Height * 0.3f), 
            (ViewPort.Width * 0.4f), 
            (ViewPort.Height * 0.3f));

        public ChatHandler(Game game) : base(game)
        {
            UpdateOrder = 10000;
            DrawOrder = 10000;
            Enabled = false;
        }

        public void Init()
        {
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

        private void Render()
        {
            lock(Log)
            {
                RenderRectF(bounds, ChatBoxColor);

                float yOffset = bottomPadding;
                foreach (var line in Log.GetLog().Reverse().Skip(scrollIndex))
                {

                    float scalingFactor = targetTextHeight / line.MaxTextHeight;

                    foreach (var element in line.Elements)
                    {
                        var parsedText = ParseText(element.text, scalingFactor);
                        var lineCount = parsedText.Count(x => x == '\n');
                        yOffset += lineCount * line.MaxTextHeight * scalingFactor;
                        var position = new Vector2(leftPadding, bounds.Bottom - yOffset);
                        CelesteNetClientFont.Draw(
                            parsedText,
                            position,
                            Vector2.One * scalingFactor,
                            element.color * TextAlpha
                        );
                    }

                    yOffset += line.MaxTextHeight * scalingFactor;
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
            if (!Enabled || !isVisible) return;

            var sb = Monocle.Draw.SpriteBatch;

            sb.Begin();
            Monocle.Draw.Rect(0, 0, ViewPort.Width, ViewPort.Height, Color.Aqua * 0.3f);
            ;
            sb.End();

            var rs = new RasterizerState() { CullMode = CullMode.None, ScissorTestEnable = true };
            var rect = bounds.Round();
            sb.GraphicsDevice.ScissorRectangle = rect;
            sb.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                rs,
                null,
                Matrix.Identity
            );

            Render();
            
            sb.End();
        }

        private void HandleInputs()
        {
            var settings = CelesteArchipelagoModule.Settings;
            
            if (settings.ToggleChat.Pressed)
            {
                isToggled = !isToggled;
            }
            if (settings.ScrollChatUp.Check)
            {
                if (scrollIndex < Log.Length() - 1)
                {
                    scrollIndex++;
                }
            }
            if (settings.ScrollChatDown.Check)
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

        private string ParseText(string text, float scalingFactor)
        {
            string line = string.Empty;
            string returnString = string.Empty;
            string[] wordArray = text.Split(' ');

            foreach (string word in wordArray)
            {
                if (CelesteNetClientFont.Measure(line + word).X > bounds.Width * (1 / scalingFactor))
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

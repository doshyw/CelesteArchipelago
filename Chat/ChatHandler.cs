using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatHandler : DrawableGameComponent
    {
        private const float TextAlpha = 1f;

        private const float TextHeightProportion = 0.03f;
        
        private static readonly Color ChatBoxColor = Color.Black * 0.3f;

        public ChatLog Log = new ChatLog();

        private bool isVisible = false;
        private KeyboardState keyboardState;

        private int screenHeight => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        private int screenWidth => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        private float targetTextHeight => screenHeight * TextHeightProportion;
        private readonly float leftPadding;
        private readonly float bottomPadding;

        private RectangleF bounds;

        public ChatHandler(Game game) : base(game)
        {
            leftPadding = screenHeight * TextHeightProportion;
            bottomPadding = screenHeight * TextHeightProportion;
            var boundsHeight = screenHeight * 0.3f;
            var boundsWidth = screenWidth * 0.4f;
            bounds = new RectangleF(leftPadding, screenHeight - boundsHeight - bottomPadding, boundsWidth, boundsHeight);

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
                    Log.Add(new ChatLine(text, color));
                }
            }
        }

        void Render(GameTime gameTime)
        {
            lock(Log)
            {
                RenderRectF(bounds, ChatBoxColor);

                float yOffset = bottomPadding;
                foreach (var line in Log.GetLog().Reverse())
                {

                    float scalingFactor = targetTextHeight / line.MaxTextHeight;

                    foreach (var element in line.Elements)
                    {
                        var parsedText = parseText(element.text, scalingFactor);
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

        public override void Draw(GameTime gameTime)
        {
            var previousState = keyboardState;
            keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.T) && previousState.IsKeyUp(Keys.T))
            {
                isVisible = !isVisible;
            }

            bounds.Width += (int)keyboardState[Keys.NumPad6] * 3;
            bounds.Width -= (int)keyboardState[Keys.NumPad4] * 3;
            bounds.Height += (int)keyboardState[Keys.NumPad8] * 3;
            bounds.Height -= (int)keyboardState[Keys.NumPad2] * 3;

            if (!isVisible) return;
            
            Monocle.Draw.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Matrix.Identity
            );
            DrawDebug();
            Render(gameTime);
            Monocle.Draw.SpriteBatch.End();
        }

        private void RenderRectF(RectangleF rect, Color color)
        {
            Monocle.Draw.Rect(rect.X, rect.Y, rect.Width, rect.Height, color);
        }

        private void DrawDebug()
        {
            CelesteNetClientFont.Draw($"X:{bounds.X} Y:{bounds.Y} W:{bounds.Width} H:{bounds.Height}", new(10, 10), Color.White);
        }

        private string parseText(string text, float scalingFactor)
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

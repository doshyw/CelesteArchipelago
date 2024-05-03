using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;
using System.Linq;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Celeste.Mod.CelesteArchipelago.Graphics;
using Microsoft.Xna.Framework.Input;
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

        private bool isToggled = false;
        private KeyboardState keyboardState;

        private int screenHeight => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        private int screenWidth => GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        private float targetTextHeight => screenHeight * TextHeightProportion;
        private readonly float leftPadding;
        private readonly float bottomPadding;

        private DateTime? lastReceivedMessage;
        private static readonly TimeSpan messagePreviewDuration = TimeSpan.FromSeconds(5);
        private RectangleF bounds;

        private long snapshotGT;
        private int scrollIndex;

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

        void Render(GameTime gameTime)
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
            if (!Enabled) return;

            var previousState = keyboardState;
            keyboardState = Keyboard.GetState();
            HandleKeyDown(previousState, keyboardState);

            var hasRecentMessage = lastReceivedMessage.HasValue && (DateTime.Now - lastReceivedMessage.Value) < messagePreviewDuration;

            if (!(isToggled || hasRecentMessage)) return;

            var sb = Monocle.Draw.SpriteBatch;

            sb.Begin();
            var asWord = string.Join(";", keyboardState.GetPressedKeys().Select(x => x.ToString()));
            CelesteNetClientFont.Draw(asWord, Vector2.One, Vector2.One, Color.White);
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

            Render(gameTime);
            
            sb.End();
        }

        private void HandleKeyDown(KeyboardState previousState, KeyboardState currentState)
        {
            foreach (var key in currentState.GetPressedKeys())
            {
                if (!currentState.IsKeyDown(key) || !previousState.IsKeyUp(key)) continue;

                if (key == Keys.NumPad8)
                {
                    if (scrollIndex < Log.Length() - 1)
                    {
                        scrollIndex++;
                    }
                }
                if (key == Keys.NumPad2)
                {
                    if (scrollIndex > 0)
                    {
                        scrollIndex--;
                    }
                }
                if (key == Keys.T)
                {
                    isToggled = !isToggled;
                }
            }
            
        }

        private void RenderRectF(RectangleF rect, Color color)
        {
            Monocle.Draw.Rect(rect.X, rect.Y, rect.Width, rect.Height, color);
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

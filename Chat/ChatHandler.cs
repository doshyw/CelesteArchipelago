using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using Celeste.Mod.CelesteArchipelago.Chat;
using Archipelago.MultiClient.Net.MessageLog.Messages;

namespace Celeste.Mod.CelesteArchipelago
{
    public class ChatHandler : DrawableGameComponent
    {
        public ChatLog Log = new ChatLog();

        public float TextAlpha = 1f;
        public float ChatBoxAlpha = 0.3f;

        public Color TextColor = Color.White;
        public Color ChatBoxColor = Color.Black;

        public float TextHeightProportion = 0.03f;

        public TimeSpan messagePreviewTime = new TimeSpan(0, 0, 10);

        private int screenHeight
        {
            get
            {
                return GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
        }

        private int screenWidth
        {
            get
            {
                return GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            }
        }

        private float targetTextHeight
        {
            get
            {
                return screenHeight * TextHeightProportion;
            }
        }

        private float windowOffset
        {
            get
            {
                return screenHeight * TextHeightProportion;
            }
        }

        private float textBorderOffset
        {
            get
            {
                return 0.5f * screenHeight * TextHeightProportion;
            }
        }

        public ChatHandler(Game game) : base(game)
        {
            UpdateOrder = 10000;
            DrawOrder = 10000;
            Enabled = false;
        }

        public void Init()
        {
            Enabled = true;
            Log.Add(ChatLine.TestLine);
        }

        public void HandleMessage(LogMessage message)
        {
            lock(Log)
            {
                Log.Add(new ChatLine(message));
            }
        }

        Vector4 RenderLine(Vector2 bottomLeft, ChatLine chatLine, float fadeAlpha)
        {
            float scalingFactor = targetTextHeight / chatLine.MaxTextHeight;

            float rectangleWidth = scalingFactor * (chatLine.TotalTextWidth + 2 * textBorderOffset);
            float rectangleHeight = scalingFactor * (chatLine.MaxTextHeight + 2 * textBorderOffset);
            RenderRect(
                bottomLeft.X,
                bottomLeft.Y - rectangleHeight,
                rectangleWidth,
                rectangleHeight,
                ChatBoxColor * ChatBoxAlpha * fadeAlpha
            );

            Vector2 sizeText;
            float wordOffset = bottomLeft.X + scalingFactor * textBorderOffset;
            foreach (var element in chatLine.Elements)
            {
                sizeText = CelesteNetClientFont.Measure(element.text);
                CelesteNetClientFont.Draw(
                    element.text,
                    new Vector2(wordOffset, bottomLeft.Y - scalingFactor * (sizeText.Y + textBorderOffset)),
                    Vector2.Zero,
                    Vector2.One * scalingFactor,
                    element.color * TextAlpha * fadeAlpha
                );
                wordOffset += scalingFactor * sizeText.X;
            }

            return new Vector4(bottomLeft.X, bottomLeft.Y - rectangleHeight, rectangleWidth, rectangleHeight);

        }

        void Render(GameTime gameTime)
        {
            lock(Log)
            {
                DateTime now = DateTime.Now;

                float yOffset = windowOffset;
                float deltaToFade, fadeAlpha;
                Vector4 previousBounds;
                foreach (var line in Log.GetLogWithTimeout((float)messagePreviewTime.TotalSeconds))
                {
                    if ((now - line.createdTime).TotalSeconds > messagePreviewTime.TotalSeconds) continue;
                    deltaToFade = (float)((now - line.createdTime).TotalSeconds - messagePreviewTime.TotalSeconds / 2f);
                    if (deltaToFade <= 0)
                    {
                        fadeAlpha = 1;
                    }
                    else
                    {
                        fadeAlpha = 1f - Ease.CubeIn(deltaToFade);
                    }
                    previousBounds = RenderLine(new Vector2(windowOffset, screenHeight - yOffset), line, fadeAlpha);
                    yOffset += previousBounds.W + 1;
                }
            }
        }

        public override void Draw(GameTime gameTime)
        {
            Monocle.Draw.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Matrix.Identity
            );

            Render(gameTime);

            Monocle.Draw.SpriteBatch.End();
        }

        public void DisplayText(string text)
        {
            Logger.Log("CelesteArchipelago.ChatHandler", text);
        }

        private void RenderRect(float x, float y, float width, float height, Color color)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            int wi = (int)Math.Ceiling(x + width) - xi;
            int hi = (int)Math.Ceiling(y + height) - yi;

            Monocle.Draw.Rect(xi, yi, wi, hi, color);
        }

    }
}

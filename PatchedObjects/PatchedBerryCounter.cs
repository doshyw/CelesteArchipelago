using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PatchedBerryCounter : IPatchable
    {
        public void Load()
        {
			On.Celeste.GameplayStats.Render += BerryCounter;

		}

        public void Unload()
        {
            On.Celeste.GameplayStats.Render -= BerryCounter;
		}
		public static void BerryCounter(On.Celeste.GameplayStats.orig_Render render, GameplayStats stats)
		{

			if (stats.DrawLerp <= 0f)
			{
				return;
			}
			float num = Ease.CubeOut(stats.DrawLerp);
			Level level = stats.Scene as Level;
			AreaKey area = level.Session.Area;
			AreaModeStats areaModeStats = SaveData.Instance.Areas[area.ID].Modes[(int)area.Mode];
			ModeProperties modeProperties = AreaData.Get(area).Mode[(int)area.Mode];
			int totalStrawberries = modeProperties.TotalStrawberries;
			int num2 = 32;
			int num3 = (totalStrawberries - 1) * num2;
			int num4 = ((totalStrawberries > 0 && modeProperties.Checkpoints != null) ? (modeProperties.Checkpoints.Length * num2) : 0);
			Vector2 position = new Vector2((1920 - num3 - num4) / 2, 1016f + (1f - num) * 80f);
			if (totalStrawberries <= 0)
			{
				return;
			}
			int num5 = ((modeProperties.Checkpoints == null) ? 1 : (modeProperties.Checkpoints.Length + 1));
			for (int i = 0; i < num5; i++)
			{
				int num6 = ((i == 0) ? modeProperties.StartStrawberries : modeProperties.Checkpoints[i - 1].Strawberries);
				for (int j = 0; j < num6; j++)
				{
					EntityData entityData = modeProperties.StrawberriesByCheckpoint[i, j];
					if (entityData == null)
					{
						continue;
					}
					bool flag = false;
					foreach (EntityID strawberry in level.Session.Strawberries)
					{
						if (entityData.ID == strawberry.ID && entityData.Level.Name == strawberry.Level)
						{
							flag = true;
						}
					}
					MTexture mTexture = GFX.Gui["dot"];
					if (flag)
					{
						if (area.Mode == AreaMode.CSide)
						{
							mTexture.DrawOutlineCentered(position, Calc.HexToColor("f2ff30"), 1.5f);
						}
						else
						{
							mTexture.DrawOutlineCentered(position, Calc.HexToColor("ff3040"), 1.5f);
						}
					}
					else
					{
						bool flag2 = false;
						foreach (EntityID strawberry2 in areaModeStats.Strawberries)
						{
							if (entityData.ID == strawberry2.ID && entityData.Level.Name == strawberry2.Level)
							{
								flag2 = true;
							}
						}
						if (flag2)
						{
							mTexture.DrawOutlineCentered(position, Calc.HexToColor("4193ff"), 1f);
						}
						else
						{
							Draw.Rect(position.X - (float)mTexture.ClipRect.Width * 0.5f, position.Y - 4f, mTexture.ClipRect.Width, 8f, Color.DarkGray);
						}
					}
					position.X += num2;
				}
				if (modeProperties.Checkpoints != null && i < modeProperties.Checkpoints.Length)
				{
					Draw.Rect(position.X - 3f, position.Y - 16f, 6f, 32f, Color.DarkGray);
					position.X += num2;
				}
			}
		}
	}
}

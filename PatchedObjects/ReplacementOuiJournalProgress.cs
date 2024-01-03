using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class ReplacementOuiJournalProgress : OuiJournalPage
    {
        private Table table;

        public ReplacementOuiJournalProgress(OuiJournal journal)
            : base(journal)
        {
            PageTexture = "page";
            table = new Table().AddColumn(new TextCell(Dialog.Clean("journal_progress"), new Vector2(0f, 0.5f), 1f, Color.Black * 0.7f)).AddColumn(new EmptyCell(20f)).AddColumn(new EmptyCell(64f))
                .AddColumn(new EmptyCell(64f))
                .AddColumn(new EmptyCell(100f))
                .AddColumn(new IconCell("strawberry", 150f))
                .AddColumn(new IconCell("skullblue", 100f));
            if (SaveData.Instance.UnlockedModes >= 2)
            {
                table.AddColumn(new IconCell("skullred", 100f));
            }
            if (SaveData.Instance.UnlockedModes >= 3)
            {
                table.AddColumn(new IconCell("skullgold", 100f));
            }
            table.AddColumn(new IconCell("time", 220f));
            foreach (AreaStats item in SaveData.Instance.Areas_Safe)
            {
                AreaData areaData = AreaData.Get(item.ID_Safe);
                if (areaData.Interlude_Safe)
                {
                    continue;
                }
                if (areaData.ID > SaveData.Instance.UnlockedAreas_Safe)
                {
                    break;
                }
                string text = null;
                if (areaData.Mode[0].TotalStrawberries > 0 || item.TotalStrawberries > 0)
                {
                    text = item.TotalStrawberries.ToString();
                    if (item.Modes[0].Completed)
                    {
                        text = text + "/" + areaData.Mode[0].TotalStrawberries;
                    }
                }
                else
                {
                    text = "-";
                }
                List<string> list = new List<string>();
                for (int i = 0; i < item.Modes.Length; i++)
                {
                    if (item.Modes[i].HeartGem)
                    {
                        list.Add("heartgem" + i);
                    }
                }
                if (list.Count <= 0)
                {
                    list.Add("dot");
                }
                IconsCell iconsCell;
                Row row = table.AddRow().Add(new TextCell(Dialog.Clean(areaData.Name), new Vector2(1f, 0.5f), 0.6f, TextColor)).Add(null)
                    .Add(iconsCell = new IconsCell(CompletionIcon(item)));
                if (areaData.CanFullClear)
                {
                    row.Add(new IconsCell(ArchipelagoController.Instance.ProgressionSystem.IsCollectedVisually(new AreaKey(item.ID_Safe), CollectableType.CASSETTE) ? "cassette" : "dot"));
                    row.Add(new IconsCell(-32f, list.ToArray()));
                }
                else
                {
                    iconsCell.SpreadOverColumns = 3;
                    row.Add(null).Add(null);
                }
                row.Add(new TextCell(text, TextJustify, 0.5f, TextColor));
                if (areaData.IsFinal_Safe)
                {
                    row.Add(new TextCell(Dialog.Deaths(item.Modes[0].Deaths), TextJustify, 0.5f, TextColor)
                    {
                        SpreadOverColumns = SaveData.Instance.UnlockedModes
                    });
                    for (int j = 0; j < SaveData.Instance.UnlockedModes - 1; j++)
                    {
                        row.Add(null);
                    }
                }
                else
                {
                    for (int k = 0; k < SaveData.Instance.UnlockedModes; k++)
                    {
                        if (areaData.HasMode((AreaMode)k))
                        {
                            row.Add(new TextCell(Dialog.Deaths(item.Modes[k].Deaths), TextJustify, 0.5f, TextColor));
                        }
                        else
                        {
                            row.Add(new TextCell("-", TextJustify, 0.5f, TextColor));
                        }
                    }
                }
                if (item.TotalTimePlayed > 0)
                {
                    row.Add(new TextCell(Dialog.Time(item.TotalTimePlayed), TextJustify, 0.5f, TextColor));
                }
                else
                {
                    row.Add(new IconCell("dot"));
                }
            }
            if (table.Rows > 1)
            {
                table.AddRow();
                Row row2 = table.AddRow().Add(new TextCell(Dialog.Clean("journal_totals"), new Vector2(1f, 0.5f), 0.7f, TextColor)).Add(null)
                    .Add(null)
                    .Add(null)
                    .Add(null)
                    .Add(new TextCell(SaveData.Instance.TotalStrawberries_Safe.ToString(), TextJustify, 0.6f, TextColor));
                row2.Add(new TextCell(Dialog.Deaths(SaveData.Instance.TotalDeaths), TextJustify, 0.6f, TextColor)
                {
                    SpreadOverColumns = SaveData.Instance.UnlockedModes
                });
                for (int l = 1; l < SaveData.Instance.UnlockedModes; l++)
                {
                    row2.Add(null);
                }
                row2.Add(new TextCell(Dialog.Time(SaveData.Instance.Time), TextJustify, 0.6f, TextColor));
                table.AddRow();
            }
        }

        private string CompletionIcon(AreaStats data)
        {
            if (!AreaData.Get(data.ID_Safe).CanFullClear && data.Modes[0].Completed)
            {
                return "beat";
            }
            if (data.Modes[0].FullClear)
            {
                return "fullclear";
            }
            if (data.Modes[0].Completed)
            {
                return "clear";
            }
            return "dot";
        }

        public override void Redraw(VirtualRenderTarget buffer)
        {
            base.Redraw(buffer);
            Draw.SpriteBatch.Begin();
            table.Render(new Vector2(60f, 20f));
            Draw.SpriteBatch.End();
        }

        private void DrawIcon(Vector2 pos, bool obtained, string icon)
        {
            if (obtained)
            {
                MTN.Journal[icon].DrawCentered(pos);
            }
            else
            {
                MTN.Journal["dot"].DrawCentered(pos);
            }
        }
    }
}

using Monocle;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Celeste.Mod.CelesteArchipelago
{
    internal class DebugCommands
    {

        [Command("loadscreen", "loads a given level in an area + mode")]
        private static void CmdLoadLevel(string sid, string room, string mode = "0")
        {
            if(!int.TryParse(mode, out var intMode))
            {
                return;
            }
            if (sid == null)
            {
                Engine.Commands.Log("Please specify a map ID or SID.");
                return;
            }
            AreaData areaData = AreaData.Get(sid);
            MapData mapData = null;
            if (areaData?.Mode.Length > intMode)
            {
                mapData = ((areaData == null) ? null : areaData.Mode[intMode]?.MapData);
            }
            if (areaData == null)
            {
                Engine.Commands.Log("Map " + sid + " does not exist!");
                return;
            }
            if (mapData == null)
            {
                Engine.Commands.Log($"Map {sid} has no {mode} mode!");
                return;
            }
            if (room != null)
            {
                List<LevelData> levels = mapData.Levels;
                if (levels != null && levels.All((LevelData level) => level.Name != room))
                {
                    Engine.Commands.Log($"Map {sid} / mode {mode} has no room named {room}!");
                    return;
                }
            }

            AreaKey areaKey = new AreaKey(areaData.ID, (AreaMode)intMode);
            SaveData.Instance.LastArea_Safe = areaKey;
            Session session = new Session(areaKey);
            if (room != null && session.MapData.Get(room) != null)
            {
                if (AreaData.GetCheckpoint(areaKey, room) != null)
                {
                    session = new Session(areaKey, room)
                    {
                        StartCheckpoint = null
                    };
                }
                else
                {
                    session.Level = room;
                }
                session.StartedFromBeginning = (session.FirstLevel = room == session.MapData.StartLevel().Name);
            }
            Engine.Scene = new LevelLoader(session);
        }

        [Command("dumplocations", "dumps a file containing collectable screens")]
        private static void CmdDumpLocations(string filename = "locations.txt")
        {
            try
            {
                ArchipelagoNetworkItem item;
                string levelString, entityString, path;
                List<string> lines = new List<string>();
                lines.Add($"[8061000] Level 1 A-Side Crystal Heart : loadscreen Celeste/1-ForsakenCity s1 0");
                using (var writer = new StreamWriter(filename))
                {
                    MapData mapData;
                    for (int area = 0; area < AreaData.Areas.Count; area++)
                    {
                        for (int mode = 0; mode < AreaData.Areas[area].Mode.Count(); mode++)
                        {
                            levelString = $"Level {area} {"ABC"[mode]}-Side";
                            mapData = AreaData.Areas[area].Mode[mode].MapData;
                            path = AreaData.Areas[area].SID;
                            item = new ArchipelagoNetworkItem(CollectableType.COMPLETION, area, mode);
                            entityString = "Completion";
                            foreach (var level in mapData.Levels)
                            {
                                foreach (var entity in level.Entities)
                                {
                                    switch(entity.Name)
                                    {
                                        case "blackGem":
                                            item = new ArchipelagoNetworkItem(CollectableType.HEARTGEM, area, mode);
                                            entityString = "Crystal Heart";
                                            break;
                                        case "cassette":
                                            item = new ArchipelagoNetworkItem(CollectableType.CASSETTE, area, mode);
                                            entityString = "Cassette";
                                            break;
                                        case "heartGemDoor":
                                            item = new ArchipelagoNetworkItem(CollectableType.HEARTGEM, area, mode);
                                            item.offset = 1;
                                            entityString = "Crystal Heart Door";
                                            break;
                                        case "strawberry":
                                            item = new ArchipelagoNetworkItem(CollectableType.STRAWBERRY, area, mode, new EntityID(entity.Level.Name, entity.ID));
                                            entityString = $"Strawberry {item.offset + 1}";
                                            break;
                                        default: continue;
                                    }
                                    lines.Add($"[{item.ID}] {levelString} {entityString} : loadscreen {path} {level.Name} {mode}");
                                }
                                item = new ArchipelagoNetworkItem(CollectableType.COMPLETION, area, mode);
                                entityString = $"Completion : loadscreen {path} {level.Name} {mode}";
                            }
                            lines.Add($"[{item.ID}] {levelString} {entityString}");
                        }
                    }
                    lines.Sort(StringComparer.OrdinalIgnoreCase);
                    foreach (string line in lines)
                    {
                        writer.WriteLine(line.Substring(10));
                    }
                }
            }
            catch(Exception e)
            {
                Engine.Commands.Log(e.ToString());
            }
        }

    }
}

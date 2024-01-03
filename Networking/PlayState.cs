using System.Text;

namespace Celeste.Mod.CelesteArchipelago
{
    public class PlayState
    {
        public bool IsOverworld;
        public AreaKey AreaKey;
        public string Room;
        public int CheckpointValue;

        public PlayState(string encoded)
        {
            string[] split = encoded.Split(';');
            if(split.Length > 0)
            {
                IsOverworld = split[0] == "1";
            }
            if (split.Length > 2)
            {
                if(int.TryParse(split[1], out int area) && int.TryParse(split[2], out int mode))
                {
                    AreaKey = new AreaKey(area, (AreaMode)mode);
                }
            }
            if(split.Length > 3)
            {
                Room = split[3];
            }
        }

        public PlayState(bool isOverworld, AreaKey areaKey, string room)
        {
            IsOverworld = isOverworld;
            this.AreaKey = areaKey;
            this.Room = room;
        }

        public bool DoTutorial()
        {
            return Room == "dotutorial";
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(IsOverworld ? "1" : "0");
            sb.Append(";");
            sb.Append(AreaKey.ID);
            sb.Append(";");
            sb.Append((int)AreaKey.Mode);
            sb.Append(";");
            sb.Append(Room);
            return sb.ToString();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class FlagStorage
    {
        private HashSet<int> storage = new HashSet<int>();

        public bool IsFlagged(AreaKey key)
        {
            var hash = ComputeHash(key);
            return storage.Contains(hash);
        }

        public void Flag(AreaKey key)
        {
            var hash = ComputeHash(key);
            storage.Add(hash);
        }

        public void UnFlag(AreaKey key)
        {
            var hash = ComputeHash(key);
            storage.Remove(hash);
        }

        public int GetTotal()
        {
            return storage.Count;
        }

        public void Clear()
        {
            storage.Clear();
        }

        private int ComputeHash(AreaKey key)
        {
            return key.ID * 101 + (int)key.Mode;
        }
    }
}

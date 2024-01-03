using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class NumberStorage
    {
        private int total = 0;
        private Dictionary<int, int> storage = new Dictionary<int, int>();

        public int Get(AreaKey key)
        {
            var hash = ComputeHash(key);
            if (!storage.ContainsKey(hash)) return 0;
            return storage[hash];
        }

        public void Set(AreaKey key, int value)
        {
            var hash = ComputeHash(key);
            if (storage.ContainsKey(hash)) total -= storage[hash];
            storage[hash] = value;
            total += value;
        }

        public void Increment(AreaKey key)
        {
            var hash = ComputeHash(key);
            if (storage.ContainsKey(hash))
            {
                storage[hash] += 1;
            }
            else
            {
                storage[hash] = 1;
            }
            total += 1;
        }

        public void Decrement(AreaKey key)
        {
            var hash = ComputeHash(key);
            if (storage.ContainsKey(hash))
            {
                storage[hash] -= 1;
            }
            else
            {
                storage[hash] = -1;
            }
            total -= 1;
        }

        public int GetTotal()
        {
            return total;
        }

        public void Clear()
        {
            storage.Clear();
            total = 0;
        }

        private int ComputeHash(AreaKey key)
        {
            return key.ID * 101 + (int)key.Mode;
        }
    }
}

using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.CelesteArchipelago
{
    public class CollectableStorage<T>
    {
        private int total = 0;
        private Dictionary<int, HashSet<T>> storage = new Dictionary<int, HashSet<T>>();

        public bool Contains(AreaKey key, T obj)
        {
            var hash = ComputeHash(key);

            if (!storage.ContainsKey(hash)) return false;
            return storage[hash].Contains(obj);
        }

        public void Put(AreaKey key, T obj)
        {
            if (Contains(key, obj)) return;

            var hash = ComputeHash(key);
            if (!storage.ContainsKey(hash)) storage[hash] = new HashSet<T>();

            storage[hash].Add(obj);
            total++;
        }

        public void Remove(AreaKey key, T obj)
        {
            if (!Contains(key, obj)) return;

            var hash = ComputeHash(key);
            storage[hash].Remove(obj);
            total--;
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

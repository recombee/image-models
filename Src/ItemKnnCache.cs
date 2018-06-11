using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DeepRecommender
{
    class ItemKnnCache
    {
        public Func<string, IEnumerable<(string, float)>> Loader { get; }
        public ConcurrentDictionary<string, Lazy<List<(string, float)>>> ItemsCache { get; } = new ConcurrentDictionary<string, Lazy<List<(string, float)>>>();

        public ItemKnnCache(Func<string, IEnumerable<(string, float)>> loader)
        {
            Loader = loader;
        }

        public float ForItems(string item1, string item2)
        {
            foreach(var (i, v) in ForItem(item1))
            {
                if(i == item2)
                    return v;
            }

            return 0;
        }

        public List<(string, float)> ForItem(string item)
        {
            if(ItemsCache.TryGetValue(item, out var cache) == false)
            {
                cache = ItemsCache.GetOrAdd(string.Intern(item), new Lazy<List<(string, float)>>
                (
                    () => Loader(item).Select(x => (string.Intern(x.Item1), x.Item2)).Take(1000).ToList(),
                    LazyThreadSafetyMode.ExecutionAndPublication
                ));
            }

            return cache.Value;
        }
    }

}
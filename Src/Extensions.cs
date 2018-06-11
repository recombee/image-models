using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DeepRecommender
{
    static class Extensions
    {
        public static T[] LoadJSONArray<T>(this string file, T format)
        {
            var fileContent = File.ReadAllText(file);
            return JsonConvert.DeserializeObject<T[]>(fileContent);
        }

        public static void AddList<K, V>(this Dictionary<K, List<V>> collection, K key, V value)
        {
            if(collection.TryGetValue(key, out var list) == false)
            {
                list = new List<V>();
                collection.Add(key, list);
            }

            list.Add(value);
        }

        public static void Increment<K>(this Dictionary<K, float> collection, K key, float value)
        {
            collection.TryGetValue(key, out var val);
            collection[key] = val + value;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }
    }
}

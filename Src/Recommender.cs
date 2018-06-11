using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepRecommender
{
    class InteractionData
    {
        private static List<(string, float)> EMPTY_INTERACTION_LIST = new List<(string, float)>();

        public Dictionary<string, List<(string, float)>> UserInteractions { get; }

        private volatile HashSet<string> _Users;
        public HashSet<string> Users
        {
            get
            {
                var users = _Users;

                if(users == null)
                {
                    users = new HashSet<string>(UserInteractions.Select(x => x.Key));;

                    //Race-condition duplicates are fine, they have all the same data
                    _Users = users;
                }

                return users;
            }
        }

        public InteractionData(Dictionary<string, List<(string, float)>> userInteractions)
        {
            UserInteractions = userInteractions;
        }

        public List<(string, float)> InteractionsForUser(string user)
        {
            if(UserInteractions.TryGetValue(user, out var userList))
                return userList;
            
            return EMPTY_INTERACTION_LIST;
        }

        public static InteractionData FromDisk(string userInteractionsPath, Func<string, bool> doLoad = null)
        {
            if(doLoad == null)
            {
                doLoad = x => true;
            }

            var userInteractions = new Dictionary<string, List<(string, float)>>();

            var files = Directory.GetFiles(userInteractionsPath);
            var checkedItems = new ConcurrentDictionary<string, bool>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, file =>
            {
                var fileData = file.LoadJSONArray(new { UserId = "", ItemId = "", Value = default(float) });
                foreach(var record in fileData)
                {
                    var item = string.Intern(record.ItemId);
                    if(checkedItems.TryGetValue(item, out var load) == false)
                    {
                        load = doLoad(item);
                        checkedItems.TryAdd(item, load);
                    }

                    if(load == false)
                    {
                        continue;
                    }

                    lock(userInteractions)
                    {
                        userInteractions.AddList(string.Intern(record.UserId), (item, record.Value));
                    }
                }
            });

            return new InteractionData(userInteractions);
        }
    }

    class Recommender
    {
        public ItemKnnCache ItemKnns { get; }
        public InteractionData UserInteractions { get; }

        public Recommender(ItemKnnCache itemKnns, InteractionData userInteractions)
        {
            ItemKnns = itemKnns;
            UserInteractions = userInteractions;
        }

        public (Dictionary<string, float>, Func<List<(string, float)>>) RecommendItemKNNForUser(string user, int K)
        {
            var knnAccumulator = new Dictionary<string, float>();
            var userInteractions = UserInteractions.InteractionsForUser(user);
            foreach(var (currentItem, itemCoefficient) in userInteractions)
            {
                var itemKnns = ItemKnns.ForItem(currentItem);

                int used = 0;
                for(int i = 0; i < itemKnns.Count && used < K; i++)
                {
                    var (item, coeff) = itemKnns[i];

                    if(currentItem == item)
                        continue;

                    var value = itemCoefficient * coeff;
                    if(value > 0)
                    {
                        used++;
                        knnAccumulator.Increment(item, value);
                    }
                }
            }

            return (knnAccumulator, () =>
            {
                var result = new List<(string, float)>(knnAccumulator.Count);
                result.AddRange(knnAccumulator.Select(x => (x.Key, x.Value)));

                //Sort descending by score
                result.Sort((x, y) => y.Item2.CompareTo(x.Item2));
                return result;
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;

namespace DeepRecommender
{
    static class InteractionDownloader_Recombee
    {
        private const string DB_COMMAND = @"SELECT userId, itemId, LEAST(SUM(value), 1.0) AS rating
FROM
  (SELECT userId, itemId, 0.25 AS value
   FROM detail_views
   UNION ALL
   SELECT userId, itemId, 0.75 AS value
   FROM purchases
   UNION ALL
   SELECT userId, itemId, 0.75 AS value
   FROM cart_additions
   UNION ALL
   SELECT userId, itemId, 0.75 AS value
   FROM bookmarks
   UNION ALL
   SELECT userId, itemId, rating AS value
   FROM ratings) AS pg_sucks
GROUP BY userId, itemId
ORDER BY userId
; ";

        public static void DownloadInteractions(string connectionString, string db, string dstDir, Action<string> writeLine)
        {
            writeLine("Creating directory structure ...");
            Directory.CreateDirectory(dstDir);

            writeLine("Quering DB ...");
            using(var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();

                using(var cmd = new NpgsqlCommand("SET work_mem TO '1GB';", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using(var cmd = new NpgsqlCommand($"SET search_path TO '{db}';", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                using(var cmd = new NpgsqlCommand(DB_COMMAND, conn))
                {

                    cmd.CommandTimeout = 100000;
                    var query = cmd.ExecuteEnumerate().Select(x => new
                    {
                        UserId = x.GetString(0),
                        ItemId = x.GetString(1),
                        Value = x.GetDouble(2)
                    });

                    int fileNumber = 0;
                    var cache = new List<object>();

                    void Save()
                    {
                        var data = JsonConvert.SerializeObject(cache);
                        File.WriteAllText(Path.Combine(dstDir, $"data{fileNumber++}.json"), data);

                        cache.Clear();
                    }

                    long total = 0;
                    foreach(var item in query)
                    {
                        if(string.IsNullOrWhiteSpace(item.UserId))
                            continue;

                        if(string.IsNullOrWhiteSpace(item.ItemId))
                            continue;

                        total++;
                        cache.Add(item);

                        if(cache.Count >= 1000000)
                            Save();
                    }

                    Save();

                    writeLine($"Total {total} interactions");
                }
            }
        }

        public static IEnumerable<NpgsqlDataReader> ExecuteEnumerate(this NpgsqlCommand command)
        {
            using(var reader = command.ExecuteReader())
            {
                while(reader.Read())
                {
                    yield return reader;
                }
            }
        }
    }
}

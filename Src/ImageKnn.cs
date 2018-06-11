using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeepRecommender
{
    class ImageKnn
    {
        public const string KnnPath = "Knn";
        public const string VectorsPath = "Vectors";

        public static void ComputeKNN(string basePath, Action<string> writeLine)
        {
            var srcPath = Path.Combine(basePath, VectorsPath) + '\\';
            var dstPath = Path.Combine(basePath, KnnPath) + '\\';

            ComputeKNN(srcPath, dstPath, writeLine);
        }

        public static void ComputeKNN(string srcPath, string dstPath, Action<string> writeLine)
        {
            writeLine("Preparing output directory structure ...");
            Directory.CreateDirectory(dstPath);

            if(dstPath.Last() != '\\' || dstPath.Last() != '/')
            {
                dstPath += "\\";
            }

            writeLine("Loading ...");

            var files = Directory.GetFiles(srcPath);
            var remaining = files.Length;

            var threadNr = Environment.ProcessorCount;
            var itemVectors = new ConcurrentDictionary<string, SparseVector>(threadNr, 100_000);
            Parallel.ForEach
            (
                files,
                new ParallelOptions { MaxDegreeOfParallelism = threadNr },
                file =>
                {
                    var rem = Interlocked.Decrement(ref remaining);
                    if(rem % 100 == 0)
                    {
                        writeLine(rem.ToString());
                    }

                    var data = file.LoadJSONArray(new { I = default(int), V = default(float) });

                    var indices = new int[data.Length];
                    var buffer = new float[data.Length];

                    for(int i = 0; i < data.Length; i++)
                    {
                        buffer[i] = data[i].V;
                        indices[i] = data[i].I;
                    }
                    
                    var itemName = Path.GetFileNameWithoutExtension(file);
                    var res = itemVectors.TryAdd(itemName, new SparseVector(buffer, indices));

                    if(res == false)
                    {
                        throw new Exception("Item already exists!");
                    }
                }
            );

            writeLine("Indexing ...");

            var indexToItem = new string[itemVectors.Count];
            var indexToVector = new SparseVector[itemVectors.Count];
            Parallel.ForEach(itemVectors, (pair, state, index) =>
            {
                indexToItem[index] = pair.Key;
                indexToVector[index] = pair.Value;
            });

            itemVectors = null;

            //Collect garbage and compact LOH heap now so the GC doesn't kick in while computing
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();

            writeLine("Computing ...");
            var N = indexToVector.Length;
            remaining = N;

            Parallel.For
            (0, N, 
            () => new List<(string, float)>(N), 
            (thisItemIndex, loop, localList) =>
            {
                var rem = Interlocked.Decrement(ref remaining);
                if(rem % 100 == 0)
                {
                    writeLine(rem.ToString());
                }

                var thisItem = indexToItem[thisItemIndex];
                var thisVector = indexToVector[thisItemIndex];

                var dst = dstPath + thisItem + ".csv";
                if(File.Exists(dst))
                    return localList;

                var similarities = localList;

                for(int i = 0; i < N; i++)
                {
                    var item = indexToItem[i];
                    var vector = indexToVector[i];
                    var sim = SparseVector.CosSimmilarity(thisVector, vector);

                    similarities.Add((item, sim));
                }

                similarities.Sort((x, y) => y.Item2.CompareTo(x.Item2));

                using(var file = File.Open(dst, FileMode.CreateNew))
                {
                    using(var sw = new StreamWriter(file))
                    {
                        sw.Write("Item");
                        sw.Write(';');
                        sw.Write("Similarity");
                        sw.WriteLine();

                        foreach(var similarity in similarities)
                        {
                            sw.Write(similarity.Item1);
                            sw.Write(';');
                            sw.Write(similarity.Item2);
                            sw.WriteLine();
                        }
                    }
                }

                similarities.Clear();
                return localList;
            },
            x => { }
            );
        }
    }
}

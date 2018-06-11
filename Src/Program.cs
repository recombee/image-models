using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeepRecommender
{
    class Program
    {
        private const string B = @"R:\Downloads\zoot-images\zoot-images\RecTest\";
        private const string img_src = B + "images";
        private const string vectors_dst = B + "vectors";
        private const string knn_dst = B + "knn";

        static void Main(string[] args)
        {
            try
            {
                RunMain(args);
                //RunMain(new []{ "s", @"R:\Downloads\zoot-images\zoot-images\RecTest\knn", @"R:\Downloads\zoot-images\zoot-images\RecTest\images" });
            }
            catch(HelpException e)
            {
                Console.WriteLine(e.Message);
            }
            catch(Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static void RunMain(string[] args)
        {
            Dictionary<string, (string, Action<string[], string>)> modes = null;
            modes = new Dictionary<string, (string, Action<string[], string>)>
            {
                {"v", ("Extracts feature vectors from item images", ExtractFeatureVectors) },
                {"k", ("Computes nearest neighbours for each item", ComputeImageKnn) },
                {"s", ("Shows nearest items in interactive way", ShowSimilarItems) },
                {"d", ("Downloads interactions from DB and precomputes interaction matrix", DownloadInteractions) },
                {"r", ("Recommends items for user in interactive way", RecommendForUser) },

                {
                    "help", ("Prints this help", (_, __) =>
                    {
                        Console.WriteLine("Available commands:");
                        foreach(var (m, (text, _)) in modes)
                        {
                            Console.WriteLine($"({m}) {text}");
                        }

                        Console.WriteLine();
                        Console.WriteLine("Use \"[command] help\" for more info about command parameters.");
                    })
                }
            };

            var mode = args.Length > 0 ? args[0] : "help";
            if(modes.TryGetValue(mode, out var info) == false)
            {
                throw new Exception($"Unknown mode: {mode}! Use help command for more info.");
            }

            info.Item2(args.Skip(1).ToArray(), info.Item1);
        }

        private static void RecommendForUser(string[] args, string info)
        {
            AssertArgs(args, info, "Image K-NN K", "Image KNN directory", "Interactions directory", "Images directory");
            AssertDirExists(args[1]);
            AssertDirExists(args[2]);

            if(int.TryParse(args[0], out var K) == false || K < 1)
            {
                throw new Exception("K needs to be int greater than 0.");
            }

            Console.WriteLine("Loading data ...");
            var itemKnn = VGG16.ItemKnn(args[1]);
            var interactions = InteractionData.FromDisk(args[2]);
            var recommender = new Recommender(itemKnn, interactions);

            Present("user", args[3], userId =>
            {
                if(interactions.Users.Contains(userId) == false)
                    return null;

                var (_, recInvoker) = recommender.RecommendItemKNNForUser(userId, K);
                var recommentaions = recInvoker();

                if(recommentaions.Count < 4)
                {
                    Console.WriteLine("Given user might not have enought interactions.");
                }

                return recommentaions.Select(x => x.Item1);
            });
        }

        private static void DownloadInteractions(string[] args, string info)
        {
            AssertArgs(args, info, "Connection string", "DB name", "Destination directory");
            InteractionDownloader_Recombee.DownloadInteractions(args[0], args[1], args[2], Console.WriteLine);
        }

        static void ShowSimilarItems(string[] args, string info)
        {
            AssertInputOutputDirs(args, info, "Vector directory", "Image directory");

            Present("item", args[1], itemId =>
            {
                var knnPath = Path.Combine(args[0], itemId + ".csv");

                //TODO:Perf: To list is overkill, but I'm lazy
                var knn = VGG16.LoadtemSimilarities(knnPath).ToList();
                if(knn.Count == 0)
                {
                    return null;
                }

                //knn.Sort((x, y) => y.Item2.CompareTo(x.Item2));
                return knn.Select(x => x.Item1);
            });
        }

        static void ExtractFeatureVectors(string[] args, string info)
        {
            AssertInputOutputDirs(args, info);

            Console.WriteLine("Starting NN backend (might take a while for the first time) ...");
            VGG16.ExtractFeatureVectors(args[0], args[1]);

            Console.WriteLine();
            Console.WriteLine("Done");
        }

        static void ComputeImageKnn(string[] args, string info)
        {
            AssertInputOutputDirs(args, info);
            ImageKnn.ComputeKNN(args[0], args[1], Console.WriteLine);

            Console.WriteLine();
            Console.WriteLine("Done");
        }

        static void Present(string itemName, string imagePath, Func<string, IEnumerable<string>> recommend)
        {
            while(true)
            {
                Console.WriteLine($"Enter {itemName} id (Empty to load from clipboard):");
                var itemId = Console.ReadLine();
                if(string.IsNullOrWhiteSpace(itemId))
                {
                    try
                    {
                        itemId = (string) Clipboard.GetData(DataFormats.Text);
                        if(itemId == null)
                        {
                            Console.WriteLine("Null in clipboard :(");
                            continue;
                        }

                        Console.WriteLine(itemId);
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                }

                var items = recommend(itemId);
                if(items != null)
                {
                    Presenter.DisplayItems(items, imagePath);
                }
                else
                {
                    Console.WriteLine($"Given {itemName} not found.");
                }
            }
        }

        static void AssertInputOutputDirs(string[] args, string info, string inputDir = "Input directory", string outputDir = "Output directory")
        {
            AssertArgs(args, info, inputDir, outputDir);
            AssertDirExists(args[0]);
        }

        static void AssertArgs(string[] args, string info, params string[] argInfo)
        {
            if(args.Length != argInfo.Length)
            {
                var isHelp = args.Length > 0 && args[0] == "help";

                var sb = new StringBuilder();
                if(isHelp)
                {
                    sb.AppendLine(info);
                }
                else
                {
                    sb.AppendLine("Invalid number of arguments!");
                }
                
                sb.AppendLine($"Requires { argInfo.Length} arguments:");

                foreach(var arg in argInfo)
                    sb.AppendLine(arg);

                if(isHelp)
                {
                    throw new HelpException(sb.ToString());
                }

                throw new Exception(sb.ToString());
            }
        }

        static void AssertDirExists(string dir)
        {
            if(Directory.Exists(dir) == false)
            {
                throw new Exception($"Directory not found: {dir}");
            }
        }
    }

    class HelpException : Exception
    {
        public HelpException(string message) :
            base(message)
        { }
    }
}

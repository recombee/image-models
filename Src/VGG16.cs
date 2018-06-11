using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeepRecommender
{
    class VGG16
    {
        private const string VGG_Script = "VGG16_FeatureExtractor.py";

        public static void ExtractFeatureVectors(string srcDir, string outDir)
        {
            if(Directory.Exists(srcDir) == false)
            {
                throw new Exception("Source directory does not exist!");
            }

            var binDir = Path.GetDirectoryName(typeof(VGG16).Assembly.Location);
            var scriptPath = Path.Combine(binDir, VGG_Script);

            if(File.Exists(scriptPath) == false)
            {
                throw new Exception($"Missing script file!{Environment.NewLine}{scriptPath}");
            }

            Directory.CreateDirectory(outDir);

            var start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"{scriptPath} {srcDir} {outDir}",
                UseShellExecute = false
            };

            using(var process = Process.Start(start))
            {
                if(process == null)
                {
                    throw new Exception("Could not start script!");
                }

                process.WaitForExit();
            }
        }

        public static ItemKnnCache ItemKnn(string folder)
        {
            return new ItemKnnCache(itemId =>
            {
                var file = Path.Combine(folder, itemId + ".json");
                return LoadtemSimilarities(file);
            });
        }

        public static IEnumerable<(string, float)> LoadtemSimilarities(string file)
        {
            StreamReader fileStream;
            try
            {
                fileStream = new StreamReader(File.OpenRead(file));
            }
            catch(FileNotFoundException)
            {
                Console.WriteLine($"Warning: Missing K-NN file: {file}");
                yield break;
            }

            try
            {
                string line = fileStream.ReadLine();
                if(line != null && line.Contains(';'))
                {
                    //Note: Skips header line
                    while((line = fileStream.ReadLine()) != null)
                    {
                        if(line.Length == 0)
                            continue;

                        var sep = line.IndexOf(';');

                        var simStr = line.Substring(sep + 1);
                        var itemName = line.Substring(0, sep);

                        if(float.TryParse(simStr, out var deepSim) == false)
                            deepSim = 0;

                        yield return (itemName, deepSim);
                    }

                    yield break;
                }
                    
                //Continue and ...
            }
            finally
            {
                fileStream.Dispose();
            }

            //... parse JSON
            var deepSimilarities = file.LoadJSONArray(new { I = default(string), V = default(float) });
            foreach(var deepSim in deepSimilarities)
            {
                yield return (deepSim.I, deepSim.V);
            }
        }
    }
}

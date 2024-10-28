using System;
using System.IO;
using System.Text.Json;
using Dergwasm.Environments;

namespace DergwasmExtractResoniteApi
{
    public class Program
    {
        ResoniteEnv resoniteEnv;

        Program()
        {
            resoniteEnv = new ResoniteEnv(null, null, null);
        }

        void Run(string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            var api = JsonSerializer.Serialize(resoniteEnv.ApiData, options);
            if (path != null)
            {
                File.WriteAllText(path, api);
            }
            else
            {
                Console.WriteLine(api);
            }
        }

        public static void Main(string[] args)
        {
            new Program().Run(args.Length > 0 ? args[0] : null);
        }
    }
}

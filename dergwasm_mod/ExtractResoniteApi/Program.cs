using System.IO;
using System.Text.Json;
using Derg;
using Derg.Modules;

namespace DergwasmExtractResoniteApi
{
    public class Program
    {
        ResoniteEnv resoniteEnv;

        Program()
        {
            resoniteEnv = new ResoniteEnv(null, null, null);
        }

        void Run()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            File.WriteAllText(
                "../../../../../resonite_api.json",
                JsonSerializer.Serialize(resoniteEnv.ApiData, options)
            );
        }

        public static void Main(string[] args)
        {
            new Program().Run();
        }
    }
}

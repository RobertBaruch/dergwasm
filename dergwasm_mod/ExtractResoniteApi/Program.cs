using System.IO;
using System.Text.Json;
using Derg;
using Derg.Modules;

namespace DergwasmExtractResoniteApi
{
    public class Program
    {
        ResoniteEnv resoniteEnv;
        ReflectedModule<ResoniteEnv> reflected;

        Program()
        {
            resoniteEnv = new ResoniteEnv(null, null, null);
            reflected = new ReflectedModule<ResoniteEnv>(resoniteEnv);
        }

        void Run()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };

            File.WriteAllText(
                "../../../../../resonite_api.json",
                JsonSerializer.Serialize(reflected.ApiData, options)
            );
        }

        public static void Main(string[] args)
        {
            new Program().Run();
        }
    }
}

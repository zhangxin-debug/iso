using Microsoft.Extensions.Configuration;
using System.IO;

namespace ISO11820.Config
{
    public static class AppConfig
    {
        private static IConfiguration? _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    _configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("Config/appsettings.json", optional: false, reloadOnChange: true)
                        .Build();
                }
                return _configuration;
            }
        }

        public static string DatabasePath => Configuration["Database:SqlitePath"] ?? "Data\\ISO11820.db";
        public static string BaseDirectory => Configuration["FileStorage:BaseDirectory"] ?? "D:\\ISO11820";
        public static string TestDataDirectory => Configuration["FileStorage:TestDataDirectory"] ?? "D:\\ISO11820\\TestData";
        public static string ReportDirectory => Configuration["Report:OutputDirectory"] ?? "D:\\ISO11820\\Reports";
        public static double InitialFurnaceTemp => double.Parse(Configuration["Simulation:InitialFurnaceTemp"] ?? "720");
        public static double TargetFurnaceTemp => double.Parse(Configuration["Simulation:TargetFurnaceTemp"] ?? "750");
        public static double HeatingRatePerSecond => double.Parse(Configuration["Simulation:HeatingRatePerSecond"] ?? "40");
        public static double TempFluctuation => double.Parse(Configuration["Simulation:TempFluctuation"] ?? "0.5");
        public static double StableThreshold => double.Parse(Configuration["Simulation:StableThreshold"] ?? "3");
    }
}
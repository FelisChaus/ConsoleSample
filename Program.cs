using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Sample
{
    public class Constants
    {
        public double Pi { get; set; }
        public double E { get; set; }
    }

    public interface ITestService
    {
        void Run();
    }

    public class TestService : ITestService
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly Constants _constants;

        public TestService(ILogger logger,
            IConfiguration config,
            IOptions<Constants> constants)
        {
            _logger = logger;
            _config = config;
            _constants = constants.Value;
        }

        private static string ResourceFileToString(string resourceFilename, Assembly assembly)
        {
            var t = "." + resourceFilename;
            foreach (var f in assembly.GetManifestResourceNames())
            {
                if (!f.EndsWith(t))
                {
                    continue;
                }

                var stream = assembly.GetManifestResourceStream(f);
                if (stream != null)
                {
                    return new StreamReader(stream).ReadToEnd();
                }
            }

            return null;
        }

        public void Run()
        {
            _logger.Information("Version {version}", _config.GetValue<string>("Version"));
            _logger.Information("Pi value is {pi}", _constants.Pi);
            _logger.Information("E  value is {e}", _constants.E);
            _logger.Information("Query is {query}",
                ResourceFileToString("query.sql",
                    Assembly.GetExecutingAssembly()));
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("./appsettings/appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .CreateLogger();

            Log.Logger.Information("Starting: {fullName}", 
                Assembly.GetExecutingAssembly().FullName);

            var host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, configuration) => { configuration.AddConfiguration(config); })
                .ConfigureServices((context, services) =>
                {
                    services.Configure<Constants>(config.GetSection(nameof(Constants)));
                    services.AddTransient<ITestService, TestService>();
                })
                .UseSerilog(logger: Log.Logger)
                .Build();

            var svc = ActivatorUtilities.GetServiceOrCreateInstance<ITestService>(host.Services);
            svc.Run();
        }
    }
}
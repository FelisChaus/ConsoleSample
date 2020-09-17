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
        private readonly Constants _config;

        public TestService(ILogger logger, IOptions<Constants> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public void Run()
        {
            _logger.Information("Pi value is {pi}", _config.Pi);
            _logger.Information("E  value is {e}", _config.E);
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

            Log.Logger.Information($"Starting: {Assembly.GetExecutingAssembly().FullName}");

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.Configure<Constants>(config.GetSection(nameof(Constants)));;
                    services.AddTransient<ITestService, TestService>();
                })
                .UseSerilog(logger: Log.Logger)
                .Build();

            var svc = ActivatorUtilities.GetServiceOrCreateInstance<ITestService>(host.Services);
            svc.Run();
        }
    }
}
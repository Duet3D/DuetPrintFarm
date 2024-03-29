using DuetPrintFarm.Singletons;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DuetPrintFarm
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IJobQueue, JobQueue>();
                    services.AddSingleton<IPrinterList, PrinterList>();

                    services.AddHostedService<Services.PrinterManager>();
                    services.AddHostedService<Services.JobManager>();
                });
    }
}

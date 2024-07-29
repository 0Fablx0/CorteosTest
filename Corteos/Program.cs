using Corteos.Controllers;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Corteos.Controllers.Options;
using Serilog;
using Corteos.Logics;

namespace Corteos
{
    class Program
    {
        private static IHost _host;


        static private void CreateHost()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((host, config) => config
                .AddJsonFile("appSettings.json", true, true))
                .UseSerilog((hostingContext, loggerConfiguration) =>
                {
                    loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration);
                })
                .ConfigureServices((host, services) =>
                {
                    services.Configure<DataBaseOptions>(host.Configuration.GetSection("DataBase"));
                    services.AddSingleton<DBController>();
                    services.AddSingleton<SoapAPI>();
                    services.AddTransient<UnloadingCurrency>();
                })
            .Build();
            _host.Start();

        }

        static async Task Main()
        {
            CreateHost();
            UnloadingCurrency unloadingController = _host.Services.GetRequiredService<UnloadingCurrency>();
            await unloadingController.GetTodayCurrencyAsync();

            DateTime firstDayOfCurrentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var startPeriod = firstDayOfCurrentMonth.AddMonths(-1);
            var endPeriod = firstDayOfCurrentMonth.AddDays(-1);

            await unloadingController.GetCurrencyInTimePeriodAsync(startPeriod, endPeriod);
            return;
        }
    }
}

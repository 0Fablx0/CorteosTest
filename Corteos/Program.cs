using Corteos.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Corteos.Controllers.Options;
using Serilog;
using Serilog.Hosting;
using Serilog.Configuration;
using Microsoft.Extensions.Logging;
using Corteos.Logics;

namespace Corteos
{
    class Program
    {
        private static IHost _host;


        static private void CreateHost()
        {
            //Создание IoC контейнера и добавление туда сервисов
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
            await unloadingController.GetTodayCurrencyAsync(); //Получение выгрузки за день
            await unloadingController.GetMonthCurrencyAsync(); //Получение недостающих выгрузок за текущий месяц
            return;
        }
    }
}

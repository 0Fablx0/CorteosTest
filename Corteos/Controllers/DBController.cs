using Corteos.Controllers.Options;
using Corteos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Corteos.Controllers
{
    class DBController
    {
        private ILogger _logger { get; }
        private DataBaseOptions _options { get; }

        public DBController(IServiceProvider serviceProvider)
        {
            _options = serviceProvider.GetService<IOptions<DataBaseOptions>>().Value;
            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<DBController>();
        }

        private class DBContext : Microsoft.EntityFrameworkCore.DbContext
        {
            private readonly DataBaseOptions _options;
            private readonly ILogger _logger;
            public DbSet<CurrencyModel> currencies { get; set; }
            public DbSet<ExchangeRateModel> exchangerates { get; set; }
            public DBContext(DBController controller)
            {
                _options = controller._options;
                _logger = controller._logger;
                try
                {
                    Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Database connection error: {ex}");
                    throw ex;
                }

            }
            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseNpgsql($"Host={_options.Host};Port={_options.Port};Database={_options.Database};Username={_options.Username};Password={_options.Password}");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<CurrencyModel>().HasKey(curr => curr.num_code);
                modelBuilder.Entity<ExchangeRateModel>().HasKey(exRate => new { exRate.date, exRate.num_code });
            }
        }

        /// <summary>
        /// Запись в БД котировок валют
        /// </summary>
        /// <param name="valutes">Котировки валют</param>
        /// <returns></returns>
        public async Task WriteValutesAsync(List<(CurrencyModel, ExchangeRateModel)> valutes)
        {
            if (valutes?.Count > 0)
            {
                using (DBContext db = new DBContext(this))
                {
                    foreach (var (currency, exchangeRate) in valutes)
                    {
                        if (!await db.currencies.AnyAsync(r => r.num_code == currency.num_code))
                        {
                            await db.currencies.AddAsync(currency);
                        }
                        if (!await db.exchangerates.AnyAsync(r => r.date == exchangeRate.date))
                        {
                            var s = await db.exchangerates.AddAsync(exchangeRate);
                        }
                    }
                    var savedRecords = await db.SaveChangesAsync();
                    _logger.LogInformation($"New records have added to the database : {savedRecords}");
                }
            }
        }

        /// <summary>
        /// Получение дат имеющихся в БД курсов валют за переданный временной промежуток
        /// <list type="number">
        /// <item><param name="startPeriodDate"> Дата начала периода </param></item>
        /// <item><param name="endPeriodDate"> Дата окончания периода </param></item>
        /// </list>
        /// </summary>
        /// <returns> Даты за которые имеются курсы валют</returns>
        public List<DateTime> GetContainedCurrencyDateInPeriod(DateTime startPeriodDate, DateTime endPeriodDate)
        {
            List<DateTime> monthDates;
            using (DBContext db = new DBContext(this))
            {
                monthDates = db.exchangerates.Select(x => x.date).Where(date => date >= startPeriodDate && date <= endPeriodDate).Distinct().ToList();
            }
            _logger.LogInformation($"Database contains {monthDates.Count} dates for which there are exchange rates in the period from {startPeriodDate} to {endPeriodDate}");
            return monthDates;
        }

    }
}

using Corteos.Controllers.Options;
using Corteos.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Corteos.Controllers
{
    class DBController
    {
        private readonly ILogger _logger;
        private readonly DataBaseOptions _options;

        public DBController(IServiceProvider serviceProvider)
        {
            _options = serviceProvider.GetService<IOptions<DataBaseOptions>>().Value;
            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<DBController>();

        }

        private class DBContext : Microsoft.EntityFrameworkCore.DbContext
        {
            private readonly DataBaseOptions _options;
            public DbSet<CurrencyModel> currencies { get; set; }
            public DbSet<ExchangeRateModel> exchangerates { get; set; }
            public DBContext(DataBaseOptions options)
            {
                _options = options;
                Database.EnsureCreated();
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


        public void WriteValutes(List<(CurrencyModel, ExchangeRateModel)> Valutes)
        {
            using (DBContext db = new DBContext(_options))
            {
                foreach (var x in Valutes)
                {
                    if (db.currencies.SingleOrDefault(r => r.num_code == x.Item1.num_code) == null) //проверка на существование идентичного первичного ключа
                    {
                        db.currencies.Add(x.Item1);
                    }
                    if (!db.exchangerates.Any(r => r.date == x.Item2.date)) //проверка на существование идентичной части первичного ключа
                    {
                        db.exchangerates.Add(x.Item2);
                    }
                }
                db.SaveChanges();
            }
        }

        public List<DateTime> GetDaysBeginingMonth()
        {
            DateTime firstMonthDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            List<DateTime> monthDates;

            using (DBContext db = new DBContext(_options))
            {
                monthDates = db.exchangerates.Select(x => x.date).Where(date => date >= firstMonthDate).Distinct().ToList();
            }

            return monthDates;
        }

    }
}

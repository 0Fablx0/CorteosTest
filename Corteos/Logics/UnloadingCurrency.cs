using Corteos.Controllers;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Linq;
using System.Linq;
using Corteos.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

namespace Corteos.Logics
{
    class UnloadingCurrency
    {
        private readonly DBController _dbController;
        private SoapAPI _caller;
        private readonly ILogger _logger;
        public UnloadingCurrency(IServiceProvider serviceProvider, DBController dbController, SoapAPI caller)
        {
            _dbController = dbController;
            _caller = caller;
            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<UnloadingCurrency>();
        }

        public async Task GetTodayCurrencyAsync(DateTime? requestDate = null)
        {
            if (requestDate == null)
            {
                requestDate = DateTime.Now.Date;
            }

            string responce = await _caller.GetQuotesAsync(requestDate?.ToString("dd'/'MM'/'yyyy"));
            var parcedResponce = CurrencyRespParser(responce, requestDate.Value);
            if (parcedResponce != null)
            {
                _dbController.WriteValutes(parcedResponce);
            }

        }

        public async Task GetMonthCurrencyAsync()
        {
            var pastDays = getAllPastDaysMonth();
            var currencyDays = _dbController.GetDaysBeginingMonth();

            foreach (var x in pastDays)
            {
                if (!currencyDays.Contains(x))
                {
                    await GetTodayCurrencyAsync(x);
                }
            }
        }

        private List<DateTime> getAllPastDaysMonth()
        {
            List<DateTime> monthDay = new List<DateTime>();
            for (int i = 1; i <= DateTime.Now.Day; i++)
            {
                monthDay.Add(new DateTime(DateTime.Now.Year, DateTime.Now.Month, i));
            }
            return monthDay;
        }

        private List<(CurrencyModel, ExchangeRateModel)> CurrencyRespParser(string response, DateTime requestDate)
        {
            DateTime updateDate;
            IEnumerable<(CurrencyModel, ExchangeRateModel)> result = null;

            if (string.IsNullOrEmpty(response))
            {
                return result?.ToList();
            }

            XDocument xDoc = XDocument.Parse(response);

            string lastCurrencyUpdateDate = xDoc.Descendants("ValCurs")
                             .FirstOrDefault()?
                             .Attribute("Date")?
                             .Value;

            if (DateTime.TryParse(lastCurrencyUpdateDate, out updateDate) && updateDate == requestDate)
            {
                try
                {
                    result = xDoc.Descendants("Valute")
                   .Select(v => (new CurrencyModel
                   {
                       num_code = int.Parse(v.Element("NumCode")?.Value),
                       char_code = v.Element("CharCode")?.Value,
                       name = v.Element("Name")?.Value
                   },
                   new ExchangeRateModel
                   {
                       num_code = int.Parse(v.Element("NumCode")?.Value),
                       date = requestDate,
                       nominal = int.Parse(v.Element("Nominal")?.Value),
                       value = float.Parse(v.Element("Value")?.Value),
                       vunit_rate = float.Parse(v.Element("VunitRate")?.Value)
                   }));
                }
                catch (Exception e)
                {
                    _logger.LogError($"XML parsing error : {e}");
                    return result?.ToList();
                }
            }

            return result?.ToList();
        }
    }
}

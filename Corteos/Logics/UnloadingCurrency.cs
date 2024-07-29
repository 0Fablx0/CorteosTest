using Corteos.Controllers;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Xml.Linq;
using System.Linq;
using Corteos.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        /// <summary>
        /// Заполнение БД курсами валют за текущий день или за переданную дату
        /// <list type="bullet">
        /// <item><param name="requestDate"> Дата за которю необходима выгрузка</param></item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// Если не передать дату, выполнится выгрузка за текущий день
        /// </remarks>
        /// <returns></returns>
        public async Task GetTodayCurrencyAsync(DateTime? requestDate = null)
        {
            string responce = await _caller.GetQuotesAsync(requestDate ?? DateTime.Now.Date);
            if (string.IsNullOrEmpty(responce)) return;
            var parcedResponce = CurrencyRespParser(responce, (requestDate ?? DateTime.Now.Date));
            if (parcedResponce != null)
            {
                await _dbController.WriteValutesAsync(parcedResponce);
            }
        }

        /// <summary>
        /// Заполнение БД курсами валют за выбранный период времени
        /// <list type="number">
        /// <item><param name="startPeriodDate"> Дата начала периода заполнения</param></item>
        /// <item><param name="endPeriodDate"> Дата окончания периода заполнения</param></item>
        /// </list>
        /// </summary>
        /// <returns></returns>
        public async Task GetCurrencyInTimePeriodAsync(DateTime startPeriodDate, DateTime endPeriodDate)
        {
            _logger.LogInformation($"Upload requested from {startPeriodDate} to {endPeriodDate}");

            endPeriodDate = endPeriodDate > DateTime.Now ? DateTime.Now : endPeriodDate;
            var daysInSelectPeriod = GetAllDaysInPeriod(startPeriodDate, endPeriodDate);

            if (daysInSelectPeriod == null || daysInSelectPeriod.Count == 0) return;

            var currencyDays = _dbController.GetContainedCurrencyDateInPeriod(startPeriodDate, endPeriodDate);
            var missingDays = daysInSelectPeriod.Except(currencyDays);

            foreach (var x in missingDays)
            {
                await GetTodayCurrencyAsync(x);
            }
        }

        private List<DateTime> GetAllDaysInPeriod(DateTime startPeriodDate, DateTime endPeriodDate)
        {
            return Enumerable.Range(0, (endPeriodDate - startPeriodDate).Days + 1)
                             .Select(offset => startPeriodDate.AddDays(offset))
                             .ToList();
        }

        private List<(CurrencyModel, ExchangeRateModel)> CurrencyRespParser(string response, DateTime requestDate)
        {
            XDocument xDoc = XDocument.Parse(response);
            string lastCurrencyUpdateDate = xDoc.Descendants("ValCurs")
                                                .FirstOrDefault()
                                                .Attribute("Date")?
                                                .Value;

            if (DateTime.TryParse(lastCurrencyUpdateDate, out var updateDate) && updateDate == requestDate)
            {
                try
                {
                    return xDoc.Descendants("Valute")
                   .Select(v => (new CurrencyModel
                   {
                       num_code = int.Parse(v.Element("NumCode").Value),
                       char_code = v.Element("CharCode").Value,
                       name = v.Element("Name").Value
                   },
                   new ExchangeRateModel
                   {
                       num_code = int.Parse(v.Element("NumCode").Value),
                       date = requestDate,
                       nominal = int.Parse(v.Element("Nominal").Value),
                       value = float.Parse(v.Element("Value").Value),
                       vunit_rate = float.Parse(v.Element("VunitRate").Value)
                   })).ToList();
                }
                catch (Exception e)
                {
                    _logger.LogError($"XML parsing error (requestDate:{requestDate}) : {e}");
                    return null;
                }
            }

            if (updateDate != requestDate) _logger.LogInformation($"Exchange rate has not been updated for the {requestDate}");

            return null;
        }
    }
}

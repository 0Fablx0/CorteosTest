using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Corteos.Controllers
{
    class SoapAPI
    {
        private string _urlForeginCurrency = "http://www.cbr.ru/scripts/XML_daily.asp?date_req=";
        private readonly ILogger _logger;

        public SoapAPI(IServiceProvider serviceProvider)
        {
            _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<SoapAPI>();
        }

        /// <summary>
        /// Запрос валютных котировок у ЦБР на момент переданной даты
        /// </summary>
        /// <param name="receivingDate"> Дата для получения котировок</param>
        /// <returns>XML документ в виде строки</returns>
        public async Task<string> GetQuotesAsync(DateTime receivingDate)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(_urlForeginCurrency + receivingDate.ToString("dd'/'MM'/'yyyy"));

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"CBR received the exchange rate for {receivingDate}");
                        return await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        _logger.LogError($"Error response from CBR : {response.RequestMessage}");
                        return string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception while getting quotes: {ex}");
                    return string.Empty;
                }

            }
        }
    }
}


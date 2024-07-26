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

        public async Task<string> GetQuotesAsync(string receivingDate)
        {

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(_urlForeginCurrency + receivingDate);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    _logger.LogError($"Error response from CBR : {response.RequestMessage}");
                    return string.Empty;
                }
            }
        }
    }
}


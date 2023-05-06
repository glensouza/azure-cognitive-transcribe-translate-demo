using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API
{
    public class WeatherForecastFunction
    {
        private readonly ILogger logger;

        public WeatherForecastFunction(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<WeatherForecastFunction>();
        }

        [Function("WeatherForecast")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            Random randomNumber = new();
            int temp = 0;

            WeatherForecast[] result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = temp = randomNumber.Next(-20, 55),
                Summary = GetSummary(temp)
            }).ToArray();

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.WriteAsJsonAsync(result);

            return response;
        }

        private static string GetSummary(int temp)
        {
            string summary = temp switch
            {
                >= 32 => "Hot",
                <= 16 and > 0 => "Cold",
                <= 0 => "Freezing",
                _ => "Mild"
            };

            return summary;
        }
    }
}

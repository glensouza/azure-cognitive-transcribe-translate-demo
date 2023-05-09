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
        private readonly SignalRHub signalRHub;

        public WeatherForecastFunction(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<WeatherForecastFunction>();
            this.signalRHub = new SignalRHub(loggerFactory);
        }

        [Function("WeatherForecast")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            this.logger.LogInformation("HERE");

            //string? userId = req.Headers.GetValues("x-ms-client-principal-id").First();
            //this.signalRHub.SendTranscription(new SignalRNotification { Record = "Hello World", UserId = userId });
            //this.signalRHub.SendTranslation(new SignalRNotification { Record = "Hello World", UserId = userId });
            //this.signalRHub.SendTranscription(new SignalRNotification { Record = "Hello World" });
            //this.signalRHub.SendTranslation(new SignalRNotification { Record = "Hello World" });

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

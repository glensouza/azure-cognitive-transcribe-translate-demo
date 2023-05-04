using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace TranscribeTranslateDemo.API
{
    public class Settings
    {
        private readonly ILogger logger;

        public Settings(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<Settings>();
        }

        [Function("Settings")]
        public Shared.Settings Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            this.logger.LogInformation("C# HTTP trigger function processed a request.");

            return new Shared.Settings { Test = "Test" };
        }
    }
}
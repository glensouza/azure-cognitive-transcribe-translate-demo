using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TranscribeTranslateDemo.API
{
    public class Settings
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public Settings(ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            this.logger = loggerFactory.CreateLogger<Settings>();
            this.configuration = configuration;
        }

        [Function("Settings")]
        public Shared.Settings Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            this.logger.LogInformation("C# HTTP trigger function processed a request.");

            return new Shared.Settings { Test = this.configuration.GetValue<string>("FUNCTIONS_WORKER_RUNTIME") };
        }
    }
}

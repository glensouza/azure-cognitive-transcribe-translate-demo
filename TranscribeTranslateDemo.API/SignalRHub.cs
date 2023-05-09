using System.Net;
using System.Net.Http.Json;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Azure;
using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API
{
    public class SignalRHub
    {
        private readonly ILogger logger;
        private static readonly HttpClient HttpClient = new();
        private static string Etag = string.Empty;
        private static int StarCount = 0;

        public SignalRHub(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<SignalRHub>();
        }

        [Function("negotiate")]
        public string Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "notifications", UserId = "{headers.x-ms-client-principal-id}", ConnectionStringSetting = "AzureSignalRConnectionString")] string connectionInfo)
            //[SignalRConnectionInfoInput(HubName = "notifications", ConnectionStringSetting = "AzureSignalRConnectionString")] string connectionInfo)
        {
            this.logger.LogInformation($"SignalR Connection Info = '{connectionInfo}'");
            return connectionInfo;
        }

        [Function("transcriptionNotification")]
        [SignalROutput(HubName = "notifications")]
        public SignalRMessageAction SendTranscription([HttpTrigger(AuthorizationLevel.Anonymous, "post")] SignalRNotification transcription)
        {
            this.logger.LogInformation("SignalR Transcription = '{0}'", transcription.Record);
            SignalRMessageAction signalRMessage = new("transcription")
            {
                Arguments = new[] { transcription.Record },
                UserId = transcription.UserId
            };
            return signalRMessage;
        }

        [Function("translationNotification")]
        [SignalROutput(HubName = "notifications")]
        public SignalRMessageAction SendTranslation([HttpTrigger(AuthorizationLevel.Anonymous, "post")] SignalRNotification translation)
        {
            string userId = "";
            string message = "";
            //this.logger.LogInformation("SignalR Translation = '{0}'", translation.Record);
            //return new SignalRMessageAction("translation")
            //{
            //    Arguments = new[] { translation.Record },
            //    UserId = translation.UserId
            //};
            return new SignalRMessageAction("translation")
            {
                Arguments = new[] { message }, //translation.Record
                UserId = userId //translation.UserId
            };
        }
    }
}

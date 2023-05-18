using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API
{
    public class SignalRHub
    {
        private readonly ILogger logger;

        public SignalRHub(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<SignalRHub>();
        }

        [Function("negotiate")]
        public string Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(ConnectionStringSetting = "AzureSignalRConnectionString", HubName = "notifications", UserId = "{headers.x-ms-client-principal-id}")] string connectionInfo)
        {
            this.logger.LogInformation("SignalR Connection Info = '{0}'", connectionInfo);
            return connectionInfo;
        }

        [Function("NotificationQueue")]
        [SignalROutput(ConnectionStringSetting = "AzureSignalRConnectionString", HubName = "notifications")]
        public SignalRMessageAction NotificationQueue([QueueTrigger(NotificationTypes.Notification, Connection = "AzureWebJobsStorage")] SignalRNotification notification)
        {
            this.logger.LogInformation("SignalR Notification = '{0}'", notification.Record);
            SignalRMessageAction signalRMessage = new(notification.Target, new object[] { notification.Record }) { UserId = notification.UserId };
            return signalRMessage;
        }
    }
}

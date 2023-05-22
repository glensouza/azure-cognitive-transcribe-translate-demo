using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API;

public class SignalRHub
{
    private readonly ILogger logger;

    public SignalRHub(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<SignalRHub>();
    }

    [FunctionName("negotiate")]
    public SignalRConnectionInfo Negotiate(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
        [SignalRConnectionInfo(ConnectionStringSetting = "AzureSignalRConnectionString", HubName = NotificationTypes.SignalRHubName, UserId = "{headers.x-ms-client-principal-id}")] SignalRConnectionInfo connectionInfo)
    {
        this.logger.LogInformation("SignalR Connection Info = '{0}'", connectionInfo);
        return connectionInfo;
    }

    [FunctionName("SendMessage")]
    public Task SendMessage(
        [QueueTrigger(queueName: NotificationTypes.Notification, Connection = "AzureWebJobsStorage")] string signalRNotification,
        [SignalR(ConnectionStringSetting = "AzureSignalRConnectionString", HubName = NotificationTypes.SignalRHubName)] IAsyncCollector<SignalRMessage> signalRMessages)
    {
        // deserialize the inbound message
        SignalRNotification notification = JsonSerializer.Deserialize<SignalRNotification>(signalRNotification);
        this.logger.LogInformation("SignalR Notification = '{0}'", notification.Record);
        SignalRMessage signalRMessage = new()
        {
            // the message will only be sent to this user ID
            UserId = notification.UserId,
            Target = notification.Target,
            Arguments = new object[] { notification.Record }
        };
        return signalRMessages.AddAsync(signalRMessage);
    }
}

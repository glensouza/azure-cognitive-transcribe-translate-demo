using System.Text;
using System.Text.Json;
using Azure.Storage.Queues;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API;

public class NotificationQueueClient
{
    private readonly QueueClient queueClient;

    public NotificationQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, NotificationTypes.Notification);
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(SignalRNotification signalRNotification)
    {
        string message = JsonSerializer.Serialize(signalRNotification);
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        string notification = Convert.ToBase64String(bytes);
        await this.queueClient.SendMessageAsync(notification);
    }
}

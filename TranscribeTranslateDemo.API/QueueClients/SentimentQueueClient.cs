using Azure.Storage.Queues;
using System.Text;
using System.Text.Json;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueClients;

public class SentimentQueueClient
{
    private readonly QueueClient queueClient;

    public SentimentQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, NotificationTypes.Sentiment);
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rowKey);
        string notification = Convert.ToBase64String(bytes);
        await this.queueClient.SendMessageAsync(notification);
    }
}

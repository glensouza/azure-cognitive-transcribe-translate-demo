using Azure.Storage.Queues;
using System.Text;
using System.Text.Json;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueClients;

public class TranscribeQueueClient
{
    private readonly QueueClient queueClient;

    public TranscribeQueueClient(string storageConnectionString)
    {
        //this.queueClient = new QueueClient(storageConnectionString, NotificationTypes.Transcription);
        this.queueClient = new QueueClient(storageConnectionString, "transcription");
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rowKey);
        string notification = Convert.ToBase64String(bytes);
        await this.queueClient.SendMessageAsync(notification);
    }
}

using Azure.Storage.Queues;
using System.Text;
using System.Text.Json;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueClients;

public class TextToSpeechQueueClient
{
    private readonly QueueClient queueClient;

    public TextToSpeechQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, NotificationTypes.TextToSpeech);
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(rowKey);
        string notification = Convert.ToBase64String(bytes);
        await this.queueClient.SendMessageAsync(notification);
    }
}

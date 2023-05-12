using Azure.Storage.Queues;

namespace TranscribeTranslateDemo.API.QueueClients;

public class SentimentQueueClient
{
    private readonly QueueClient queueClient;

    public SentimentQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, "sentiment");
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        await this.queueClient.SendMessageAsync(rowKey);
    }
}

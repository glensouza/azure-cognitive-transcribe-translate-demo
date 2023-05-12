using Azure.Storage.Queues;

namespace TranscribeTranslateDemo.API.QueueClients;

public class TranslateQueueClient
{
    private readonly QueueClient queueClient;

    public TranslateQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, "translate");
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        await this.queueClient.SendMessageAsync(rowKey);
    }
}

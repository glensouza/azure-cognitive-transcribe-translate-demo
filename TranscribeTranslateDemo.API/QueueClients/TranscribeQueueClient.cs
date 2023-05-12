using Azure.Storage.Queues;

namespace TranscribeTranslateDemo.API.QueueClients;

public class TranscribeQueueClient
{
    private readonly QueueClient queueClient;

    public TranscribeQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, "transcribe");
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        await this.queueClient.SendMessageAsync(rowKey);
    }
}

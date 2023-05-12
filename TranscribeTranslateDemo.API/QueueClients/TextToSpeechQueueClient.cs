using Azure.Storage.Queues;

namespace TranscribeTranslateDemo.API.QueueClients;

public class TextToSpeechQueueClient
{
    private readonly QueueClient queueClient;

    public TextToSpeechQueueClient(string storageConnectionString)
    {
        this.queueClient = new QueueClient(storageConnectionString, "texttospeech");
        this.queueClient.CreateIfNotExists();
    }

    public async Task SendMessageAsync(string rowKey)
    {
        await this.queueClient.SendMessageAsync(rowKey);
    }
}

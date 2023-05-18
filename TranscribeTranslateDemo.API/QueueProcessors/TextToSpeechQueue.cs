using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueProcessors;

public class TextToSpeechQueue
{
    private readonly ILogger logger;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly NotificationQueueClient notificationQueueClient;

    public TextToSpeechQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, NotificationQueueClient notificationQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<TextToSpeechQueue>();
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.notificationQueueClient = notificationQueueClient;
    }

    [Function("TextToSpeechQueue")]
    public async Task Run([QueueTrigger(NotificationTypes.TextToSpeech, Connection = "AzureWebJobsStorage")] string rowKey)
    {
        this.logger.LogInformation($"C# ServiceBus queue trigger function processed message: {rowKey}");

        DemoEntity? demo = await this.tableClient.GetEntityAsync<DemoEntity>("Demo", rowKey);
        if (demo == null)
        {
            return;
        }

        BlobClient? cloudBlockBlob = this.blobContainerClient.GetBlobClient($"{rowKey}.flac");
        if (cloudBlockBlob == null)
        {
            return;
        }

        SignalRNotification notification = new()
        {
            Target = NotificationTypes.TextToSpeech,
            Record = $"TEXT TO SPEECH MESSAGE {rowKey}",
            UserId = demo.UserId
        };
        await this.notificationQueueClient.SendMessageAsync(notification);
    }
}

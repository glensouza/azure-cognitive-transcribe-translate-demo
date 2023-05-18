using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueProcessors;

public class TranscribeQueue
{
    private readonly ILogger logger;
    private readonly SignalRHub signalRHub;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly TranslateQueueClient translateQueueClient;
    private readonly SentimentQueueClient sentimentQueueClient;
    private readonly NotificationQueueClient notificationQueueClient;

    public TranscribeQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, TranslateQueueClient translateQueueClient, SentimentQueueClient sentimentQueueClient, NotificationQueueClient notificationQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<TranscribeQueue>();
        this.signalRHub = new SignalRHub(loggerFactory);
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.translateQueueClient = translateQueueClient;
        this.sentimentQueueClient = sentimentQueueClient;
        this.notificationQueueClient = notificationQueueClient;
    }

    [Function("TranscribeQueue")]
    public async Task Run([QueueTrigger(NotificationTypes.Transcription, Connection = "AzureWebJobsStorage")] string rowKey)
    {
        this.logger.LogInformation($"C# Queue trigger function processed: {rowKey}");

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
            Target = NotificationTypes.Transcription,
            Record = $"TRANSCRIPTION MESSAGE {rowKey}",
            UserId = demo.UserId
        };
        await this.notificationQueueClient.SendMessageAsync(notification);

        await this.translateQueueClient.SendMessageAsync(rowKey);
        await this.sentimentQueueClient.SendMessageAsync(rowKey);
    }
}

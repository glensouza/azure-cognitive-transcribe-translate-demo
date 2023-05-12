using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API;

public class TranscribeQueue
{
    private readonly ILogger logger;
    private readonly SignalRHub signalRHub;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly TranslateQueueClient translateQueueClient;
    private readonly SentimentQueueClient sentimentQueueClient;

    public TranscribeQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, TranslateQueueClient translateQueueClient, SentimentQueueClient sentimentQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<TranscribeQueue>();
        this.signalRHub = new SignalRHub(loggerFactory);
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.translateQueueClient = translateQueueClient;
        this.sentimentQueueClient = sentimentQueueClient;
    }

    [Function("TranscribeQueue")]
    public async Task Run([QueueTrigger("transcribe", Connection = "AzureWebJobsStorage")] string rowKey)
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
            Record = "TRANSCRIPTION MESSAGE TEST",
            UserId = demo.UserId
        };
        this.signalRHub.SendNotification(notification, NotificationTypes.Transcription);

        await this.translateQueueClient.SendMessageAsync(rowKey);
        await this.sentimentQueueClient.SendMessageAsync(rowKey);
    }
}

using System;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueProcessors;

public class SentimentQueue
{
    private readonly ILogger logger;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly NotificationQueueClient notificationQueueClient;

    public SentimentQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, NotificationQueueClient notificationQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<SentimentQueue>();
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.notificationQueueClient = notificationQueueClient;
    }

    [Function("SentimentQueue")]
    public async Task Run([QueueTrigger(NotificationTypes.Sentiment, Connection = "AzureWebJobsStorage")] string rowKey)
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
            Target = NotificationTypes.Sentiment,
            Record = $"SENTIMENT MESSAGE {rowKey}",
            UserId = demo.UserId
        };
        await this.notificationQueueClient.SendMessageAsync(notification);
    }
}

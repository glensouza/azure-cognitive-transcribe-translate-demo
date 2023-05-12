using System;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API;

public class SentimentQueue
{
    private readonly ILogger logger;
    private readonly SignalRHub signalRHub;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;

    public SentimentQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient)
    {
        this.logger = loggerFactory.CreateLogger<SentimentQueue>();
        this.signalRHub = new SignalRHub(loggerFactory);
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
    }

    [Function("SentimentQueue")]
    public async Task Run([QueueTrigger("sentiment", Connection = "AzureWebJobsStorage")] string rowKey)
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
            Record = "SENTIMENT MESSAGE TEST",
            UserId = demo.UserId
        };
        this.signalRHub.SendNotification(notification, "sentiment");
    }
}

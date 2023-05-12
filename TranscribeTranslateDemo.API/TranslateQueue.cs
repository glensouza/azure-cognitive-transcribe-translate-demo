using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API;

public class TranslateQueue
{
    private readonly ILogger logger;
    private readonly SignalRHub signalRHub;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly TextToSpeechQueueClient textToSpeechQueueClient;

    public TranslateQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, TextToSpeechQueueClient textToSpeechQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<TranslateQueue>();
        this.signalRHub = new SignalRHub(loggerFactory);
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.textToSpeechQueueClient = textToSpeechQueueClient;
    }

    [Function("TranslateQueue")]
    public async Task Run([ServiceBusTrigger("translate", Connection = "AzureWebJobsStorage")] string rowKey)
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
            Record = "TRANSLATE MESSAGE TEST",
            UserId = demo.UserId
        };
        this.signalRHub.SendNotification(notification, "translate");

        await this.textToSpeechQueueClient.SendMessageAsync(rowKey);
    }
}

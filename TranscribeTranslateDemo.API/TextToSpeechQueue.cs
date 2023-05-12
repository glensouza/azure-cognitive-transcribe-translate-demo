using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API;

public class TextToSpeechQueue
{
    private readonly ILogger logger;
    private readonly SignalRHub signalRHub;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;

    public TextToSpeechQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient)
    {
        this.logger = loggerFactory.CreateLogger<TextToSpeechQueue>();
        this.signalRHub = new SignalRHub(loggerFactory);
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
    }

    [Function("TextToSpeechQueue")]
    public async Task Run([ServiceBusTrigger("texttospeech", Connection = "AzureWebJobsStorage")] string rowKey)
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
            Record = "TEXT TO SPEECH MESSAGE TEST",
            UserId = demo.UserId
        };
        this.signalRHub.SendNotification(notification, "textToSpeech");
    }
}

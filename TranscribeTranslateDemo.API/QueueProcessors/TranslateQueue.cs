using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using TranscribeTranslateDemo.API.Entities;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

namespace TranscribeTranslateDemo.API.QueueProcessors;

public class TranslateQueue
{
    private readonly ILogger logger;
    private readonly SignalRHub signalRHub;
    private readonly TableClient tableClient;
    private readonly BlobContainerClient blobContainerClient;
    private readonly TextToSpeechQueueClient textToSpeechQueueClient;
    private readonly NotificationQueueClient notificationQueueClient;

    public TranslateQueue(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, TextToSpeechQueueClient textToSpeechQueueClient, NotificationQueueClient notificationQueueClient)
    {
        this.logger = loggerFactory.CreateLogger<TranslateQueue>();
        this.signalRHub = new SignalRHub(loggerFactory);
        this.tableClient = tableClient;
        this.blobContainerClient = blobClient;
        this.textToSpeechQueueClient = textToSpeechQueueClient;
        this.notificationQueueClient = notificationQueueClient;
    }

    [Function("TranslateQueue")]
    public async Task Run([QueueTrigger(NotificationTypes.Translation, Connection = "AzureWebJobsStorage")] string rowKey)
    {
        this.logger.LogInformation("C# ServiceBus queue trigger function processed message: {0}", rowKey);

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
            Target = NotificationTypes.Translation,
            Record = $"TRANSLATE MESSAGE {rowKey}",
            UserId = demo.UserId
        };
        await this.notificationQueueClient.SendMessageAsync(notification);

        await this.textToSpeechQueueClient.SendMessageAsync(rowKey);
    }
}

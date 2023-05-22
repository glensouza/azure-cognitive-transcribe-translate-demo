using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
//using Microsoft.WindowsAzure.Storage.Blob;
using Azure.Data.Tables;
using TranscribeTranslateDemo.Shared;
using System.IO;
using System.Web.Http;
using Xabe.FFmpeg.Enums;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TranscribeTranslateDemo.API;

public class TranscribeFunction
{
    private readonly NotificationQueueClient notificationQueueClient;
    private readonly TranscribeQueueClient transcribeQueueClient;

    public TranscribeFunction(NotificationQueueClient notificationQueueClient, TranscribeQueueClient transcribeQueueClient)
    {
        this.notificationQueueClient = notificationQueueClient;
        this.transcribeQueueClient = transcribeQueueClient;
    }

    [FunctionName("Transcribe")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
        [Table("Transcriptions", Connection = "AzureWebJobsStorage")] TableClient tableClient,
        //[Blob("Transcriptions")] CloudBlobContainer cloudBlobContainer,
        [Blob("transcriptions")] BlobContainerClient blobContainerClient,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        IFormCollection formData = await req.ReadFormAsync();
        string userId = formData["userId"];
        if (string.IsNullOrEmpty(userId) || req.Form.Files.Count == 0)
        {
            return new BadRequestResult();
        }

        IFormFile audioFile = req.Form.Files[0];
        if (audioFile == null || audioFile.Length == 0)
        {
            return new BadRequestResult();
        }

        string languageFrom = formData["languageFrom"];
        if (string.IsNullOrEmpty(languageFrom))
        {
            languageFrom = "en-US";
        }

        string languageTo = formData["languageTo"];
        if (string.IsNullOrEmpty(languageTo))
        {
            languageTo = "es-US";
        }

        string rowKey = Guid.NewGuid().ToString();
        TranscriptionEntity? transcription = null;
        do
        {
            rowKey = Guid.NewGuid().ToString();
            try
            {
                transcription = await tableClient.GetEntityAsync<TranscriptionEntity>("Demo", rowKey);
            }
            catch (Exception )
            {
                // ignore
            }
        }
        while (transcription != null);

        await this.notificationQueueClient.SendMessageAsync(new SignalRNotification
        {
            UserId = userId,
            Target = NotificationTypes.RowKey,
            Record = rowKey
        });

        await using Stream stream = audioFile.OpenReadStream();
        //string outputPath = Path.ChangeExtension(Path.GetTempFileName(), FileExtensions.Mp3);
        //string directoryName = Path.GetDirectoryName(outputPath);
        //string filePath = $"{directoryName}\\{rowKey}.mp3";
        //if (File.Exists(filePath))
        //{
        //    File.Delete(filePath);
        //}

        //using (FileStream file = new(filePath, FileMode.Create, FileAccess.Write))
        //{
        //    byte[] bytes = new byte[stream.Length];
        //    stream.Read(bytes, 0, (int)stream.Length);
        //    file.Write(bytes, 0, bytes.Length);
        //    stream.Close();
        //}


        // TODO: Do I need this?
        await blobContainerClient.CreateIfNotExistsAsync();
        BlobContainerPermissions permissions = new()
        {
            PublicAccess = BlobContainerPublicAccessType.Blob
        };
        await blobContainerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

        //CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference($"{rowKey}.mp3");
        BlobClient cloudBlockBlob = blobContainerClient.GetBlobClient($"{rowKey}.mp3");

        bool fileExists = await cloudBlockBlob.ExistsAsync();
        while (fileExists)
        {
            await cloudBlockBlob.DeleteAsync();
            fileExists = await cloudBlockBlob.ExistsAsync();
        }

        await cloudBlockBlob.UploadAsync(stream);
        string uri = cloudBlockBlob.Uri.AbsoluteUri;
        if (cloudBlockBlob.CanGenerateSasUri)
        {
            // Create a SAS token that's valid for one hour.
            BlobSasBuilder sasBuilder = new()
            {
                BlobContainerName = cloudBlockBlob.GetParentBlobContainerClient().Name,
                BlobName = cloudBlockBlob.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = cloudBlockBlob.GenerateSasUri(sasBuilder);
            log.LogInformation("SAS URI for blob is: {0}", sasUri);

            uri = sasUri.AbsoluteUri;
        }
        else
        {
            //return new ExceptionResult(new Exception("BlobClient must be authorized with Shared Key credentials to create a service SAS."), false);
        }





        //if (cloudBlockBlob.CanGenerateSasUri)
        //{
        //    BlobSasBuilder sasBuilder = new()
        //    {
        //        BlobContainerName = cloudBlockBlob.GetParentBlobContainerClient().Name,
        //        BlobName = cloudBlockBlob.Name,
        //        Resource = "b",
        //        ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
        //    };

        //    sasBuilder.SetPermissions(BlobSasPermissions.Read);

        //    Uri sasUri = cloudBlockBlob.GenerateSasUri(sasBuilder);
        //    uri = sasUri.AbsoluteUri;
        //}

        transcription = new TranscriptionEntity
        {
            RowKey = rowKey,
            UserId = userId,
            SourceAudioFileUrl = uri,
            LanguageFrom = languageFrom,
            LanguageTo = languageTo
        };
        await tableClient.AddEntityAsync(transcription);

        await this.transcribeQueueClient.SendMessageAsync(rowKey);

        return new OkResult();
    }
}

using System.Diagnostics;
using System.Net;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using TranscribeTranslateDemo.API.Entities;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;
using Xabe.FFmpeg.Streams;
using TranscribeTranslateDemo.Shared;
using Azure.Storage.Sas;
using Microsoft.CognitiveServices.Speech.Transcription;

namespace TranscribeTranslateDemo.API
{
    public class Transcribe
    {
        private readonly ILogger logger;
        private readonly SignalRHub signalRHub;
        private readonly TableClient tableClient;
        private readonly BlobContainerClient blobContainerClient;
        private readonly NotificationQueueClient notificationQueueClient;
        private readonly TranscribeQueueClient transcribeQueueClient;

        public Transcribe(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, NotificationQueueClient notificationQueueClient, TranscribeQueueClient transcribeQueueClient)
        {
            this.logger = loggerFactory.CreateLogger<Transcribe>();
            this.signalRHub = new SignalRHub(loggerFactory);
            this.tableClient = tableClient;
            this.blobContainerClient = blobClient;
            this.notificationQueueClient = notificationQueueClient;
            this.transcribeQueueClient = transcribeQueueClient;
        }

        [Function("Transcribe")]
        public async Task<HttpResponseData> TranscribeRequest([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
        {
            this.logger.LogInformation("C# HTTP trigger function processed a request.");

            // get form-body
            MultipartFormDataParser parsedFormBody = await MultipartFormDataParser.ParseAsync(req.Body);
            string? userId = parsedFormBody.GetParameterValue("userId");
            string? languageFrom = parsedFormBody.GetParameterValue("languageFrom") ?? "en-US"; // TODO: languageFrom,
            string? languageTo = parsedFormBody.GetParameterValue("languageTo") ?? "es-US"; // TODO: languageTo,
            if (parsedFormBody.Files.Count == 0 || string.IsNullOrEmpty(userId))
            {
                HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                badRequestResponse.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                await badRequestResponse.WriteStringAsync("Please pass a userId and a file in the request body");
                return badRequestResponse;
            }

            FilePart audioFile = parsedFormBody.Files[0];
            Stream stream = audioFile.Data;

            string? localRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
            this.logger.LogInformation("localRoot: {0}", localRoot);
            string azureRoot = Path.Combine($"{Environment.GetEnvironmentVariable("HOME")}", "site","wwwroot");
            this.logger.LogInformation("azureRoot: {0}", azureRoot);
            string rootPath = localRoot ?? azureRoot;
            this.logger.LogInformation("rootPath: {0}", rootPath);
            FFmpeg.ExecutablesPath = rootPath;

            string outputPath = Path.ChangeExtension(Path.GetTempFileName(), FileExtensions.Mp3);
            this.logger.LogInformation("outputPath: {0}", outputPath);
            string directoryName = Path.GetDirectoryName(outputPath)!;
            this.logger.LogInformation("directoryName: {0}", directoryName);
            string filename = Path.Combine(directoryName, $"{audioFile.FileName}.mp3");
            this.logger.LogInformation("filename: {0}", filename);

            await using (FileStream file = new(filename, FileMode.Create, FileAccess.Write))
            {
                byte[] bytes = new byte[stream.Length];
                _ = await stream.ReadAsync(bytes.AsMemory(0, (int)stream.Length));
                file.Write(bytes, 0, bytes.Length);
                stream.Close();
            }

            string rowKey = Guid.NewGuid().ToString();
            BlobClient? blobClient = this.blobContainerClient.GetBlobClient($"{rowKey}.mp3");
            bool fileExists = await blobClient.ExistsAsync();
            while (fileExists)
            {
                rowKey = Guid.NewGuid().ToString();
                blobClient = this.blobContainerClient.GetBlobClient($"{rowKey}.mp3");
                fileExists = await blobClient.ExistsAsync();
            }

            SignalRNotification notification = new()
            {
                Target = NotificationTypes.RowKey,
                Record = $"{rowKey}",
                UserId = userId
            };
            await this.notificationQueueClient.SendMessageAsync(notification);

            await blobClient.UploadAsync(filename);
            string uri = blobClient.Uri.AbsoluteUri;
            if (blobClient.CanGenerateSasUri)
            {
                BlobSasBuilder sasBuilder = new()
                {
                    BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.AddYears(1)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
                uri = sasUri.AbsoluteUri;
            }

            try
            {
                IMediaInfo inputFile = await MediaInfo.Get(filename).ConfigureAwait(false);

                IAudioStream audioStream = inputFile.AudioStreams.First();

                int sampleRate = audioStream.SampleRate;
                int channels = audioStream.Channels;
                //CodecType codec = audioStream.CodecType;

                if (sampleRate < 41100)
                {
                    audioStream.SetSampleRate(41100);
                }

                if (channels != 1)
                {
                    audioStream.SetChannels(1);
                }

                await Conversion.New().AddStream(audioStream).SetOutput(outputPath).Start().ConfigureAwait(false);

                await using Mp3FileReader mp3 = new(outputPath);
                await using WaveStream pcm = WaveFormatConversionStream.CreatePcmStream(mp3);
                WaveFileWriter.CreateWaveFile(outputPath + ".flac", pcm);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Debugger.Break();
            }

            blobClient = this.blobContainerClient.GetBlobClient($"{rowKey}.flac");
            fileExists = await blobClient.ExistsAsync();
            if (fileExists)
            {
                await blobClient.DeleteAsync();
            }

            await blobClient.UploadAsync(outputPath + ".flac");
            File.Delete(filename);
            File.Delete(outputPath + ".flac");
            File.Delete(outputPath);

            DemoEntity demo = new()
            {
                PartitionKey = "Demo",
                RowKey = rowKey,
                UserId = userId,
                SourceAudioFileUrl = uri,
                LanguageFrom = languageFrom,
                LanguageTo = languageTo,
                Transcription = string.Empty,
                Translation = string.Empty,
                TranslatedAudioFileUrl = string.Empty
            };
            await this.tableClient.AddEntityAsync(demo);

            await this.transcribeQueueClient.SendMessageAsync(rowKey);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync(rowKey);
            return response;
        }
    }
}

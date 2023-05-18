using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using TranscribeTranslateDemo.API.Entities;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;
using Xabe.FFmpeg.Model;
using Xabe.FFmpeg.Streams;
using System;
using TranscribeTranslateDemo.API.QueueClients;
using TranscribeTranslateDemo.Shared;

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
            string? languageTo = parsedFormBody.GetParameterValue("languageTo") ?? "es-MX"; // TODO: languageTo,
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
            string azureRoot = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";
            string rootPath = localRoot ?? azureRoot;

            FFmpeg.ExecutablesPath = rootPath;
            string outputPath = Path.ChangeExtension(Path.GetTempFileName(), FileExtensions.Mp3);
            string directoryName = Path.GetDirectoryName(outputPath)!;
            string filename = $"{directoryName}\\{audioFile.FileName}.mp3";

            await using (FileStream file = new(filename, FileMode.Create, FileAccess.Write))
            {
                byte[] bytes = new byte[stream.Length];
                _ = await stream.ReadAsync(bytes.AsMemory(0, (int)stream.Length));
                file.Write(bytes, 0, bytes.Length);
                stream.Close();
            }

            string rowKey = Guid.NewGuid().ToString();
            BlobClient? cloudBlockBlob = this.blobContainerClient.GetBlobClient($"{rowKey}.mp3");
            bool fileExists = await cloudBlockBlob.ExistsAsync();
            while (fileExists)
            {
                rowKey = Guid.NewGuid().ToString();
                cloudBlockBlob = this.blobContainerClient.GetBlobClient($"{rowKey}.mp3");
                fileExists = await cloudBlockBlob.ExistsAsync();
            }

            SignalRNotification notification = new()
            {
                Target = NotificationTypes.RowKey,
                Record = $"PRE TRANSCRIPTION ROWKEY: {rowKey}",
                UserId = userId
            };
            await this.notificationQueueClient.SendMessageAsync(notification);

            await cloudBlockBlob.UploadAsync(filename);
            string uri = cloudBlockBlob.Uri.AbsoluteUri;
            notification.Target = NotificationTypes.Uri;
            notification.Record = $"PRE TRANSCRIPTION URI: {uri}";
            await this.notificationQueueClient.SendMessageAsync(notification);

            try
            {
                IMediaInfo inputFile = await MediaInfo.Get(filename).ConfigureAwait(false);

                IAudioStream audioStream = inputFile.AudioStreams.First();

                //Debugger.Break();
                int sampleRate = audioStream.SampleRate;
                int channels = audioStream.Channels;
                //CodecType codec = audioStream.CodecType;
                //Debugger.Break();

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

            cloudBlockBlob = this.blobContainerClient.GetBlobClient($"{rowKey}.flac");
            fileExists = await cloudBlockBlob.ExistsAsync();
            if (fileExists)
            {
                await cloudBlockBlob.DeleteAsync();
            }

            await cloudBlockBlob.UploadAsync(outputPath + ".flac");
            File.Delete(filename);
            File.Delete(outputPath + ".flac");
            File.Delete(outputPath);

            DemoEntity demo = new()
            {
                PartitionKey = "Demo",
                RowKey = rowKey,
                UserId = userId,
                AudioFileUrl = uri,
                LanguageFrom = languageFrom,
                LanguageTo = languageTo,
                Sentiment = string.Empty,
                Transcription = string.Empty,
                Translation = string.Empty
            };
            await this.tableClient.AddEntityAsync(demo);

















            //string filename = audioFile.FileName;
            //FileInfo fileInfo = new(Assembly.GetExecutingAssembly().Location);
            //string path = fileInfo.Directory.Parent.FullName;
            //FileStream objfilestream = new(Path.Combine(path, filename + ".mp3"), FileMode.Create, FileAccess.ReadWrite);

            //using (MemoryStream memoryStream = new())
            //{
            //    await stream.CopyToAsync(memoryStream);
            //    objfilestream.Write(memoryStream.ToArray(), 0, (int)memoryStream.Length);
            //    objfilestream.Close();
            //}


            //objfilestream.Write(stream, 0, stream.Length);
            //objfilestream.Close();




            ////DemoEntity demo = new()
            ////{
            ////    PartitionKey = "Hello",
            ////    RowKey = "World",
            ////    Text = "Hello World!"
            ////};
            ////this.tableClient.AddEntity(demo);





            ////string filePath = "sample-file";

            ////// Get a reference to a blob named "sample-file" in a container named "sample-container"
            ////BlobClient blobClient = this.blobContainerClient.GetBlobClient(blobName);

            ////// Upload local file
            ////blobClient.Upload(filePath);





            ////// Get a temporary path on disk where we can download the file
            ////string downloadPath = "hello.jpg";

            ////// Download the public blob at https://aka.ms/bloburl
            ////new BlobClient(new Uri("https://aka.ms/bloburl")).DownloadTo(downloadPath);
            ////// Download the public blob at https://aka.ms/bloburl
            ////await new BlobClient(new Uri("https://aka.ms/bloburl")).DownloadToAsync(downloadPath);





            // Print out all the blob names
            //foreach (BlobItem blob in this.blobContainerClient.GetBlobs())
            //{
            //    Console.WriteLine(blob.Name);
            //}


            await this.transcribeQueueClient.SendMessageAsync(rowKey);



            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync(rowKey);
            return response;
        }
    }
}

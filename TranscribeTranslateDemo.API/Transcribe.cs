using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace TranscribeTranslateDemo.API
{
    public class Transcribe
    {
        private readonly ILogger logger;
        private readonly TableClient tableClient;
        private readonly BlobContainerClient blobContainerClient;
        private readonly QueueClient queueClient;

        public Transcribe(ILoggerFactory loggerFactory, TableClient tableClient, BlobContainerClient blobClient, QueueClient queueClient)
        {
            this.logger = loggerFactory.CreateLogger<Transcribe>();
            this.tableClient = tableClient;
            this.blobContainerClient = blobClient;
            this.queueClient = queueClient;
        }

        [Function("Transcribe")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            this.logger.LogInformation("C# HTTP trigger function processed a request.");

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
            foreach (BlobItem blob in this.blobContainerClient.GetBlobs())
            {
                Console.WriteLine(blob.Name);
            }






            this.queueClient.SendMessage("Hello World!");


            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);
            //response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            //response.WriteString("Welcome to Azure Functions!");

            return response;
        }

        //[Function("FunctionQ")]
        //public void Run([QueueTrigger("Demo", Connection = "AzureWebJobsStorage")] string myQueueItem)
        //{
        //    this.logger.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        //}
    }
}

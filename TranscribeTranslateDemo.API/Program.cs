using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TranscribeTranslateDemo.API;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

string storageConnectionString = config.GetValue<string>("AzureWebJobsStorage");

IHost host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(s =>
    {
        s.AddSingleton(_ =>
        {
            TableClient client = new(storageConnectionString, "Demo");
            client.CreateIfNotExists();
            return client;
        });
        s.AddSingleton(_ =>
        {
            BlobContainerClient container = new(storageConnectionString, "demo");
            container.CreateIfNotExists();
            return container;
        });
        s.AddSingleton(_ => new NotificationQueueClient(storageConnectionString));
        s.AddSingleton(_ => new TranscribeQueueClient(storageConnectionString));
    })
    .Build();

await host.RunAsync();

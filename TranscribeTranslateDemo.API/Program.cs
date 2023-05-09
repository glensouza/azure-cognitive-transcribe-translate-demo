using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        s.AddSingleton(_ =>
        {
            QueueClient queue = new(storageConnectionString, "demo");
            queue.CreateIfNotExists();
            return queue;
        });
    })
    .Build();

await host.RunAsync();

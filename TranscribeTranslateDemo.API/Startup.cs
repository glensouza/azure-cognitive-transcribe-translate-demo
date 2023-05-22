using System.Reflection.Metadata.Ecma335;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(TranscribeTranslateDemo.API.Startup))]

namespace TranscribeTranslateDemo.API;

public class Startup : FunctionsStartup
{
    private static IConfiguration configuration = null;

    public override void Configure(IFunctionsHostBuilder builder)
    {
        ServiceProvider serviceProvider = builder.Services.BuildServiceProvider();
        configuration = serviceProvider.GetRequiredService<IConfiguration>();
        string storageConnectionString = configuration["AzureWebJobsStorage"];

        builder.Services.AddSingleton(_ => new NotificationQueueClient(storageConnectionString));
        builder.Services.AddSingleton(_ => new TranscribeQueueClient(storageConnectionString));
        builder.Services.AddSingleton(_ => SpeechTranslationConfig.FromSubscription(configuration.GetValue<string>("SpeechKey"), configuration.GetValue<string>("SpeechRegion")));
    }
}

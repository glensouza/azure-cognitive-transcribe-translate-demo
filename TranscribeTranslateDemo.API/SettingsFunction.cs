using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace TranscribeTranslateDemo.API;

public class SettingsFunction
{
    private readonly IConfiguration configuration;

    public SettingsFunction(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    [FunctionName("Settings")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        Shared.Settings settings = new() { FunctionKey = this.configuration.GetValue<string>("FunctionKey") };

        return new OkObjectResult(settings);
    }
}

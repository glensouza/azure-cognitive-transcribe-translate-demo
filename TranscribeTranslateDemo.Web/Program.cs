using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TranscribeTranslateDemo.Web;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

HttpClient http = new()
{
    BaseAddress = new Uri(builder.Configuration["API_Prefix"] ?? builder.HostEnvironment.BaseAddress),
    DefaultRequestHeaders =
    {
        { "Accept", "application/json" }
    }
};

using HttpResponseMessage response = await http.GetAsync("/api/settings");
await using Stream stream = await response.Content.ReadAsStreamAsync();

builder.Configuration.AddJsonStream(stream);
http.DefaultRequestHeaders.Add("x-function-key", builder.Configuration["FunctionKey"]);

builder.Services.AddScoped(sp => http);

await builder.Build().RunAsync();

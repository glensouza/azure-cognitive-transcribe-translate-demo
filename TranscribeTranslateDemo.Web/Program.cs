using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TranscribeTranslateDemo.Web;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

string? apiPrefix = builder.Configuration["API_Prefix"];
HttpClient http = new()
{
    BaseAddress = new Uri(apiPrefix ?? builder.HostEnvironment.BaseAddress),
    DefaultRequestHeaders =
    {
        { "Accept", "application/json" }
    }
};

builder.Services.AddScoped(sp => http);

using HttpResponseMessage response = await http.GetAsync($"{apiPrefix}/api/settings");
await using Stream stream = await response.Content.ReadAsStreamAsync();
builder.Configuration.AddJsonStream(stream);

builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();

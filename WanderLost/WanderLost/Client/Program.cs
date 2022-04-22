using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WanderLost.Client;
using WanderLost.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<ClientNotificationService>();
builder.Services.AddScoped<MerchantHubClient>();
builder.Services.AddScoped<ClientStaticDataController>();
builder.Services.AddScoped<ClientSettingsController>();
builder.Services.AddScoped<ActiveDataController>();

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddApiAuthorization();

await builder.Build().RunAsync();

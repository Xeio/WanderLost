using Append.Blazor.Notifications;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WanderLost.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddNotifications();

builder.Services.AddScoped<ClientNotificationService>();
builder.Services.AddScoped<MerchantHubClient>();
builder.Services.AddScoped<ClientStaticDataController>();
builder.Services.AddScoped<ClientSettingsController>();

builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();

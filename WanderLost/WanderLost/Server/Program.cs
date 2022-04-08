using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using WanderLost.Server.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(hubOptions=>
{
    hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
});
builder.Services.AddMemoryCache();

builder.Services.AddScoped<DataController>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddDbContext<MerchantsDbContext>(opts =>
{
    opts.UseSqlServer(builder.Configuration["SqlConnectionString"]);
});

#if !DEBUG
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
    });
#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseRouting();

app.UseResponseCompression();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<MerchantHub>($"/{MerchantHub.Path}");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    //Ensure model is created and up to date on startup
    using var context = serviceScope.ServiceProvider.GetService<MerchantsDbContext>();
    context?.Database.Migrate();
}

app.Run();

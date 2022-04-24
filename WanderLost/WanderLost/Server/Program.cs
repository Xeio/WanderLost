using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using WanderLost.Server.Controllers;
using WanderLost.Server.Data;
using WanderLost.Shared;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["SqlConnectionString"];
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<WanderlostUser>()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<WanderlostUser, AuthDbContext>();

builder.Services.AddAuthentication()
    .AddIdentityServerJwt()
    .AddDiscord(discordOptions =>
    {
        discordOptions.ClientSecret = builder.Configuration["DiscordClientSecret"];
        discordOptions.ClientId = builder.Configuration["DiscordClientId"];
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
    hubOptions.MaximumParallelInvocationsPerClient = 3;
}).AddMessagePackProtocol(Utils.BuildMessagePackOptions);
builder.Services.AddMemoryCache();

builder.Services.AddScoped<DataController>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddDbContext<MerchantsDbContext>(opts =>
{
    opts.UseSqlServer(connectionString);
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

app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();

app.UseResponseCompression();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");
app.MapHub<MerchantHub>($"/{MerchantHub.Path}");

app.UseBlazorFrameworkFiles();

app.UseStaticFiles(new StaticFileOptions()
{
    OnPrepareResponse = (staticFileContext) =>
    {
        if (staticFileContext.Context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/media")) ||
            staticFileContext.Context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/images")))
        {
            staticFileContext.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(7)
            };
        }
        else if (staticFileContext.Context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/data")) ||
                    staticFileContext.Context.Request.Path.Value?.EndsWith("Interop.js") == true)
        {
            staticFileContext.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromMinutes(5)
            };
        }
    }
});

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    //Ensure model is created and up to date on startup
    using var merchantsContext = serviceScope.ServiceProvider.GetService<MerchantsDbContext>();
    merchantsContext?.Database.Migrate();

    using var authContext = serviceScope.ServiceProvider.GetService<AuthDbContext>();
    authContext?.Database.Migrate();
}

app.Run();

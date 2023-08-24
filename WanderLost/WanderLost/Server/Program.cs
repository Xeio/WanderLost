using Duende.IdentityServer.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Prometheus;
using WanderLost.Server.Authorization;
using WanderLost.Server.Controllers;
using WanderLost.Server.Data;
using WanderLost.Server.Discord;
using WanderLost.Server.PushNotifications;
using WanderLost.Shared;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["SqlConnectionString"];
builder.Services.AddDbContext<MerchantsDbContext>(opts =>
{
    opts.UseSqlServer(connectionString, o =>
    {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
    });
});

builder.Services.AddDataProtection()
    .SetApplicationName("Wanderlost")
    .PersistKeysToDbContext<MerchantsDbContext>();

builder.Services.AddIdentityCore<WanderlostUser>(opts =>
{
    opts.User.AllowedUserNameCharacters = string.Empty;
})
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<MerchantsDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<WanderlostUser, MerchantsDbContext>()
    .AddOperationalStore<MerchantsDbContext>(o =>
    {
        o.EnableTokenCleanup = true;
    });

builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy(nameof(RareCombinationRestricted), policy =>
    {
        policy.Requirements.Add(new RareCombinationRestricted());
    });
    authorizationOptions.AddPolicy(nameof(DockerSubnetOnly), policy =>
    {
        policy.Requirements.Add(new DockerSubnetOnly());
    });
});

builder.Services.AddAuthentication(authenticationOptions =>
{
    authenticationOptions.DefaultScheme = IdentityConstants.ApplicationScheme;
    authenticationOptions.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityServerJwt()
    .AddDiscord(discordOptions =>
    {
        discordOptions.ClientSecret = builder.Configuration["DiscordClientSecret"] ?? throw new ApplicationException("Missing DiscordClientSecret configuration");
        discordOptions.ClientId = builder.Configuration["DiscordClientId"] ?? throw new ApplicationException("Missing DiscordClientId configuration"); ;
        discordOptions.Scope.Add("email");
        discordOptions.ClaimActions.MapJsonKey("verified", "verified");
        discordOptions.AccessDeniedPath = new PathString("/ErrorMessage/User denied access from Discord authentication.");
    })
    .AddIdentityCookies(identityCookieBuilder =>
    {
        identityCookieBuilder.ApplicationCookie?.Configure(cokieAuthOptions =>
        {
            cokieAuthOptions.SlidingExpiration = true;
            cokieAuthOptions.ExpireTimeSpan = TimeSpan.FromDays(30);
        });
    });

builder.Services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>, JwtPostConfiguration>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.ClientTimeoutInterval = TimeSpan.FromMinutes(8);
    hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(2);
    hubOptions.MaximumParallelInvocationsPerClient = 1;
}).AddMessagePackProtocol(Utils.BuildMessagePackOptions);
builder.Services.AddMemoryCache();

builder.Services.AddScoped<DataController>();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

builder.Services.AddScoped<PushMessageProcessor>();
builder.Services.AddScoped<PushSubscriptionManager>();

builder.Services.AddHostedService<PushWorkerService>();
builder.Services.AddHostedService<BackgroundVoteProcessor>();
builder.Services.AddHostedService<PurgeProcessor>();
builder.Services.AddHostedService<BanProcessor>();
builder.Services.AddHostedService<LeaderboardProcessor>();

builder.AddDiscord();

if (!string.IsNullOrWhiteSpace(builder.Configuration["FirebaseSecretFile"]))
{
    var firebaseCredential = GoogleCredential.FromFile(builder.Configuration["FirebaseSecretFile"]);
    FirebaseApp.Create(new AppOptions()
    {
        Credential = firebaseCredential,
    });
}

if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(o =>
    {
        o.SingleLine = true;
        o.TimestampFormat = "MM-dd HH:mm:ss ";
    });
}

var app = builder.Build();

var forwardedHeaderOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor,
};
foreach (var network in DockerSubnetOnly.DockerSubnets)
{
    forwardedHeaderOptions.KnownNetworks.Add(network);
}
app.UseForwardedHeaders(forwardedHeaderOptions);

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

//Since Identity Server delpoyed behind a reverse proxy, need to give it a specific origin
var identityServerOrigin = builder.Configuration["IdentityServerOrigin"];
app.Use((context, next) =>
{
    context.RequestServices.GetRequiredService<IServerUrls>().Origin = identityServerOrigin;
    return next(context);
});

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
            staticFileContext.Context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/images")) ||
            staticFileContext.File.Name.EndsWith(".woff"))
        {
            staticFileContext.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(7)
            };
        }
        else if (staticFileContext.Context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/data")) ||
                    staticFileContext.File.Name.EndsWith(".js") ||
                    staticFileContext.File.Name.EndsWith(".css"))
        {
            //App specific data that may change after a deployment
            staticFileContext.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromMinutes(2),
                SharedMaxAge = TimeSpan.FromDays(1),
            };
        }
    }
});

app.MapMetrics().RequireAuthorization(nameof(DockerSubnetOnly));

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    //Ensure model is created and up to date on startup
    using var merchantsContext = serviceScope.ServiceProvider.GetRequiredService<MerchantsDbContext>();

    using CancellationTokenSource timeoutTokenSource = new();
    timeoutTokenSource.CancelAfter(TimeSpan.FromMinutes(10));

    //Wait for the DB to be available(may happen if all the containers including SQL Server are restarted)
    //Has a hard timeout based on the token source above
    while (!await merchantsContext.Database.CanConnectAsync(timeoutTokenSource.Token))
    {
        await Task.Delay(TimeSpan.FromSeconds(5), timeoutTokenSource.Token);
    }

    try
    {
        merchantsContext.Database.Migrate();
    }
    catch (Microsoft.Data.SqlClient.SqlException)
    {
        //Try migrating twice, since we potentially expect the CombineDbContexts migration to fail the first time
        merchantsContext.Database.Migrate();
    }
}

app.Run();

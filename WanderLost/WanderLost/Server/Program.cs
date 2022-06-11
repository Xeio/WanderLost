using Duende.IdentityServer.Extensions;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using WanderLost.Server.Controllers;
using WanderLost.Server.Data;
using WanderLost.Shared;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["SqlConnectionString"];
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDataProtection()
    .SetApplicationName("Wanderlost")
    .PersistKeysToDbContext<AuthDbContext>();

builder.Services.AddIdentityCore<WanderlostUser>(opts => {
    opts.User.AllowedUserNameCharacters = string.Empty;
})
    .AddSignInManager()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddIdentityServer()
    .AddApiAuthorization<WanderlostUser, AuthDbContext>()
    .AddOperationalStore<AuthDbContext>(o =>
    {
        o.EnableTokenCleanup = true;
    });

builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy(nameof(RareCombinationRestricted), policy =>
    {
        policy.Requirements.Add(new RareCombinationRestricted());
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
        discordOptions.ClientSecret = builder.Configuration["DiscordClientSecret"];
        discordOptions.ClientId = builder.Configuration["DiscordClientId"];
        discordOptions.Scope.Add("email");
        discordOptions.ClaimActions.MapJsonKey("verified", "verified");
        discordOptions.AccessDeniedPath = new PathString("/ErrorMessage/User denied access from Discord authentication.");
    })
    .AddIdentityCookies(identityCookieBuilder =>
    {
        identityCookieBuilder.ApplicationCookie.Configure(cokieAuthOptions =>
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
    hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1); //Compatability: Increase this after clients are caught up (all at 1-minute)
    hubOptions.MaximumParallelInvocationsPerClient = 1;
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

builder.Services.AddScoped<PushMessageProcessor>();
builder.Services.AddHostedService<PushWorkerService>();
builder.Services.AddHostedService<BackgroundVoteProcessor>();
builder.Services.AddHostedService<PurgeProcessor>();

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
        o.TimestampFormat = "hh:mm:ss ";
    });
}

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

//Since Identity Server delpoyed behind a reverse proxy, need to give it a specific origin
var identityServerOrigin = builder.Configuration["IdentityServerOrigin"];
app.Use((context, next) =>
{
    context.SetIdentityServerOrigin(identityServerOrigin);
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
            staticFileContext.Context.Request.Path.StartsWithSegments(PathString.FromUriComponent("/images")))
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

using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    //Ensure model is created and up to date on startup
    using var merchantsContext = serviceScope.ServiceProvider.GetService<MerchantsDbContext>();
    merchantsContext?.Database.Migrate();

    using var authContext = serviceScope.ServiceProvider.GetService<AuthDbContext>();
    authContext?.Database.Migrate();
}

app.Run();

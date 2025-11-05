using DbMigrator;
using Infrastructure.FakePlatform;
using Microsoft.Extensions.Caching.Hybrid;
using RippleSync.API.Authentication;
using RippleSync.API.Common.Middleware;
using RippleSync.API.Platforms;
using RippleSync.API.PostPublisher;
using RippleSync.Application;
using RippleSync.Application.Platforms;
using RippleSync.Infrastructure;
using RippleSync.Infrastructure.Security;
using RippleSync.Infrastructure.SoMePlatforms.X;
using Serilog;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    }
});

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day, formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

/// Need connectionString
string? connString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connString))
{
    throw new InvalidOperationException("Connection string not found");
}

if (builder.Environment.IsDevelopment())
{
    if (DatabaseMigrator.MigrateDatabase(connString, true) == 0)
        DatabaseMigrator.MigrateDatabase(connString);
}

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddControllersWithViews()
    .AddApplicationPart(FakeOAuthAssemblyReference.Assembly);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandling>();

//builder.Services.AddSerilog(options =>
//{
//    options.ReadFrom.Configuration(builder.Configuration);
//});

var integrationSection = builder.Configuration.GetSection("Integrations");

builder.Services
    .AddOptions<XOptions>()
        .Bind(integrationSection.GetSection("X"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<LinkedInOptions>()
        .Bind(integrationSection.GetSection("LinkedIn"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<FacebookOptions>()
        .Bind(integrationSection.GetSection("Facebook"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<InstagramOptions>()
        .Bind(integrationSection.GetSection("Instagram"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ThreadsOptions>()
        .Bind(integrationSection.GetSection("Threads"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

bool targetProd = false;
_ = bool.TryParse(Environment.GetEnvironmentVariable("TARGET_PROD"), out targetProd);
connString = targetProd ? builder.Configuration.GetConnectionString("ProdPostgres") : connString;

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure(connString);

builder.Services.AddSingleton<IPlatformFactory, DependencyInjectionPlatformFactory>();

builder.Services.AddOptions<PasswordHasherOptions>()
    .Bind(builder.Configuration.GetSection("PasswordHasher"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("JWT")
    .ValidateDataAnnotations()
    .ValidateOnStart();

JwtOptions jwtOptions = builder.Configuration.GetRequiredSection("JWT").Get<JwtOptions>()!;
builder.Services.AddJwtAuthentication(jwtOptions);

builder.Services.AddSingleton<PostChannel>();

builder.Services.AddHostedService<PostSchedulingBackgroundService>();
builder.Services.AddHostedService<PostConsumer>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(
            [
                "http://localhost:4200",
                "https://localhost:4200",
                "https://localhost:7275",
                "https://www.ripplesync-frontend.graybeach-8775421e.northeurope.azurecontainerapps.io",
                "https://www.ripplesync.dk",
                "https://ripplesync.dk"
            ])
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(10)
    };
});

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

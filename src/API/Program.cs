using RippleSync.API.Authentication;
using RippleSync.API.Common.Middleware;
using RippleSync.Application;
using RippleSync.Infrastructure;
using RippleSync.Infrastructure.Security;
using Serilog;
using DbMigrator;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

/// Need connectionString
//string? connString = builder.Configuration.GetConnectionString("Postgres");

//if (string.IsNullOrWhiteSpace(connString))
//{
//    throw new InvalidOperationException("Connection string not found");
//}

//if (builder.Environment.IsDevelopment())
//    if (DatabaseMigrator.MigrateDatabase(connString, true) == 0)
//        DatabaseMigrator.MigrateDatabase(connString);

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
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandling>();

//builder.Services.AddSerilog(options =>
//{
//    options.ReadFrom.Configuration(builder.Configuration);
//});

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructure();

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

var app = builder.Build();

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

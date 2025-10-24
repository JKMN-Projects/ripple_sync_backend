using RippleSync.API.Common.Middleware;
using RippleSync.Application;
using RippleSync.Infrastructure;
using Serilog;
using DbMigrator;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

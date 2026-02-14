using HomeCenter.Data;
using HomeCenter.Extensions;
using HomeCenter.Services;

// Load .env file for local development
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Log configuration on startup
var environment = builder.Environment.EnvironmentName;
Console.WriteLine("=== HomeCenter Configuration ===");
Console.WriteLine($"Environment: {environment}");
Console.WriteLine($"ContentRootPath: {builder.Environment.ContentRootPath}");

builder.Services.AddQuizServices(builder.Configuration);

var app = builder.Build();

// Log critical configuration values (without exposing full secrets)
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    Console.WriteLine("\n=== Configuration Status ===");
    
    // Admin credentials
    var adminUsername = config["Admin:Username"];
    var adminPassword = config["Admin:Password"];
    Console.WriteLine($"Admin Username: {(string.IsNullOrEmpty(adminUsername) ? "NOT SET" : adminUsername)}");
    Console.WriteLine($"Admin Password: {(string.IsNullOrEmpty(adminPassword) ? "NOT SET" : "SET (length: " + adminPassword.Length + ")")}");
    
    // AI Configuration
    var aiProvider = config["AI:Provider"];
    var aiApiKey = config["AI:ApiKey"];
    var aiEnabled = config["AI:Enabled"];
    var aiModel = config["AI:Model"];
    Console.WriteLine($"\nAI Provider: {aiProvider}");
    Console.WriteLine($"AI Enabled: {aiEnabled}");
    Console.WriteLine($"AI Model: {aiModel}");
    Console.WriteLine($"AI ApiKey: {(string.IsNullOrEmpty(aiApiKey) ? "NOT SET" : "SET (length: " + aiApiKey.Length + ", starts with: " + aiApiKey.Substring(0, Math.Min(10, aiApiKey.Length)) + "...)")}");
    
    // Qwen Configuration
    var qwenApiKey = config["Qwen:ApiKey"];
    var qwenEnabled = config["Qwen:Enabled"];
    Console.WriteLine($"\nQwen Enabled: {qwenEnabled}");
    Console.WriteLine($"Qwen ApiKey: {(string.IsNullOrEmpty(qwenApiKey) ? "NOT SET" : "SET (length: " + qwenApiKey.Length + ")")}");
    
    // Connection String
    var connectionString = config.GetConnectionString("DefaultConnection");
    Console.WriteLine($"\nConnection String: {connectionString}");
    
    Console.WriteLine("================================\n");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    DatabaseMigrator.EnsureVersioningSchema(db);

    var testService = scope.ServiceProvider.GetRequiredService<ITestFileService>();
    testService.SyncTopicsFromFiles();
}

app.UseQuizPipeline();

app.Run();

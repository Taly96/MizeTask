using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ChainResource.Core;
using ChainResource.Core.Interfaces;
using ChainResource.Core.Models;
using ChainResource.Infrastructure.Storages;
using ChainResource.Runner.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
var webOptions = config.GetSection(WebStorageOptions.SectionName).Get<WebStorageOptions>() 
                 ?? throw new InvalidOperationException($"Section '{WebStorageOptions.SectionName}' is missing.");

var fileOptions = config.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>() 
                  ?? throw new InvalidOperationException($"Section '{FileStorageOptions.SectionName}' is missing.");

var memOptions = config.GetSection(MemoryStorageOptions.SectionName).Get<MemoryStorageOptions>() 
                 ?? new MemoryStorageOptions();
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("ExchangeRateChainResource", LogLevel.Debug)
        .AddConsole();
});
var httpClient = new HttpClient();
var memLogger = loggerFactory.CreateLogger<MemoryStorage<ExchangeRateList>>();
var fileLogger = loggerFactory.CreateLogger<FileSystemStorage<ExchangeRateList>>();
var webLogger = loggerFactory.CreateLogger<WebServiceStorage<ExchangeRateList>>();
var memoryStorage = new MemoryStorage<ExchangeRateList>(
    memLogger, 
    TimeSpan.FromMinutes(memOptions.CacheDurationMinutes));
var fileStorage = new FileSystemStorage<ExchangeRateList>(
    fileOptions.FullPath,
    TimeSpan.FromMinutes(fileOptions.CacheDurationMinutes), 
    fileOptions.BufferSize,
    fileLogger);
var webStorage = new WebServiceStorage<ExchangeRateList>(
    httpClient, 
    webOptions.AppId, 
    webOptions.BaseUrl, 
    webLogger);
var chainLogger = loggerFactory.CreateLogger<ChainResource<ExchangeRateList>>();
var storageChain = new IStorage<ExchangeRateList>[] { memoryStorage, fileStorage, webStorage };
var orchestrator = new ChainResource<ExchangeRateList>(storageChain, chainLogger);

Console.WriteLine("--- Starting Exchange Rate Retrieval ---");

try 
{
    var result1 = await orchestrator.GetValueAsync();
    
    Console.WriteLine($"Result 1: Base Currency is {result1.Base}");
    Console.WriteLine("\n--- Second Call (Should hit Memory) ---");
    var result2 = await orchestrator.GetValueAsync();
    
    Console.WriteLine($"Result 2: Base Currency is {result2.Base}");
}
catch (Exception exception)
{
    Console.WriteLine($"Critical Error: {exception.Message}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
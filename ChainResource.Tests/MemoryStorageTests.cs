using ChainResource.Infrastructure.Storages;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChainResource.Tests;

public class MemoryStorageTests
{
    private readonly Mock<ILogger<MemoryStorage<string>>> _loggerMock = new();

    [Fact]
    public async Task TryGetValueAsync_ReturnsInvalid_WhenEmpty()
    {
        var storage = new MemoryStorage<string>(_loggerMock.Object, TimeSpan.FromMinutes(5));
        var (isValid, value) = await storage.TryGetValueAsync();
        
        Assert.False(isValid);
        Assert.Null(value);
    }

    [Fact]
    public async Task TryGetValueAsync_ReturnsInvalid_AfterExpiration()
    {
        var storage = new MemoryStorage<string>(_loggerMock.Object, TimeSpan.FromMilliseconds(10));
        
        await storage.SetValueAsync("Expired Data");
        await Task.Delay(50); 
        var (isValid, value) = await storage.TryGetValueAsync();

        Assert.False(isValid);
        Assert.Null(value);
    }
    
    [Fact]
    public async Task SetValueAsync_UpdatesExistingValue_AndResetsExpiration()
    {
        var storage = new MemoryStorage<string>(_loggerMock.Object, TimeSpan.FromMilliseconds(100));
        
        await storage.SetValueAsync("Initial Value");
        await storage.SetValueAsync("Updated Value");
        var (isValid, value) = await storage.TryGetValueAsync();
        
        Assert.True(isValid);
        Assert.Equal("Updated Value", value);

        await Task.Delay(150); 
        var (stillValid, _) = await storage.TryGetValueAsync();
        
        Assert.False(stillValid, "The value should have expired eventually.");
    }
}
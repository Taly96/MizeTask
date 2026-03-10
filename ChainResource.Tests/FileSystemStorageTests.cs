using ChainResource.Infrastructure.Storages;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChainResource.Tests;

public class FileSystemStorageTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
    private readonly Mock<ILogger<FileSystemStorage<string>>> _loggerMock = new();

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public async Task SetValueAsync_CreatesPhysicalFile()
    {
        var storage = new FileSystemStorage<string>(_tempFile, TimeSpan.FromMinutes(10), 4096, _loggerMock.Object);
        
        await storage.SetValueAsync("File Content");
        Assert.True(File.Exists(_tempFile));
        var content = await File.ReadAllTextAsync(_tempFile);

        Assert.Contains("File Content", content);
    }

    [Fact]
    public async Task TryGetValueAsync_ReturnsInvalid_IfFileIsOld()
    {
        var storage = new FileSystemStorage<string>(_tempFile, TimeSpan.FromSeconds(1), 4096, _loggerMock.Object);
        
        await storage.SetValueAsync("Old Data");
        File.SetLastWriteTime(_tempFile, DateTime.Now.AddHours(-1));
        var (isValid, value) = await storage.TryGetValueAsync();

        Assert.False(isValid);
    }
    
    [Fact]
    public async Task TryGetValueAsync_ReturnsInvalid_IfFileDoesNotExist()
    {
        var nonExistentFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
        var storage = new FileSystemStorage<string>(nonExistentFile, TimeSpan.FromMinutes(10), 4096, _loggerMock.Object);
        var (isValid, value) = await storage.TryGetValueAsync();

        Assert.False(isValid);
        Assert.Null(value);
    }
    
    [Fact]
    public async Task TryGetValueAsync_ReturnsInvalid_AndLogsError_OnCorruptJson()
    {
        await File.WriteAllTextAsync(_tempFile, "THIS IS NOT JSON {[[");
        var storage = new FileSystemStorage<string>(_tempFile, TimeSpan.FromMinutes(10), 4096, _loggerMock.Object);
        var (isValid, value) = await storage.TryGetValueAsync();

        Assert.False(isValid);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)!),
            Times.AtLeastOnce);
    }
}
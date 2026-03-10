using ChainResource.Core;
using ChainResource.Core.Interfaces;
using ChainResource.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace ChainResource.Tests;

public class ChainResourceTests
{
    [Fact]
    public async Task GetValueAsync_FallsBackToFile_WhenMemoryIsEmpty()
    {
        var mockMemory = new Mock<IStorage<ExchangeRateList>>();
        var mockFile = new Mock<IStorage<ExchangeRateList>>();
        var mockWeb = new Mock<IStorage<ExchangeRateList>>();
        var mockLogger = new Mock<ILogger<ChainResource<ExchangeRateList>>>();
        var expectedData = new ExchangeRateList { Base = "USD" };

        mockMemory.Setup(m => m.TryGetValueAsync())
            .ReturnsAsync((false, null));
        mockFile.Setup(f => f.TryGetValueAsync())
            .ReturnsAsync((true, expectedData));

        var storageChain = new[] { mockMemory.Object, mockFile.Object, mockWeb.Object };
        var orchestrator = new ChainResource<ExchangeRateList>(storageChain, mockLogger.Object);
        var result = await orchestrator.GetValueAsync();

        Assert.NotNull(result);
        Assert.Equal("USD", result.Base);
        mockMemory.Verify(m => m.TryGetValueAsync(), Times.Exactly(2));
        mockFile.Verify(f => f.TryGetValueAsync(), Times.Once);
        mockWeb.Verify(w => w.TryGetValueAsync(), Times.Never);
    }
    
    [Fact]
    public async Task GetValueAsync_BackFillsAllPreviousLayers_WhenFoundInWeb()
    {
        var mockMemory = new Mock<IStorage<ExchangeRateList>>();
        var mockFile = new Mock<IStorage<ExchangeRateList>>();
        var mockWeb = new Mock<IStorage<ExchangeRateList>>();
        var mockLogger = new Mock<ILogger<ChainResource<ExchangeRateList>>>();
        var expectedData = new ExchangeRateList { Base = "USD" };
        
        mockMemory.Setup(m => m.TryGetValueAsync()).ReturnsAsync((false, null));
        mockMemory.SetupGet(m => m.CanWrite).Returns(true);
        mockFile.Setup(f => f.TryGetValueAsync()).ReturnsAsync((false, null));
        mockFile.SetupGet(f => f.CanWrite).Returns(true);
        mockWeb.Setup(w => w.TryGetValueAsync()).ReturnsAsync((true, expectedData));

        var orchestrator = new ChainResource<ExchangeRateList>(
            [mockMemory.Object, mockFile.Object, mockWeb.Object], 
            mockLogger.Object);
        var result = await orchestrator.GetValueAsync();

        Assert.Equal("USD", result.Base);
        mockMemory.Verify(m => m.SetValueAsync(It.IsAny<ExchangeRateList>()), Times.Once);
        mockFile.Verify(f => f.SetValueAsync(It.IsAny<ExchangeRateList>()), Times.Once);
        mockWeb.Verify(w => w.SetValueAsync(It.IsAny<ExchangeRateList>()), Times.Never);
    }
    
    [Fact]
    public async Task GetValueAsync_IsThreadSafe_FetchesFromSlowLayerOnlyOnce()
    {
        // Arrange
        var mockMemory = new Mock<IStorage<ExchangeRateList>>();
        var mockWeb = new Mock<IStorage<ExchangeRateList>>();
        var mockLogger = new Mock<ILogger<ChainResource<ExchangeRateList>>>();
        var expectedData = new ExchangeRateList { Base = "USD" };

        // 1. Simulate stateful memory caching
        var isCachedInMemory = false;
        mockMemory.Setup(m => m.TryGetValueAsync())
            // Dynamically return true/data ONLY if it has been saved
            .ReturnsAsync(() => (isCachedInMemory, isCachedInMemory ? expectedData : null)); 
        mockMemory.Setup(m => m.SetValueAsync(It.IsAny<ExchangeRateList>()))
            .Callback(() => isCachedInMemory = true)
            .Returns(Task.CompletedTask);
        mockMemory.SetupGet(m => m.CanWrite).Returns(true);
        mockWeb.Setup(w => w.TryGetValueAsync())
            .Returns(async () => 
            {
                await Task.Delay(100);
                
                return (true, expectedData);
            });

        var orchestrator = new ChainResource<ExchangeRateList>(
            [mockMemory.Object, mockWeb.Object], 
            mockLogger.Object);

        // Act
        // Launch 10 simultaneous requests
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => orchestrator.GetValueAsync()));
        var results = await Task.WhenAll(tasks);

        // Assert
        // Verify all 10 threads got the correct data
        Assert.All(results, r => Assert.Equal("USD", r.Base));
    
        // THE CRITICAL ASSERTION: The slow web layer was only queried exactly ONCE
        mockWeb.Verify(w => w.TryGetValueAsync(), Times.Once);
    }
}
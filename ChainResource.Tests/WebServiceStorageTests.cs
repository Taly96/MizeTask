using System.Net;
using ChainResource.Core.Models;
using ChainResource.Infrastructure.Storages;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace ChainResource.Tests;

public class WebServiceStorageTests
{
    [Fact]
    public async Task TryGetValueAsync_ReturnsData_OnSuccess()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"base\": \"USD\", \"rates\": {\"ILS\": 3.6}}")
        };

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        var client = new HttpClient(mockHandler.Object);
        var logger = new Mock<ILogger<WebServiceStorage<ExchangeRateList>>>();
        var storage = new WebServiceStorage<ExchangeRateList>(client, "api-key", "https://api.test/", logger.Object);
        var (isValid, result) = await storage.TryGetValueAsync();

        Assert.True(isValid);
        Assert.Equal("USD", result?.Base);
    }

    [Fact]
    public async Task TryGetValueAsync_ReturnsInvalid_OnHttpError()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.Unauthorized });
        var client = new HttpClient(mockHandler.Object);
        var logger = new Mock<ILogger<WebServiceStorage<ExchangeRateList>>>();
        var storage = new WebServiceStorage<ExchangeRateList>(client, "bad-key", "https://api.test/", logger.Object);
        var (isValid, _) = await storage.TryGetValueAsync();

        Assert.False(isValid);
    }
}
using System.Net;
using MonitorPedidos.Web.Features.ApiChecks;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Xunit;

namespace MonitorPedidos.UnitTests.ApiChecks;

public class SalesforceClientTests
{
    private static SalesforceClient BuildClient(MockHttpMessageHandler mockHttp, string baseUrl = "https://api.sf.test/")
    {
        var http = mockHttp.ToHttpClient();
        http.BaseAddress = new Uri(baseUrl);
        http.Timeout     = TimeSpan.FromSeconds(10);
        return new SalesforceClient(http, NullLogger<SalesforceClient>.Instance);
    }

    [Fact]
    public async Task PingOrdersAsync_Http200_ReturnsSuccess()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://api.sf.test/orders*")
            .Respond(HttpStatusCode.OK);

        var result = await BuildClient(mock).PingOrdersAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.HttpStatusCode);
    }

    [Fact]
    public async Task PingOrdersAsync_Http401_ReturnsUnauthorized()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://api.sf.test/orders*")
            .Respond(HttpStatusCode.Unauthorized);

        var result = await BuildClient(mock).PingOrdersAsync();

        Assert.True(result.IsUnauthorized);
        Assert.False(result.IsSuccess);
        Assert.Equal(401, result.HttpStatusCode);
    }

    [Fact]
    public async Task PingOrdersAsync_Http503_ReturnsServerError()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "https://api.sf.test/orders*")
            .Respond(HttpStatusCode.ServiceUnavailable);

        var result = await BuildClient(mock).PingOrdersAsync();

        Assert.False(result.IsSuccess);
        Assert.False(result.IsUnauthorized);
        Assert.Equal(503, result.HttpStatusCode);
    }
}

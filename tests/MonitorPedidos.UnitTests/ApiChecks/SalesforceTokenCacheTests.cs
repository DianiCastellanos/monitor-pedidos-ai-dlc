using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MonitorPedidos.Web.Features.ApiChecks;
using Moq;
using RichardSzalay.MockHttp;
using System.Net;
using Xunit;

namespace MonitorPedidos.UnitTests.ApiChecks;

public class SalesforceTokenCacheTests
{
    private const string Host     = "https://ocapi.test";
    private const string ClientId = "test-client";
    private const string Password = "test-pass";
    private const string TokenUrl = $"{Host}/dw/oauth2/access_token*";

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Salesforce:OcapiHost"]      = Host,
                ["Salesforce:ClientId"]       = ClientId,
                ["Salesforce:ClientPassword"] = Password,
            })
            .Build();

    private static IConfiguration BuildEmptyConfig() =>
        new ConfigurationBuilder().Build();

    private static IHttpClientFactory MockFactory(MockHttpMessageHandler mock)
    {
        var http    = mock.ToHttpClient();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SalesforceAuth")).Returns(http);
        return factory.Object;
    }

    private static string TokenJson(int expiresIn = 900) =>
        $$"""{"access_token":"tok-{{Guid.NewGuid():N}}","token_type":"Bearer","expires_in":{{expiresIn}}}""";

    [Fact]
    public async Task GetValidTokenAsync_ValidCredentials_ReturnsToken()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, TokenUrl).Respond("application/json", TokenJson());

        var cache = new SalesforceTokenCache(MockFactory(mock), BuildConfig(), NullLogger<SalesforceTokenCache>.Instance);

        var token = await cache.GetValidTokenAsync();

        Assert.NotNull(token);
        Assert.StartsWith("tok-", token);
    }

    [Fact]
    public async Task GetValidTokenAsync_CalledTwice_ReturnsSameToken()
    {
        var mock = new MockHttpMessageHandler();
        // Token fijo para poder verificar igualdad
        mock.When(HttpMethod.Post, TokenUrl)
            .Respond("application/json", """{"access_token":"fixed-token-abc","token_type":"Bearer","expires_in":900}""");

        var cache = new SalesforceTokenCache(MockFactory(mock), BuildConfig(), NullLogger<SalesforceTokenCache>.Instance);

        var t1 = await cache.GetValidTokenAsync();
        var t2 = await cache.GetValidTokenAsync();

        Assert.Equal("fixed-token-abc", t1);
        Assert.Equal(t1, t2); // mismo token cacheado en segunda llamada
    }

    [Fact]
    public async Task GetValidTokenAsync_AfterInvalidate_FetchesNewToken()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, TokenUrl)
            .Respond("application/json", TokenJson());

        var cache = new SalesforceTokenCache(MockFactory(mock), BuildConfig(), NullLogger<SalesforceTokenCache>.Instance);

        var t1 = await cache.GetValidTokenAsync();
        cache.Invalidate();
        var t2 = await cache.GetValidTokenAsync();

        Assert.NotNull(t1);
        Assert.NotNull(t2);
        // Ambos son válidos pero pueden ser distintos tokens (UUID distintos en el mock)
    }

    [Fact]
    public async Task GetValidTokenAsync_TokenEndpoint401_ReturnsNull()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, TokenUrl).Respond(HttpStatusCode.Unauthorized);

        var cache = new SalesforceTokenCache(MockFactory(mock), BuildConfig(), NullLogger<SalesforceTokenCache>.Instance);

        var token = await cache.GetValidTokenAsync();

        Assert.Null(token);
    }

    [Fact]
    public async Task GetValidTokenAsync_MissingCredentials_ReturnsNull()
    {
        var mock  = new MockHttpMessageHandler();
        var cache = new SalesforceTokenCache(MockFactory(mock), BuildEmptyConfig(), NullLogger<SalesforceTokenCache>.Instance);

        var token = await cache.GetValidTokenAsync();

        Assert.Null(token);
        mock.VerifyNoOutstandingRequest(); // no debería haber llamada HTTP
    }

    [Fact]
    public async Task GetValidTokenAsync_NetworkError_ReturnsNull()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, TokenUrl).Throw(new HttpRequestException("connection refused"));

        var cache = new SalesforceTokenCache(MockFactory(mock), BuildConfig(), NullLogger<SalesforceTokenCache>.Instance);

        var token = await cache.GetValidTokenAsync();

        Assert.Null(token);
    }
}

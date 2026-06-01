using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MonitorPedidos.Web.Features.ApiChecks;
using RichardSzalay.MockHttp;
using Xunit;

namespace MonitorPedidos.UnitTests.ApiChecks;

public class SalesforceClientTests
{
    private const string BaseUrl   = "https://api.sf.test/s/MySite/dw/shop/v23_2/";
    private const string SearchUrl = "https://api.sf.test/s/MySite/dw/shop/v23_2/order_search";

    private static SalesforceClient BuildClient(MockHttpMessageHandler mockHttp, IConfiguration? cfg = null)
    {
        var http = mockHttp.ToHttpClient();
        http.BaseAddress = new Uri(BaseUrl);
        http.Timeout     = TimeSpan.FromSeconds(10);
        // Auth inyectado por SalesforceAuthHandler en producción — no en tests unitarios del cliente

        cfg ??= new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Salesforce:SearchWindowHours"] = "24" })
            .Build();

        return new SalesforceClient(http, cfg, NullLogger<SalesforceClient>.Instance);
    }

    private static string OrderSearchResponseJson(int total, params (string orderNo, string siteId)[] hits)
    {
        // $$$""" (3 dollars) permite tener }} literal en el contenido JSON
        var hitsJson = string.Join(",", hits.Select(h =>
            $$$"""{"data":{"order_no":"{{{h.orderNo}}}","site_id":"{{{h.siteId}}}","status":"new","export_status":"ready","creation_date":"2026-05-01T10:00:00+00:00","payment_status":"paid"}}"""));
        return $$$"""{"count":{{{hits.Length}}},"total":{{{total}}},"hits":[{{{hitsJson}}}]}""";
    }

    [Fact]
    public async Task SearchPendingOrdersAsync_SuccessResponse_ReturnsTotalAndItems()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, SearchUrl)
            .Respond("application/json", OrderSearchResponseJson(2, ("ORD-001", "Patprimo"), ("ORD-002", "SevenSeven")));

        var result = await BuildClient(mock).SearchPendingOrdersAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Total);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Patprimo",   result.Items[0].SiteId);
        Assert.Equal("SevenSeven", result.Items[1].SiteId);
    }

    [Fact]
    public async Task SearchPendingOrdersAsync_EmptyHits_ReturnsSuccessWithZero()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, SearchUrl)
            .Respond("application/json", """{"count":0,"total":0,"hits":[]}""");

        var result = await BuildClient(mock).SearchPendingOrdersAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchPendingOrdersAsync_Http401_ReturnsUnauthorized()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, SearchUrl)
            .Respond(HttpStatusCode.Unauthorized);

        var result = await BuildClient(mock).SearchPendingOrdersAsync();

        Assert.True(result.IsUnauthorized);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SearchPendingOrdersAsync_Http503_ReturnsFailure()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, SearchUrl)
            .Respond(HttpStatusCode.ServiceUnavailable);

        var result = await BuildClient(mock).SearchPendingOrdersAsync();

        Assert.False(result.IsSuccess);
        Assert.False(result.IsUnauthorized);
        Assert.Contains("503", result.ErrorDetails);
    }

    [Fact]
    public async Task SearchPendingOrdersAsync_Timeout_ReturnsTimeout()
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Post, SearchUrl)
            .Throw(new TaskCanceledException());

        var result = await BuildClient(mock).SearchPendingOrdersAsync();

        Assert.True(result.IsTimeout);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SearchPendingOrdersAsync_NoBaseAddress_ReturnsFailure()
    {
        var mock = new MockHttpMessageHandler();
        var http = mock.ToHttpClient(); // sin BaseAddress
        var cfg  = new ConfigurationBuilder().Build();

        var client = new SalesforceClient(http, cfg, NullLogger<SalesforceClient>.Instance);
        var result = await client.SearchPendingOrdersAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("no configurado", result.ErrorDetails);
    }
}

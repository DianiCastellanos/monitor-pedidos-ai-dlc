using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MonitorPedidos.Web.Features.ApiChecks;
using Polly;
using RichardSzalay.MockHttp;
using Xunit;

namespace MonitorPedidos.UnitTests.ApiChecks;

public class ApiRetryPolicyTests
{
    [Fact]
    public async Task Policy_Http200_NeverRetries()
    {
        var callCount = 0;
        var policy = ApiRetryPolicy.Create(NullLogger.Instance);
        var context = new Context();

        var response = await policy.ExecuteAsync(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }, context);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, callCount);
        Assert.Empty(context.GetRetryAttempts());
    }

    [Fact]
    public async Task Policy_Http503_503_200_RetriesTwice()
    {
        var responses = new Queue<HttpStatusCode>(
        [
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK
        ]);

        var callCount = 0;
        var policy    = ApiRetryPolicy.Create(NullLogger.Instance);
        var context   = new Context();

        var response = await policy.ExecuteAsync(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(responses.Dequeue()));
        }, context);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, callCount);
        Assert.Equal(2, context.GetRetryAttempts().Count);
        Assert.Equal(1, context.GetRetryAttempts()[0].AttemptNumber);
        Assert.Equal(2, context.GetRetryAttempts()[1].AttemptNumber);
    }

    [Fact]
    public async Task Policy_Http401_NeverRetries()
    {
        var callCount = 0;
        var policy    = ApiRetryPolicy.Create(NullLogger.Instance);
        var context   = new Context();

        // 401 no es error transitorio — Polly no reintenta
        var response = await policy.ExecuteAsync(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }, context);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, callCount);
        Assert.Empty(context.GetRetryAttempts());
    }

    [Fact]
    public async Task Policy_503_503_503_ExhaustsRetries()
    {
        var callCount = 0;
        var policy    = ApiRetryPolicy.Create(NullLogger.Instance);
        var context   = new Context();

        var response = await policy.ExecuteAsync(_ =>
        {
            callCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }, context);

        // 1 intento original + 2 reintentos = 3 llamadas
        Assert.Equal(3, callCount);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(2, context.GetRetryAttempts().Count);
    }
}

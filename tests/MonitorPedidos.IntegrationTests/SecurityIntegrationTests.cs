using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.RegularExpressions;

namespace MonitorPedidos.IntegrationTests;

public class SecurityIntegrationTests : IClassFixture<MonitorPedidosWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityIntegrationTests(MonitorPedidosWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies     = true
        });
    }

    // Extrae el token antiforgery del HTML del formulario de login
    // @Html.AntiForgeryToken() genera: name="..." type="hidden" value="..." → hay attrs entre name y value
    private async Task<string> GetAntiforgeryTokenAsync()
    {
        var response = await _client.GetAsync("/Identity/Select");
        var html     = await response.Content.ReadAsStringAsync();
        // Permite atributos intermedios (type="hidden") entre name y value
        var match = Regex.Match(html, @"name=""__RequestVerificationToken""[^>]+value=""([^""]*)""");
        if (!match.Success)
            match = Regex.Match(html, @"value=""([^""]*)""[^>]+name=""__RequestVerificationToken""");
        return match.Success ? match.Groups[1].Value : "";
    }

    private async Task<HttpResponseMessage> LoginAsync(string identity)
    {
        var token = await GetAntiforgeryTokenAsync();
        var form  = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("identity",                    identity),
            new KeyValuePair<string, string>("__RequestVerificationToken",  token),
        });
        return await _client.PostAsync("/Identity/Select", form);
    }

    [Fact]
    public async Task Get_Dashboard_Unauthenticated_RedirectsToLogin()
    {
        // RF-26, BR-AUTHZ-01: deny-by-default — sin cookie → redirige al login
        var response = await _client.GetAsync("/dashboard");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Select", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Get_Root_Unauthenticated_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Post_SelectIdentity_Operador_SetsCookieAndRedirects()
    {
        var response = await LoginAsync("Operador");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/dashboard", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task Get_Dashboard_AfterLogin_Returns200()
    {
        await LoginAsync("Operador");

        var response = await _client.GetAsync("/dashboard");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_Logs_AsOperador_RedirectsAwayFromPage()
    {
        // RF-26: Logs solo accesible para rol Técnico — Operador recibe redirect
        await LoginAsync("Operador");

        var response = await _client.GetAsync("/logs");

        // Espera redirect (a login o AccessDenied según el estado de auth de Blazor)
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden,
            $"Expected redirect or 403, got {response.StatusCode}");
    }
}

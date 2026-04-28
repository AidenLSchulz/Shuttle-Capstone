using Microsoft.AspNetCore.Mvc.Testing;
using MidStateShuttleService;
using Xunit;

public class DashboardAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public DashboardAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_UnauthenticatedUser_RedirectsToLogin()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Dashboard");

        // Assert — redirects to Microsoft login
        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("login.microsoftonline.com", response.Headers.Location?.ToString());
    }
}
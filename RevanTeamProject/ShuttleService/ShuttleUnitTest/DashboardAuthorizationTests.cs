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
    public async Task Dashboard_UnauthenticatedUser_DoesNotReturnOk()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Dashboard");

        // Just verify user is NOT allowed direct access
        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
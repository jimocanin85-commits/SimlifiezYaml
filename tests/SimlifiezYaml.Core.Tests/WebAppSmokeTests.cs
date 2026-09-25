using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SimlifiezYaml.Core.Tests;

/// <summary>Starts the real web app in memory and requests its pages.</summary>
public class WebAppSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public WebAppSmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task HomePageRenders()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("SimlifiezYaml", html);
        Assert.Contains("Project type", html);
        Assert.Contains("_framework/blazor.web.js", html);
    }

    [Theory]
    [InlineData("/js/download.js", "downloadText")]
    [InlineData("/app.css", ".wizard-nav")]
    public async Task StaticAssetsAreServed(string path, string expected)
    {
        var response = await _factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ErrorPageExists()
    {
        var response = await _factory.CreateClient().GetAsync("/Error");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

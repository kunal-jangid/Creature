using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Creature.SystemIntegration;
using Xunit;

namespace Creature.Tests.SystemIntegration;

public class UpdateManagerTests
{
    [Theory]
    [InlineData("v1.0.0", 1, 0, 0)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("V2.1.0.4", 2, 1, 0, 4)]
    public void TryParseVersion_ValidTags_ParsesCorrectly(string tag, int major, int minor, int build, int revision = -1)
    {
        var success = UpdateManager.TryParseVersion(tag, out var version);

        Assert.True(success);
        Assert.NotNull(version);
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
        if (revision >= 0)
        {
            Assert.Equal(revision, version.Revision);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-tag")]
    public void TryParseVersion_InvalidTags_ReturnsFalse(string? tag)
    {
        var success = UpdateManager.TryParseVersion(tag, out var version);

        Assert.False(success);
        Assert.Null(version);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.2.0", "1.1.9", false)]
    public void IsNewerVersion_ComparesCorrectly(string currentStr, string latestStr, bool expected)
    {
        var current = Version.Parse(currentStr);
        var latest = Version.Parse(latestStr);

        var isNewer = UpdateManager.IsNewerVersion(current, latest);

        Assert.Equal(expected, isNewer);
    }

    [Fact]
    public void ParseGitHubReleaseJson_ValidJson_ReturnsReleaseInfo()
    {
        var json = """
        {
            "tag_name": "v1.2.0",
            "html_url": "https://github.com/kunal-jangid/Creature/releases/tag/v1.2.0",
            "body": "Fixed animations and improved stability."
        }
        """;

        var (tag, url, notes) = UpdateManager.ParseGitHubReleaseJson(json);

        Assert.Equal("v1.2.0", tag);
        Assert.Equal("https://github.com/kunal-jangid/Creature/releases/tag/v1.2.0", url);
        Assert.Equal("Fixed animations and improved stability.", notes);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNewerVersionExists_ReportsUpdateAvailable()
    {
        var responseJson = """
        {
            "tag_name": "v2.0.0",
            "html_url": "https://github.com/kunal-jangid/Creature/releases/tag/v2.0.0",
            "body": "Major release"
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseJson);
        using var httpClient = new HttpClient(handler);
        using var updateManager = new TestableUpdateManager(new Version(1, 0, 0), httpClient);

        var result = await updateManager.CheckForUpdatesAsync();

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal(new Version(1, 0, 0), result.CurrentVersion);
        Assert.Equal(new Version(2, 0, 0), result.LatestVersion);
        Assert.Equal("https://github.com/kunal-jangid/Creature/releases/tag/v2.0.0", result.ReleaseUrl);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenSameVersion_ReportsNoUpdate()
    {
        var responseJson = """
        {
            "tag_name": "v1.0.0",
            "html_url": "https://github.com/kunal-jangid/Creature/releases/tag/v1.0.0",
            "body": "Current release"
        }
        """;

        var handler = new MockHttpMessageHandler(HttpStatusCode.OK, responseJson);
        using var httpClient = new HttpClient(handler);
        using var updateManager = new TestableUpdateManager(new Version(1, 0, 0), httpClient);

        var result = await updateManager.CheckForUpdatesAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.Equal(new Version(1, 0, 0), result.CurrentVersion);
        Assert.Equal(new Version(1, 0, 0), result.LatestVersion);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task CheckForUpdatesAsync_WhenHttpFails_ReturnsErrorMessage()
    {
        var handler = new MockHttpMessageHandler(HttpStatusCode.NotFound, "Not Found");
        using var httpClient = new HttpClient(handler);
        using var updateManager = new TestableUpdateManager(new Version(1, 0, 0), httpClient);

        var result = await updateManager.CheckForUpdatesAsync();

        Assert.False(result.IsUpdateAvailable);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("404", result.ErrorMessage);
    }

    private class TestableUpdateManager : UpdateManager
    {
        private readonly Version _version;

        public TestableUpdateManager(Version version, HttpClient httpClient) : base(httpClient)
        {
            _version = version;
        }

        public override Version GetCurrentVersion() => _version;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _content;

        public MockHttpMessageHandler(HttpStatusCode statusCode, string content)
        {
            _statusCode = statusCode;
            _content = content;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_content)
            };
            return Task.FromResult(response);
        }
    }
}

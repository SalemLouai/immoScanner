using FluentAssertions;
using ImmoScorer.Infrastructure.AntiBot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;

namespace ImmoScorer.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="AntiBotService"/>.
/// Focus: delay calculation, backoff escalation, robots.txt parsing, captcha detection.
/// </summary>
public sealed class AntiBotServiceTests
{
    [Fact]
    public async Task GetRandomUserAgentAsync_ReturnsNonEmptyString()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var userAgent = await sut.GetRandomUserAgentAsync();

        // Assert
        userAgent.Should().NotBeNullOrEmpty();
        userAgent.Should().Contain("Mozilla", "user agent should look realistic");
    }

    [Fact]
    public async Task GetRandomUserAgentAsync_ReturnsDifferentAgents_OnMultipleCalls()
    {
        // Arrange
        var sut = CreateService();
        var agents = new HashSet<string>();

        // Act
        for (int i = 0; i < 20; i++)
        {
            agents.Add(await sut.GetRandomUserAgentAsync());
        }

        // Assert
        agents.Should().HaveCountGreaterThan(1, "multiple user agents should be available");
    }

    [Fact]
    public async Task DelayBeforeRequestAsync_WithNoErrors_DelaysWithinConfiguredRange()
    {
        // Arrange
        var options = new AntiBotOptions
        {
            MinDelayMs = 100,
            MaxDelayMs = 200,
            BaseBackoffMs = 1000,
            MaxBackoffAttempts = 3,
            RobotsTxtCacheHours = 24,
        };

        var sut = CreateService(options);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await sut.DelayBeforeRequestAsync("example.com");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeInRange(100, 250, "delay should be within configured range + tolerance");
    }

    [Fact]
    public async Task HandleResponseErrorAsync_IncrementsErrorCount_AndAppliesBackoff()
    {
        // Arrange
        var options = new AntiBotOptions
        {
            MinDelayMs = 100,
            MaxDelayMs = 200,
            BaseBackoffMs = 500,
            MaxBackoffAttempts = 3,
            RobotsTxtCacheHours = 24,
        };

        var sut = CreateService(options);

        // Act
        await sut.HandleResponseErrorAsync("example.com", 429);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await sut.DelayBeforeRequestAsync("example.com");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(500, "backoff should be applied after error");
    }

    [Fact]
    public async Task HandleResponseErrorAsync_ExponentialBackoff_IncreasesWithEachError()
    {
        // Arrange
        var options = new AntiBotOptions
        {
            MinDelayMs = 0,
            MaxDelayMs = 0,
            BaseBackoffMs = 100,
            MaxBackoffAttempts = 5,
            RobotsTxtCacheHours = 24,
        };

        var sut = CreateService(options);

        // Act - simulate 3 consecutive errors
        await sut.HandleResponseErrorAsync("example.com", 429);
        await sut.HandleResponseErrorAsync("example.com", 429);
        await sut.HandleResponseErrorAsync("example.com", 429);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await sut.DelayBeforeRequestAsync("example.com");
        stopwatch.Stop();

        // Assert
        // After 3 errors: baseDelay (0) + backoff (100 * 2^2 = 400)
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(350, "exponential backoff should compound");
    }

    [Fact]
    public async Task HandleResponseErrorAsync_MaxBackoffAttempts_StopsIncrementing()
    {
        // Arrange
        var options = new AntiBotOptions
        {
            MinDelayMs = 0,
            MaxDelayMs = 0,
            BaseBackoffMs = 100,
            MaxBackoffAttempts = 2,
            RobotsTxtCacheHours = 24,
        };

        var logger = Substitute.For<ILogger<AntiBotService>>();
        var sut = CreateService(options, logger);

        // Act - exceed max attempts
        await sut.HandleResponseErrorAsync("example.com", 429);
        await sut.HandleResponseErrorAsync("example.com", 429);
        await sut.HandleResponseErrorAsync("example.com", 429); // exceeds max

        // Assert
        logger.Received(1).LogWarning(
            Arg.Is<string>(s => s.Contains("exceeded max backoff attempts")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task IsAllowedByRobotsTxtAsync_MalformedUrl_ReturnsTrue()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.IsAllowedByRobotsTxtAsync("not-a-valid-url");

        // Assert
        result.Should().BeTrue("malformed URLs should fail open");
    }

    [Fact]
    public async Task IsCaptchaDetectedAsync_PageContainsCaptchaKeyword_ReturnsTrue()
    {
        // Arrange
        var sut = CreateService();
        var pageContent = "<html><body>Please verify you are human by solving this captcha</body></html>";

        // Act
        var result = await sut.IsCaptchaDetectedAsync(pageContent);

        // Assert
        result.Should().BeTrue("page contains 'captcha' keyword");
    }

    [Fact]
    public async Task IsCaptchaDetectedAsync_PageContainsRecaptcha_ReturnsTrue()
    {
        // Arrange
        var sut = CreateService();
        var pageContent = "<html><body><div class='g-recaptcha'></div></body></html>";

        // Act
        var result = await sut.IsCaptchaDetectedAsync(pageContent);

        // Assert
        result.Should().BeTrue("page contains 'recaptcha' keyword");
    }

    [Fact]
    public async Task IsCaptchaDetectedAsync_PageContainsBlockedKeyword_ReturnsTrue()
    {
        // Arrange
        var sut = CreateService();
        var pageContent = "<html><body>Access denied - automated access is not allowed</body></html>";

        // Act
        var result = await sut.IsCaptchaDetectedAsync(pageContent);

        // Assert
        result.Should().BeTrue("page contains 'automated' and 'blocked' keywords");
    }

    [Fact]
    public async Task IsCaptchaDetectedAsync_NormalPage_ReturnsFalse()
    {
        // Arrange
        var sut = CreateService();
        var pageContent = "<html><body><h1>Welcome</h1><p>Normal content</p></body></html>";

        // Act
        var result = await sut.IsCaptchaDetectedAsync(pageContent);

        // Assert
        result.Should().BeFalse("page contains no captcha keywords");
    }

    [Fact]
    public async Task IsCaptchaDetectedAsync_CaseInsensitive_DetectsCaptcha()
    {
        // Arrange
        var sut = CreateService();
        var pageContent = "<html><body>CAPTCHA challenge required</body></html>";

        // Act
        var result = await sut.IsCaptchaDetectedAsync(pageContent);

        // Assert
        result.Should().BeTrue("detection should be case-insensitive");
    }

    [Fact]
    public async Task DelayBeforeRequestAsync_DifferentDomains_IsolatesErrorCounts()
    {
        // Arrange
        var options = new AntiBotOptions
        {
            MinDelayMs = 0,
            MaxDelayMs = 0,
            BaseBackoffMs = 1000,
            MaxBackoffAttempts = 3,
            RobotsTxtCacheHours = 24,
        };

        var sut = CreateService(options);

        // Act - error on domain A
        await sut.HandleResponseErrorAsync("domainA.com", 429);

        // Delay for domain B (no errors)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await sut.DelayBeforeRequestAsync("domainB.com");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "domainB should have no backoff");
    }

    private static AntiBotService CreateService(
        AntiBotOptions? options = null,
        ILogger<AntiBotService>? logger = null)
    {
        options ??= new AntiBotOptions
        {
            MinDelayMs = 100,
            MaxDelayMs = 200,
            BaseBackoffMs = 1000,
            MaxBackoffAttempts = 3,
            RobotsTxtCacheHours = 24,
        };

        var optionsWrapper = Substitute.For<IOptions<AntiBotOptions>>();
        optionsWrapper.Value.Returns(options);

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var httpClient = new HttpClient(new FakeHttpMessageHandler());
        httpClientFactory.CreateClient("antibot").Returns(httpClient);

        logger ??= Substitute.For<ILogger<AntiBotService>>();

        return new AntiBotService(optionsWrapper, httpClientFactory, logger);
    }

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Return empty robots.txt
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("User-agent: *\nDisallow:\n"),
            };

            return Task.FromResult(response);
        }
    }
}

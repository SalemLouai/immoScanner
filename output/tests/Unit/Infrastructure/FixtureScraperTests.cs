using FluentAssertions;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;
using ImmoScorer.Infrastructure.Scraping;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ImmoScorer.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for <see cref="FixtureScraper"/>.
/// Focus: fallback scraper behavior, static data consistency.
/// </summary>
public sealed class FixtureScraperTests
{
    [Fact]
    public async Task ScrapeAsync_ReturnsStaticListings()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        var result = await scraper.ScrapeAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Count.Should().Be(5, "fixture scraper returns 5 static listings");
    }

    [Fact]
    public async Task ScrapeAsync_ListingsMatchCriteria_CityAndPostalCode()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        var criteria = new SearchCriteria(
            City: "Lyon",
            PostalCode: "69001",
            PropertyType: PropertyType.House,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        var result = await scraper.ScrapeAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(l => l.City == "Lyon" && l.PostalCode == "69001",
            "fixture scraper should use criteria city and postal code");
    }

    [Fact]
    public async Task ScrapeAsync_ReturnsListingsWithValidData()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        var result = await scraper.ScrapeAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(l =>
            !string.IsNullOrWhiteSpace(l.Title) &&
            l.Price > 0 &&
            l.Area > 0 &&
            !string.IsNullOrWhiteSpace(l.ExternalId),
            "all listings should have valid core data");
    }

    [Fact]
    public async Task ScrapeAsync_AllListingsHaveUniqueExternalIds()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        var result = await scraper.ScrapeAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var externalIds = result.Value.Select(l => l.ExternalId).ToList();
        externalIds.Should().OnlyHaveUniqueItems("external IDs must be unique for deduplication");
    }

    [Fact]
    public void SourceName_ReturnsFixture()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        // Act
        var sourceName = scraper.SourceName;

        // Assert
        sourceName.Should().Be("Fixture");
    }

    [Fact]
    public async Task ScrapeAsync_LogsInformation()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        await scraper.ScrapeAsync(criteria);

        // Assert
        logger.Received(1).LogInformation(
            Arg.Is<string>(s => s.Contains("FixtureScraper")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task ScrapeAsync_ListingsHaveVariedScores_WhenProcessed()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FixtureScraper>>();
        var scraper = new FixtureScraper(logger);

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: null,
            MaxPrice: null,
            MinArea: null,
            MaxArea: null,
            MinRooms: null,
            MaxRooms: null);

        // Act
        var result = await scraper.ScrapeAsync(criteria);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var pricesPerM2 = result.Value.Select(l => l.Price / l.Area).ToList();
        pricesPerM2.Should().HaveCountGreaterThan(1);
        pricesPerM2.Distinct().Should().HaveCountGreaterThan(1, "listings should have varied price/m2 for interesting scoring");
    }
}

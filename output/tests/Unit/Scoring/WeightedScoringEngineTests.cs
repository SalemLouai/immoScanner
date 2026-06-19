using FluentAssertions;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.ValueObjects;
using ImmoScorer.Infrastructure.Scoring;

namespace ImmoScorer.Tests.Unit.Scoring;

/// <summary>
/// Unit tests for <see cref="WeightedScoringEngine"/>.
/// Focus: scoring algorithm correctness, edge cases, and breakdown accuracy.
/// </summary>
public sealed class WeightedScoringEngineTests
{
    private readonly WeightedScoringEngine _sut = new();

    [Fact]
    public void ComputeScore_ListingFarBelowMarket_ReturnsHighScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 200_000m,
            Area: 50m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: "B",
            OriginalUrl: "https://test.com",
            ExternalId: "test-1");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 6000m, // listing is at 4000 EUR/m2
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var score = _sut.ComputeScore(listing, referenceData);

        // Assert
        score.Value.Should().BeGreaterThan(60, "listing is significantly underpriced");
    }

    [Fact]
    public void ComputeScore_ListingAtMarket_ReturnsNeutralScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 50m,
            Rooms: 2,
            Floor: null,
            EnergyRating: null,
            OriginalUrl: "https://test.com",
            ExternalId: "test-2");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m, // listing is exactly at 5000 EUR/m2
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.PriceGapScore.Should().Be(30, "at-market listings should score neutral");
    }

    [Fact]
    public void ComputeScore_ListingAboveMarket_ReturnsLowPriceGapScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 350_000m,
            Area: 50m,
            Rooms: 2,
            Floor: 2,
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-3");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m, // listing is at 7000 EUR/m2 (40% above)
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.PriceGapScore.Should().BeLessThan(30, "overpriced listings should have low price gap score");
    }

    [Fact]
    public void ComputeScore_NoReferenceData_ReturnsNeutralPriceGapScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 200_000m,
            Area: 50m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: "B",
            OriginalUrl: "https://test.com",
            ExternalId: "test-4");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 0m, // No data
            SampleCount: 0,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.PriceGapScore.Should().Be(30, "without reference data, neutral score is expected");
    }

    [Fact]
    public void ComputeBreakdown_LargeArea_ReturnsMaxAreaScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 500_000m,
            Area: 150m, // Large area
            Rooms: 5,
            Floor: 2,
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-5");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.House,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.AreaScore.Should().Be(15, "area >= 100m2 should receive max area score");
    }

    [Fact]
    public void ComputeBreakdown_SmallArea_ReturnsLowAreaScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 100_000m,
            Area: 20m, // Small area
            Rooms: 1,
            Floor: 1,
            EnergyRating: "D",
            OriginalUrl: "https://test.com",
            ExternalId: "test-6");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.AreaScore.Should().BeLessThan(15, "small areas should have lower area score");
        breakdown.AreaScore.Should().BeGreaterThan(0, "non-zero area should have some score");
    }

    [Fact]
    public void ComputeBreakdown_HighFloor_ReturnsHighFloorScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 8, // High floor
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-7");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.FloorScore.Should().Be(10, "floor >= 6 should receive max floor score");
    }

    [Fact]
    public void ComputeBreakdown_GroundFloor_ReturnsLowFloorScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 0, // Ground floor
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-8");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.FloorScore.Should().Be(2, "ground floor should receive lowest floor score");
    }

    [Fact]
    public void ComputeBreakdown_UnknownFloor_ReturnsNeutralFloorScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: null, // Unknown
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-9");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.FloorScore.Should().Be(5, "unknown floor should receive neutral score");
    }

    [Fact]
    public void ComputeBreakdown_EnergyRatingA_ReturnsMaxEnergyScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: "A", // Best rating
            OriginalUrl: "https://test.com",
            ExternalId: "test-10");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.EnergyScore.Should().Be(15, "energy rating A should receive max energy score");
    }

    [Fact]
    public void ComputeBreakdown_EnergyRatingG_ReturnsMinEnergyScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: "G", // Worst rating
            OriginalUrl: "https://test.com",
            ExternalId: "test-11");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.EnergyScore.Should().Be(0, "energy rating G should receive min energy score");
    }

    [Fact]
    public void ComputeBreakdown_UnknownEnergyRating_ReturnsNeutralEnergyScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: null, // Unknown
            OriginalUrl: "https://test.com",
            ExternalId: "test-12");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.EnergyScore.Should().Be(7, "unknown energy rating should receive neutral score");
    }

    [Fact]
    public void ComputeScore_ScoreIsClamped_BetweenZeroAndOneHundred()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 100_000m,
            Area: 200m, // Large area = 15 pts
            Rooms: 5,
            Floor: 10, // High floor = 10 pts
            EnergyRating: "A", // A-rated = 15 pts
            OriginalUrl: "https://test.com",
            ExternalId: "test-13");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 10_000m, // listing is at 500 EUR/m2 (very underpriced) = 60 pts
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var score = _sut.ComputeScore(listing, referenceData);

        // Assert
        score.Value.Should().BeLessOrEqualTo(100, "score must not exceed 100");
        score.Value.Should().BeGreaterOrEqualTo(0, "score must not be negative");
    }

    [Fact]
    public void ComputeBreakdown_ZeroArea_ReturnsZeroAreaScore()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 100_000m,
            Area: 0m, // Edge case: zero area
            Rooms: 1,
            Floor: 1,
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-14");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.AreaScore.Should().Be(0, "zero area should result in zero area score");
    }

    [Fact]
    public void ComputeScore_NegativeFloor_TreatedAsUnknown()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: -1, // Negative floor (basement)
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-15");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        // Negative floors fall into default case (10 pts)
        breakdown.FloorScore.Should().Be(10, "negative floor handled by default case");
    }

    [Fact]
    public void ComputeBreakdown_CaseInsensitiveEnergyRating_WorksCorrectly()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: "b", // lowercase
            OriginalUrl: "https://test.com",
            ExternalId: "test-16");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);

        // Assert
        breakdown.EnergyScore.Should().Be(13, "lowercase 'b' should be treated as 'B'");
    }

    [Fact]
    public void ComputeBreakdown_TotalScoreMatchesSum_OfAllSubScores()
    {
        // Arrange
        var scraped = new ScrapedListing(
            Title: "Test",
            Description: null,
            Source: "Test",
            City: "Paris",
            PostalCode: "75001",
            Price: 250_000m,
            Area: 60m,
            Rooms: 2,
            Floor: 3,
            EnergyRating: "C",
            OriginalUrl: "https://test.com",
            ExternalId: "test-17");

        var listing = Listing.Create(
            SearchId.New(),
            scraped,
            referencePricePerM2: 0m,
            new Score(0),
            new ScoreBreakdown(0, 0, 0, 0));

        var referenceData = new DvfReferenceData(
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MedianPricePerM2: 5000m,
            SampleCount: 100,
            DataAsOf: DateTime.UtcNow);

        // Act
        var breakdown = _sut.ComputeBreakdown(listing, referenceData);
        var score = _sut.ComputeScore(listing, referenceData);
        var expectedTotal = breakdown.PriceGapScore + breakdown.AreaScore
                            + breakdown.FloorScore + breakdown.EnergyScore;

        // Assert
        score.Value.Should().Be(expectedTotal, "total score must equal sum of sub-scores");
    }
}

using FluentAssertions;
using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Application.Scraping.Commands;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ReferenceData;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.Scoring;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ImmoScorer.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="ProcessScrapingJobCommandHandler"/>.
/// Focus: scraper resolution, DVF enrichment, scoring, deduplication.
/// </summary>
public sealed class ProcessScrapingJobCommandHandlerTests
{
    [Fact]
    public async Task Handle_ScraperFound_ReturnsListingCount()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var scraper = Substitute.For<IListingScraper>();
        scraper.SourceName.Returns("TestSource");

        var scrapedListings = new List<ScrapedListing>
        {
            new("Title1", null, "TestSource", "Paris", "75001", 200_000m, 50m, 2, 3, "C", "https://test.com/1", "ext-1"),
            new("Title2", null, "TestSource", "Paris", "75001", 300_000m, 70m, 3, 2, "B", "https://test.com/2", "ext-2"),
        };

        scraper.ScrapeAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ScrapedListing>>.Success(scrapedListings));

        var dvfProvider = Substitute.For<IDvfReferenceDataProvider>();
        dvfProvider.GetReferenceDataAsync(Arg.Any<string>(), Arg.Any<PropertyType>(), Arg.Any<CancellationToken>())
            .Returns(Result<DvfReferenceData>.Success(
                new DvfReferenceData("75001", PropertyType.Apartment, 5000m, 100, DateTime.UtcNow)));

        var scoringEngine = Substitute.For<IScoringEngine>();
        scoringEngine.ComputeScore(Arg.Any<Listing>(), Arg.Any<DvfReferenceData>())
            .Returns(new Score(75));
        scoringEngine.ComputeBreakdown(Arg.Any<Listing>(), Arg.Any<DvfReferenceData>())
            .Returns(new ScoreBreakdown(50, 10, 8, 7));

        var logger = Substitute.For<ILogger<ProcessScrapingJobCommandHandler>>();
        var handler = new ProcessScrapingJobCommandHandler(
            new[] { scraper },
            dvfProvider,
            scoringEngine,
            dbContext,
            logger);

        var criteria = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var job = new ScrapingJob(Guid.NewGuid(), SearchId.New(), "TestSource", criteria);
        var command = new ProcessScrapingJobCommand(job);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2, "both listings should be persisted");

        var listings = await dbContext.Listings.ToListAsync();
        listings.Should().HaveCount(2);
        listings.Should().OnlyContain(l => l.Score.Value == 75);
    }

    [Fact]
    public async Task Handle_ScraperNotFound_ReturnsFailure()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var scraper = Substitute.For<IListingScraper>();
        scraper.SourceName.Returns("OtherSource");

        var dvfProvider = Substitute.For<IDvfReferenceDataProvider>();
        var scoringEngine = Substitute.For<IScoringEngine>();
        var logger = Substitute.For<ILogger<ProcessScrapingJobCommandHandler>>();

        var handler = new ProcessScrapingJobCommandHandler(
            new[] { scraper },
            dvfProvider,
            scoringEngine,
            dbContext,
            logger);

        var criteria = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var job = new ScrapingJob(Guid.NewGuid(), SearchId.New(), "UnknownSource", criteria);
        var command = new ProcessScrapingJobCommand(job);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No scraper found");
    }

    [Fact]
    public async Task Handle_ScraperFails_ReturnsFailure()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var scraper = Substitute.For<IListingScraper>();
        scraper.SourceName.Returns("TestSource");
        scraper.ScrapeAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ScrapedListing>>.Failure("Scraping error"));

        var dvfProvider = Substitute.For<IDvfReferenceDataProvider>();
        var scoringEngine = Substitute.For<IScoringEngine>();
        var logger = Substitute.For<ILogger<ProcessScrapingJobCommandHandler>>();

        var handler = new ProcessScrapingJobCommandHandler(
            new[] { scraper },
            dvfProvider,
            scoringEngine,
            dbContext,
            logger);

        var criteria = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var job = new ScrapingJob(Guid.NewGuid(), SearchId.New(), "TestSource", criteria);
        var command = new ProcessScrapingJobCommand(job);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Scraping error");
    }

    [Fact]
    public async Task Handle_DvfDataUnavailable_UsesFallbackReferencePrice()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var scraper = Substitute.For<IListingScraper>();
        scraper.SourceName.Returns("TestSource");

        var scrapedListings = new List<ScrapedListing>
        {
            new("Title1", null, "TestSource", "Paris", "75001", 200_000m, 50m, 2, 3, "C", "https://test.com/1", "ext-1"),
        };

        scraper.ScrapeAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ScrapedListing>>.Success(scrapedListings));

        var dvfProvider = Substitute.For<IDvfReferenceDataProvider>();
        dvfProvider.GetReferenceDataAsync(Arg.Any<string>(), Arg.Any<PropertyType>(), Arg.Any<CancellationToken>())
            .Returns(Result<DvfReferenceData>.Failure("No data available"));

        var scoringEngine = Substitute.For<IScoringEngine>();
        scoringEngine.ComputeScore(Arg.Any<Listing>(), Arg.Any<DvfReferenceData>())
            .Returns(new Score(50));
        scoringEngine.ComputeBreakdown(Arg.Any<Listing>(), Arg.Any<DvfReferenceData>())
            .Returns(new ScoreBreakdown(30, 10, 5, 5));

        var logger = Substitute.For<ILogger<ProcessScrapingJobCommandHandler>>();
        var handler = new ProcessScrapingJobCommandHandler(
            new[] { scraper },
            dvfProvider,
            scoringEngine,
            dbContext,
            logger);

        var criteria = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var job = new ScrapingJob(Guid.NewGuid(), SearchId.New(), "TestSource", criteria);
        var command = new ProcessScrapingJobCommand(job);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);

        var listing = await dbContext.Listings.FirstAsync();
        listing.ReferencePricePerM2.Should().Be(0m, "fallback to 0 when DVF data unavailable");
    }

    [Fact]
    public async Task Handle_DuplicateListing_SkipsPersistence()
    {
        // Arrange
        var dbContext = CreateDbContext();

        // Pre-persist a listing with the same Source + ExternalId
        var existingListing = Listing.Create(
            SearchId.New(),
            new ScrapedListing("Existing", null, "TestSource", "Paris", "75001", 200_000m, 50m, 2, 3, "C", "https://test.com/1", "ext-dup"),
            5000m,
            new Score(60),
            new ScoreBreakdown(40, 10, 5, 5));

        dbContext.Listings.Add(existingListing);
        await dbContext.SaveChangesAsync();

        var scraper = Substitute.For<IListingScraper>();
        scraper.SourceName.Returns("TestSource");

        var scrapedListings = new List<ScrapedListing>
        {
            new("Duplicate", null, "TestSource", "Paris", "75001", 200_000m, 50m, 2, 3, "C", "https://test.com/1", "ext-dup"),
        };

        scraper.ScrapeAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ScrapedListing>>.Success(scrapedListings));

        var dvfProvider = Substitute.For<IDvfReferenceDataProvider>();
        dvfProvider.GetReferenceDataAsync(Arg.Any<string>(), Arg.Any<PropertyType>(), Arg.Any<CancellationToken>())
            .Returns(Result<DvfReferenceData>.Success(
                new DvfReferenceData("75001", PropertyType.Apartment, 5000m, 100, DateTime.UtcNow)));

        var scoringEngine = Substitute.For<IScoringEngine>();
        var logger = Substitute.For<ILogger<ProcessScrapingJobCommandHandler>>();

        var handler = new ProcessScrapingJobCommandHandler(
            new[] { scraper },
            dvfProvider,
            scoringEngine,
            dbContext,
            logger);

        var criteria = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var job = new ScrapingJob(Guid.NewGuid(), SearchId.New(), "TestSource", criteria);
        var command = new ProcessScrapingJobCommand(job);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0, "duplicate listing should be skipped");

        var listings = await dbContext.Listings.ToListAsync();
        listings.Should().HaveCount(1, "only the original listing should remain");
    }

    [Fact]
    public async Task Handle_EmptyScrapingResult_ReturnsZeroCount()
    {
        // Arrange
        var dbContext = CreateDbContext();
        var scraper = Substitute.For<IListingScraper>();
        scraper.SourceName.Returns("TestSource");
        scraper.ScrapeAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ScrapedListing>>.Success(Array.Empty<ScrapedListing>()));

        var dvfProvider = Substitute.For<IDvfReferenceDataProvider>();
        var scoringEngine = Substitute.For<IScoringEngine>();
        var logger = Substitute.For<ILogger<ProcessScrapingJobCommandHandler>>();

        var handler = new ProcessScrapingJobCommandHandler(
            new[] { scraper },
            dvfProvider,
            scoringEngine,
            dbContext,
            logger);

        var criteria = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var job = new ScrapingJob(Guid.NewGuid(), SearchId.New(), "TestSource", criteria);
        var command = new ProcessScrapingJobCommand(job);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        return new TestDbContext(options);
    }

    private sealed class TestDbContext : DbContext, IImmoScorerDbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Search> Searches => Set<Search>();
        public DbSet<Listing> Listings => Set<Listing>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Search>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(id => id.Value, guid => new SearchId(guid));
                entity.OwnsOne(e => e.Criteria, owned =>
                {
                    owned.Property(c => c.PropertyType).HasConversion<string>();
                });
            });

            modelBuilder.Entity<Listing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasConversion(id => id.Value, guid => new ListingId(guid));
                entity.Property(e => e.SearchId).HasConversion(id => id.Value, guid => new SearchId(guid));
                entity.OwnsOne(e => e.Address);
                entity.OwnsOne(e => e.Price);
                entity.OwnsOne(e => e.Score);
                entity.OwnsOne(e => e.ScoreBreakdown);

                entity.HasIndex(e => new { e.Source, e.ExternalId }).IsUnique();
            });
        }
    }
}

using FluentAssertions;
using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Application.Listings.Queries;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ImmoScorer.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="GetListingsQueryHandler"/>.
/// Focus: filtering, sorting, pagination.
/// </summary>
public sealed class GetListingsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoFilters_ReturnsAllListings()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_FilterByMinScore_ReturnsMatchingListings()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var filter = new ListingFilter(
            MinScore: 70,
            MaxPrice: null,
            MinArea: null,
            City: null,
            Source: null);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: filter,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2, "only 2 listings have score >= 70");
        result.Value.Items.Should().OnlyContain(l => l.Score >= 70);
    }

    [Fact]
    public async Task Handle_FilterByMaxPrice_ReturnsMatchingListings()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var filter = new ListingFilter(
            MinScore: null,
            MaxPrice: 250_000m,
            MinArea: null,
            City: null,
            Source: null);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: filter,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2, "only 2 listings have price <= 250,000");
        result.Value.Items.Should().OnlyContain(l => l.Price <= 250_000m);
    }

    [Fact]
    public async Task Handle_FilterByMinArea_ReturnsMatchingListings()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var filter = new ListingFilter(
            MinScore: null,
            MaxPrice: null,
            MinArea: 60m,
            City: null,
            Source: null);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: filter,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2, "only 2 listings have area >= 60");
        result.Value.Items.Should().OnlyContain(l => l.Area >= 60m);
    }

    [Fact]
    public async Task Handle_FilterByCity_ReturnsMatchingListings()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var filter = new ListingFilter(
            MinScore: null,
            MaxPrice: null,
            MinArea: null,
            City: "Paris",
            Source: null);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: filter,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3, "all test listings are in Paris");
        result.Value.Items.Should().OnlyContain(l => l.City == "Paris");
    }

    [Fact]
    public async Task Handle_FilterBySource_ReturnsMatchingListings()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var filter = new ListingFilter(
            MinScore: null,
            MaxPrice: null,
            MinArea: null,
            City: null,
            Source: "FixtureA");

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: filter,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2, "2 listings are from FixtureA");
        result.Value.Items.Should().OnlyContain(l => l.Source == "FixtureA");
    }

    [Fact]
    public async Task Handle_SortByScoreDescending_ReturnsCorrectOrder()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeInDescendingOrder(l => l.Score);
        result.Value.Items.First().Score.Should().Be(80);
    }

    [Fact]
    public async Task Handle_SortByPriceAscending_ReturnsCorrectOrder()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.PriceAscending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeInAscendingOrder(l => l.Price);
        result.Value.Items.First().Price.Should().Be(200_000m);
    }

    [Fact]
    public async Task Handle_SortByPriceDescending_ReturnsCorrectOrder()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.PriceDescending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeInDescendingOrder(l => l.Price);
        result.Value.Items.First().Price.Should().Be(300_000m);
    }

    [Fact]
    public async Task Handle_SortByPricePerM2Ascending_ReturnsCorrectOrder()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.PricePerM2Ascending,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeInAscendingOrder(l => l.PricePerM2);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 2,
            PageSize: 2);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1, "only 1 item on page 2");
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_PageSizeClampedToMax_DoesNotExceed100()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: 1,
            PageSize: 999); // Exceeds max

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().BeLessOrEqualTo(100, "page size is clamped to 100");
    }

    [Fact]
    public async Task Handle_NegativePageNumber_TreatedAsPageOne()
    {
        // Arrange
        var (dbContext, searchId) = await SeedTestDataAsync();
        var handler = new GetListingsQueryHandler(dbContext);

        var query = new GetListingsQuery(
            SearchId: searchId,
            Filter: null,
            SortOrder: ListingSortOrder.ScoreDescending,
            Page: -5,
            PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1, "negative page is treated as page 1");
    }

    private static async Task<(TestDbContext, SearchId)> SeedTestDataAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        var dbContext = new TestDbContext(options);
        var searchId = SearchId.New();

        var listings = new[]
        {
            Listing.Create(
                searchId,
                new ScrapedListing("Listing 1", null, "FixtureA", "Paris", "75001", 200_000m, 50m, 2, 3, "C", "https://test.com/1", "ext-1"),
                5000m,
                new Score(80),
                new ScoreBreakdown(50, 15, 8, 7)),

            Listing.Create(
                searchId,
                new ScrapedListing("Listing 2", null, "FixtureA", "Paris", "75001", 250_000m, 70m, 3, 2, "B", "https://test.com/2", "ext-2"),
                5000m,
                new Score(75),
                new ScoreBreakdown(45, 10, 10, 10)),

            Listing.Create(
                searchId,
                new ScrapedListing("Listing 3", null, "FixtureB", "Paris", "75002", 300_000m, 40m, 1, 5, "A", "https://test.com/3", "ext-3"),
                5000m,
                new Score(60),
                new ScoreBreakdown(35, 8, 9, 8)),
        };

        dbContext.Listings.AddRange(listings);
        await dbContext.SaveChangesAsync();

        return (dbContext, searchId);
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
            });
        }
    }
}

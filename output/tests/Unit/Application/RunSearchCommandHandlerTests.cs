using FluentAssertions;
using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Application.Searches.Commands;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ImmoScorer.Tests.Unit.Application;

/// <summary>
/// Unit tests for <see cref="RunSearchCommandHandler"/>.
/// Focus: search creation, validation, persistence.
/// </summary>
public sealed class RunSearchCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_CreatesSearchAndReturnsId()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<RunSearchCommandHandler>>();
        var handler = new RunSearchCommandHandler(dbContext, logger);

        var criteria = new SearchCriteria(
            City: "Paris",
            PostalCode: "75001",
            PropertyType: PropertyType.Apartment,
            MinPrice: 100_000m,
            MaxPrice: 500_000m,
            MinArea: 30m,
            MaxArea: 100m,
            MinRooms: 2,
            MaxRooms: 4);

        var command = new RunSearchCommand(criteria);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(default);

        var savedSearch = await dbContext.Searches.FirstOrDefaultAsync();
        savedSearch.Should().NotBeNull();
        savedSearch!.Criteria.City.Should().Be("Paris");
        savedSearch.Criteria.PostalCode.Should().Be("75001");
        savedSearch.Status.Should().Be(SearchStatus.Created);
    }

    [Fact]
    public async Task Handle_ValidCommand_LogsSearchCreation()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<RunSearchCommandHandler>>();
        var handler = new RunSearchCommandHandler(dbContext, logger);

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

        var command = new RunSearchCommand(criteria);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        logger.Received(1).LogInformation(
            Arg.Is<string>(s => s.Contains("created")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task Handle_MultipleCommands_CreatesMultipleSearches()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        var dbContext = new TestDbContext(options);
        var logger = Substitute.For<ILogger<RunSearchCommandHandler>>();
        var handler = new RunSearchCommandHandler(dbContext, logger);

        var criteria1 = new SearchCriteria("Paris", "75001", PropertyType.Apartment, null, null, null, null, null, null);
        var criteria2 = new SearchCriteria("Lyon", "69001", PropertyType.House, null, null, null, null, null, null);

        // Act
        var result1 = await handler.Handle(new RunSearchCommand(criteria1), CancellationToken.None);
        var result2 = await handler.Handle(new RunSearchCommand(criteria2), CancellationToken.None);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
        result1.Value.Should().NotBe(result2.Value, "each search should have unique ID");

        var searches = await dbContext.Searches.ToListAsync();
        searches.Should().HaveCount(2);
    }

    // Helper DbContext for testing
    private sealed class TestDbContext : DbContext, IImmoScorerDbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Search> Searches => Set<Search>();
        public DbSet<Listing> Listings => Set<Listing>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Minimal configuration for in-memory testing
            modelBuilder.Entity<Search>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasConversion(
                        id => id.Value,
                        guid => new SearchId(guid));

                entity.OwnsOne(e => e.Criteria, owned =>
                {
                    owned.Property(c => c.PropertyType).HasConversion<string>();
                });
            });

            modelBuilder.Entity<Listing>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasConversion(
                        id => id.Value,
                        guid => new ListingId(guid));

                entity.Property(e => e.SearchId)
                    .HasConversion(
                        id => id.Value,
                        guid => new SearchId(guid));

                entity.OwnsOne(e => e.Address);
                entity.OwnsOne(e => e.Price);
                entity.OwnsOne(e => e.Score);
                entity.OwnsOne(e => e.ScoreBreakdown);
            });
        }
    }
}

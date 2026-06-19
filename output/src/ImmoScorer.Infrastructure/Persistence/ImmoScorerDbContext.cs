using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ImmoScorer.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for the ImmoScorer application.
/// Uses SQLite for local development; configured via the connection string in appsettings.
/// </summary>
public sealed class ImmoScorerDbContext : DbContext, IImmoScorerDbContext
{
    /// <summary>Initialises a new instance with the given options.</summary>
    public ImmoScorerDbContext(DbContextOptions<ImmoScorerDbContext> options)
        : base(options) { }

    /// <inheritdoc/>
    public DbSet<Search> Searches => Set<Search>();

    /// <inheritdoc/>
    public DbSet<Listing> Listings => Set<Listing>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Search ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Search>(b =>
        {
            b.HasKey(s => s.Id);
            b.Property(s => s.Id)
                .HasConversion(id => id.Value, g => new SearchId(g));

            b.Property(s => s.Status)
                .HasConversion<string>();

            b.HasIndex(s => s.CreatedAt);

            b.OwnsOne(s => s.Criteria, c =>
            {
                c.Property(x => x.PropertyType).HasConversion<string>();
                c.Property(x => x.City).HasMaxLength(200);
                c.Property(x => x.PostalCode).HasMaxLength(10);
            });

            b.HasMany<Listing>()
                .WithOne()
                .HasForeignKey(l => l.SearchId)
                .HasPrincipalKey(s => s.Id)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Listing ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Listing>(b =>
        {
            b.HasKey(l => l.Id);
            b.Property(l => l.Id)
                .HasConversion(id => id.Value, g => new ListingId(g));

            b.Property(l => l.SearchId)
                .HasConversion(id => id.Value, g => new SearchId(g));

            b.Property(l => l.Title).HasMaxLength(500);
            b.Property(l => l.Source).HasMaxLength(100);
            b.Property(l => l.ExternalId).HasMaxLength(200);
            b.Property(l => l.OriginalUrl).HasMaxLength(2000);
            b.Property(l => l.EnergyRating).HasMaxLength(2);

            b.OwnsOne(l => l.Address, a =>
            {
                a.Property(x => x.City).HasMaxLength(200).HasColumnName("Address_City");
                a.Property(x => x.PostalCode).HasMaxLength(10).HasColumnName("Address_PostalCode");
                a.Property(x => x.Neighborhood).HasMaxLength(200).HasColumnName("Address_Neighborhood");
            });

            b.OwnsOne(l => l.Price, p =>
            {
                p.Property(x => x.Amount).HasColumnName("Price_Amount");
                p.Property(x => x.Currency).HasMaxLength(3).HasColumnName("Price_Currency");
            });

            b.OwnsOne(l => l.Score, s =>
            {
                s.Property(x => x.Value).HasColumnName("Score_Value");
            });

            b.OwnsOne(l => l.ScoreBreakdown, sb =>
            {
                sb.Property(x => x.PriceGapScore).HasColumnName("ScoreBreakdown_PriceGapScore");
                sb.Property(x => x.AreaScore).HasColumnName("ScoreBreakdown_AreaScore");
                sb.Property(x => x.FloorScore).HasColumnName("ScoreBreakdown_FloorScore");
                sb.Property(x => x.EnergyScore).HasColumnName("ScoreBreakdown_EnergyScore");
            });

            // Index: (SearchId, Score DESC) for default sorted query
            b.HasIndex(l => new { l.SearchId, ScoreValue = l.Score.Value });

            // Unique index: (Source, ExternalId) for deduplication
            b.HasIndex(l => new { l.Source, l.ExternalId }).IsUnique();
        });
    }
}

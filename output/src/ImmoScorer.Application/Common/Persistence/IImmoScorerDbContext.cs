using ImmoScorer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ImmoScorer.Application.Common.Persistence;

/// <summary>
/// Abstraction over the EF Core database context, used by application-layer handlers.
/// Allows the application layer to remain independent of a specific EF Core provider.
/// </summary>
public interface IImmoScorerDbContext
{
    /// <summary>Gets the searches table.</summary>
    DbSet<Search> Searches { get; }

    /// <summary>Gets the listings table.</summary>
    DbSet<Listing> Listings { get; }

    /// <summary>Saves all pending changes to the underlying database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

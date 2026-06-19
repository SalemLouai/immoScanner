import React, { useCallback, useEffect, useState } from 'react';
import { getListings } from '../api/client';
import type { Listing, ListingFilter, PaginatedList, SortOrder } from '../types';
import { FilterPanel } from '../components/FilterPanel';
import { ListingCard } from '../components/ListingCard';

interface ListingsPageProps {
  searchId: string;
  onBack: () => void;
  onSelectListing: (listingId: string) => void;
}

/** Page displaying a paginated, filtered list of listings for a search. */
export const ListingsPage: React.FC<ListingsPageProps> = ({
  searchId,
  onBack,
  onSelectListing,
}) => {
  const [data, setData] = useState<PaginatedList<Listing> | null>(null);
  const [filter, setFilter] = useState<ListingFilter>({});
  const [sort, setSort] = useState<SortOrder>('score_desc');
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getListings(searchId, filter, sort, page, 20);
      setData(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erreur de chargement');
    } finally {
      setLoading(false);
    }
  }, [searchId, filter, sort, page]);

  useEffect(() => { void load(); }, [load]);

  const handleFilterChange = (f: ListingFilter) => {
    setFilter(f);
    setPage(1);
  };

  const handleSortChange = (s: SortOrder) => {
    setSort(s);
    setPage(1);
  };

  return (
    <div>
      <button
        style={{ background: 'none', border: 'none', color: '#2563eb', cursor: 'pointer', fontSize: '0.875rem', marginBottom: 16, padding: 0 }}
        onClick={onBack}
      >
        ← Retour aux recherches
      </button>

      <div style={{ display: 'grid', gridTemplateColumns: '260px 1fr', gap: 20, alignItems: 'start' }}>
        {/* Filter panel */}
        <FilterPanel
          filter={filter}
          sort={sort}
          onFilterChange={handleFilterChange}
          onSortChange={handleSortChange}
        />

        {/* Listing list */}
        <div>
          {loading && (
            <div style={{ textAlign: 'center', color: '#9ca3af', padding: 40 }}>Chargement...</div>
          )}
          {error && (
            <div style={{ color: '#dc2626', padding: 12, background: '#fef2f2', borderRadius: 6 }}>{error}</div>
          )}
          {!loading && !error && data && (
            <>
              <div style={{ marginBottom: 12, fontSize: '0.875rem', color: '#6b7280' }}>
                {data.totalCount} annonce{data.totalCount !== 1 ? 's' : ''} trouvée{data.totalCount !== 1 ? 's' : ''}
              </div>

              {data.items.length === 0 ? (
                <div style={{ textAlign: 'center', color: '#9ca3af', padding: 40 }}>
                  Aucune annonce correspondant aux filtres.
                </div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
                  {data.items.map((listing) => (
                    <ListingCard
                      key={listing.id}
                      listing={listing}
                      onSelect={onSelectListing}
                    />
                  ))}
                </div>
              )}

              {/* Pagination */}
              {data.totalPages > 1 && (
                <div style={{ display: 'flex', justifyContent: 'center', gap: 8, marginTop: 20 }}>
                  <button
                    disabled={!data.hasPreviousPage}
                    style={{ padding: '6px 14px', borderRadius: 4, border: '1px solid #d1d5db', cursor: data.hasPreviousPage ? 'pointer' : 'not-allowed', opacity: data.hasPreviousPage ? 1 : 0.5 }}
                    onClick={() => setPage((p) => p - 1)}
                  >
                    ← Précédent
                  </button>
                  <span style={{ padding: '6px 12px', fontSize: '0.875rem', color: '#374151' }}>
                    Page {data.page} / {data.totalPages}
                  </span>
                  <button
                    disabled={!data.hasNextPage}
                    style={{ padding: '6px 14px', borderRadius: 4, border: '1px solid #d1d5db', cursor: data.hasNextPage ? 'pointer' : 'not-allowed', opacity: data.hasNextPage ? 1 : 0.5 }}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    Suivant →
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
};

import type {
  ListingDetail,
  ListingFilter,
  PaginatedList,
  Listing,
  SavedSearch,
  SearchCriteria,
  SortOrder,
} from '../types';

const BASE_URL = '';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options,
  });

  if (!response.ok) {
    const body = await response.text();
    throw new Error(`HTTP ${response.status}: ${body}`);
  }

  return response.json() as Promise<T>;
}

// ── Searches ──────────────────────────────────────────────────────────────────

/** Creates a new search and returns the new search ID. */
export async function createSearch(
  criteria: SearchCriteria
): Promise<{ searchId: string }> {
  return fetchJson<{ searchId: string }>(`${BASE_URL}/searches`, {
    method: 'POST',
    body: JSON.stringify({ criteria }),
  });
}

/** Returns all saved searches ordered by creation date descending. */
export async function listSearches(): Promise<SavedSearch[]> {
  return fetchJson<SavedSearch[]>(`${BASE_URL}/searches`);
}

/** Triggers scraping for the given search ID. */
export async function triggerScraping(searchId: string): Promise<void> {
  await fetchJson<unknown>(`${BASE_URL}/searches/${searchId}/scrape`, {
    method: 'POST',
  });
}

// ── Listings ──────────────────────────────────────────────────────────────────

/** Returns a paginated, filtered and sorted list of listings for a search. */
export async function getListings(
  searchId: string,
  filter?: ListingFilter,
  sort: SortOrder = 'score_desc',
  page = 1,
  pageSize = 20
): Promise<PaginatedList<Listing>> {
  const params = new URLSearchParams({ searchId, sort, page: String(page), pageSize: String(pageSize) });

  if (filter?.minScore != null) params.set('minScore', String(filter.minScore));
  if (filter?.maxPrice != null) params.set('maxPrice', String(filter.maxPrice));
  if (filter?.minArea != null) params.set('minArea', String(filter.minArea));
  if (filter?.city) params.set('city', filter.city);
  if (filter?.source) params.set('source', filter.source);

  return fetchJson<PaginatedList<Listing>>(`${BASE_URL}/listings?${params}`);
}

/** Returns the full detail of a single listing. */
export async function getListingDetail(listingId: string): Promise<ListingDetail> {
  return fetchJson<ListingDetail>(`${BASE_URL}/listings/${listingId}`);
}

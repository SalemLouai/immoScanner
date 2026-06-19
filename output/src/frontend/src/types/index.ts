// ── Domain types matching backend DTOs ──────────────────────────────────────

export interface SearchCriteria {
  city: string;
  postalCode: string;
  propertyType: 'Any' | 'Apartment' | 'House';
  minPrice?: number;
  maxPrice?: number;
  minArea?: number;
  maxArea?: number;
  minRooms?: number;
  maxRooms?: number;
}

export interface SavedSearch {
  id: string;
  city: string;
  postalCode: string;
  propertyType: string;
  status: string;
  listingCount: number;
  createdAt: string;
  completedAt?: string;
}

export interface Listing {
  id: string;
  title: string;
  source: string;
  city: string;
  postalCode: string;
  price: number;
  area: number;
  pricePerM2: number;
  referencePricePerM2: number;
  score: number;
  originalUrl: string;
  scrapedAt: string;
}

export interface ScoreBreakdown {
  priceGapScore: number;
  areaScore: number;
  floorScore: number;
  energyScore: number;
  totalScore: number;
}

export interface ListingDetail extends Listing {
  description?: string;
  rooms?: number;
  floor?: number;
  energyRating?: string;
  scoreBreakdown: ScoreBreakdown;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export type SortOrder =
  | 'score_desc'
  | 'price_asc'
  | 'price_desc'
  | 'price_per_m2_asc'
  | 'date_desc';

export interface ListingFilter {
  minScore?: number;
  maxPrice?: number;
  minArea?: number;
  city?: string;
  source?: string;
}

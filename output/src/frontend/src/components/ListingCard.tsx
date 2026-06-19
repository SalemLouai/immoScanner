import React from 'react';
import type { Listing } from '../types';
import { ScoreBadge } from './ScoreBadge';

interface ListingCardProps {
  listing: Listing;
  onSelect: (id: string) => void;
}

/** Card component for a listing in the list view. */
export const ListingCard: React.FC<ListingCardProps> = ({ listing, onSelect }) => {
  const priceGap = listing.referencePricePerM2 > 0
    ? ((listing.pricePerM2 - listing.referencePricePerM2) / listing.referencePricePerM2 * 100).toFixed(1)
    : null;

  return (
    <div
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: 8,
        padding: 16,
        cursor: 'pointer',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'flex-start',
        gap: 12,
        backgroundColor: '#fff',
        boxShadow: '0 1px 3px rgba(0,0,0,0.07)',
        transition: 'box-shadow 0.15s',
      }}
      onClick={() => onSelect(listing.id)}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => e.key === 'Enter' && onSelect(listing.id)}
    >
      <div style={{ flex: 1, minWidth: 0 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 4 }}>
          <span style={{ fontSize: '0.75rem', color: '#6b7280', background: '#f3f4f6', borderRadius: 4, padding: '1px 6px' }}>
            {listing.source}
          </span>
          <span style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
            {new Date(listing.scrapedAt).toLocaleDateString('fr-FR')}
          </span>
        </div>
        <h3 style={{ margin: '0 0 8px', fontSize: '1rem', fontWeight: 600, color: '#111827', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {listing.title}
        </h3>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 16, fontSize: '0.875rem', color: '#374151' }}>
          <span><strong>{listing.price.toLocaleString('fr-FR')} €</strong></span>
          {listing.area > 0 && <span>{listing.area} m²</span>}
          {listing.area > 0 && <span>{listing.pricePerM2.toLocaleString('fr-FR', { maximumFractionDigits: 0 })} €/m²</span>}
          <span style={{ color: '#6b7280' }}>{listing.city} ({listing.postalCode})</span>
        </div>
        {priceGap !== null && (
          <div style={{ marginTop: 6, fontSize: '0.8rem', color: parseFloat(priceGap) < 0 ? '#16a34a' : '#dc2626' }}>
            {parseFloat(priceGap) < 0 ? '▼' : '▲'} {Math.abs(parseFloat(priceGap))}% vs référence DVF
          </div>
        )}
      </div>
      <div style={{ flexShrink: 0, textAlign: 'center' }}>
        <div style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: 4 }}>Score</div>
        <ScoreBadge score={listing.score} size="lg" />
      </div>
    </div>
  );
};

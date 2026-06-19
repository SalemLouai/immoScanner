import React, { useEffect, useState } from 'react';
import { getListingDetail } from '../api/client';
import type { ListingDetail } from '../types';
import { ScoreBadge } from '../components/ScoreBadge';

interface ListingDetailPageProps {
  listingId: string;
  onBack: () => void;
}

/** Detailed view of a single listing including score breakdown and DVF comparison. */
export const ListingDetailPage: React.FC<ListingDetailPageProps> = ({
  listingId,
  onBack,
}) => {
  const [listing, setListing] = useState<ListingDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await getListingDetail(listingId);
        setListing(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Erreur de chargement');
      } finally {
        setLoading(false);
      }
    })();
  }, [listingId]);

  if (loading) return <div style={{ textAlign: 'center', padding: 40, color: '#9ca3af' }}>Chargement...</div>;
  if (error) return <div style={{ color: '#dc2626', padding: 12 }}>{error}</div>;
  if (!listing) return null;

  const priceGap = listing.referencePricePerM2 > 0
    ? ((listing.pricePerM2 - listing.referencePricePerM2) / listing.referencePricePerM2 * 100)
    : null;

  const barStyle = (value: number, max: number): React.CSSProperties => ({
    height: 8,
    borderRadius: 4,
    background: '#2563eb',
    width: `${Math.round((value / max) * 100)}%`,
  });

  return (
    <div>
      <button
        style={{ background: 'none', border: 'none', color: '#2563eb', cursor: 'pointer', fontSize: '0.875rem', marginBottom: 16, padding: 0 }}
        onClick={onBack}
      >
        ← Retour aux annonces
      </button>

      <div style={{ background: '#fff', border: '1px solid #e5e7eb', borderRadius: 10, overflow: 'hidden' }}>
        {/* Header */}
        <div style={{ padding: 24, borderBottom: '1px solid #e5e7eb', display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 16 }}>
          <div style={{ flex: 1 }}>
            <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
              <span style={{ fontSize: '0.75rem', color: '#6b7280', background: '#f3f4f6', borderRadius: 4, padding: '2px 8px' }}>{listing.source}</span>
              {listing.energyRating && (
                <span style={{ fontSize: '0.75rem', color: '#fff', background: listing.energyRating <= 'B' ? '#16a34a' : listing.energyRating <= 'D' ? '#ca8a04' : '#dc2626', borderRadius: 4, padding: '2px 8px', fontWeight: 700 }}>
                  DPE {listing.energyRating}
                </span>
              )}
            </div>
            <h1 style={{ margin: '0 0 8px', fontSize: '1.25rem', color: '#111827' }}>{listing.title}</h1>
            <div style={{ color: '#6b7280', fontSize: '0.875rem' }}>
              {listing.city} ({listing.postalCode})
              {listing.floor != null && ` · Étage ${listing.floor}`}
              {listing.rooms != null && ` · ${listing.rooms} pièce${listing.rooms > 1 ? 's' : ''}`}
            </div>
          </div>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: 4 }}>Score opportunité</div>
            <ScoreBadge score={listing.score} size="lg" />
          </div>
        </div>

        {/* Price grid */}
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 0, borderBottom: '1px solid #e5e7eb' }}>
          {[
            { label: 'Prix', value: `${listing.price.toLocaleString('fr-FR')} €` },
            { label: 'Surface', value: `${listing.area} m²` },
            { label: 'Prix/m²', value: `${listing.pricePerM2.toLocaleString('fr-FR', { maximumFractionDigits: 0 })} €/m²` },
          ].map(({ label, value }) => (
            <div key={label} style={{ padding: 16, textAlign: 'center', borderRight: '1px solid #e5e7eb' }}>
              <div style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: 4 }}>{label}</div>
              <div style={{ fontWeight: 700, fontSize: '1rem', color: '#111827' }}>{value}</div>
            </div>
          ))}
        </div>

        {/* DVF comparison */}
        {listing.referencePricePerM2 > 0 && (
          <div style={{ padding: 20, borderBottom: '1px solid #e5e7eb', background: '#f9fafb' }}>
            <h3 style={{ margin: '0 0 12px', fontSize: '0.9rem', color: '#374151' }}>Comparaison DVF</h3>
            <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', marginBottom: 8 }}>
              <span>Référence DVF (médiane)</span>
              <strong>{listing.referencePricePerM2.toLocaleString('fr-FR', { maximumFractionDigits: 0 })} €/m²</strong>
            </div>
            {priceGap !== null && (
              <div style={{ color: priceGap < 0 ? '#16a34a' : '#dc2626', fontWeight: 600, fontSize: '0.875rem' }}>
                {priceGap < 0 ? '▼' : '▲'} {Math.abs(priceGap).toFixed(1)}% {priceGap < 0 ? 'sous' : 'au-dessus de'} la médiane du marché
              </div>
            )}
          </div>
        )}

        {/* Score breakdown */}
        <div style={{ padding: 20, borderBottom: '1px solid #e5e7eb' }}>
          <h3 style={{ margin: '0 0 16px', fontSize: '0.9rem', color: '#374151' }}>Détail du score</h3>
          {[
            { label: 'Écart de prix', value: listing.scoreBreakdown.priceGapScore, max: 60 },
            { label: 'Surface', value: listing.scoreBreakdown.areaScore, max: 15 },
            { label: 'Étage', value: listing.scoreBreakdown.floorScore, max: 10 },
            { label: 'Énergie (DPE)', value: listing.scoreBreakdown.energyScore, max: 15 },
          ].map(({ label, value, max }) => (
            <div key={label} style={{ marginBottom: 12 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem', marginBottom: 4 }}>
                <span style={{ color: '#374151' }}>{label}</span>
                <span style={{ color: '#6b7280' }}>{value} / {max}</span>
              </div>
              <div style={{ background: '#e5e7eb', borderRadius: 4, height: 8 }}>
                <div style={barStyle(value, max)} />
              </div>
            </div>
          ))}
        </div>

        {/* Description */}
        {listing.description && (
          <div style={{ padding: 20, borderBottom: '1px solid #e5e7eb' }}>
            <h3 style={{ margin: '0 0 10px', fontSize: '0.9rem', color: '#374151' }}>Description</h3>
            <p style={{ margin: 0, fontSize: '0.875rem', color: '#4b5563', lineHeight: 1.6 }}>{listing.description}</p>
          </div>
        )}

        {/* Link */}
        <div style={{ padding: 20 }}>
          <a
            href={listing.originalUrl}
            target="_blank"
            rel="noopener noreferrer"
            style={{ color: '#2563eb', fontSize: '0.875rem', fontWeight: 600, textDecoration: 'none' }}
          >
            Voir l'annonce originale →
          </a>
        </div>
      </div>
    </div>
  );
};

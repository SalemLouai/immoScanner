import React, { useState } from 'react';
import type { ListingFilter, SortOrder } from '../types';

interface FilterPanelProps {
  filter: ListingFilter;
  sort: SortOrder;
  onFilterChange: (filter: ListingFilter) => void;
  onSortChange: (sort: SortOrder) => void;
}

/** Filter and sort panel for the listings list view. */
export const FilterPanel: React.FC<FilterPanelProps> = ({
  filter,
  sort,
  onFilterChange,
  onSortChange,
}) => {
  const [local, setLocal] = useState<ListingFilter>(filter);

  const handleApply = () => onFilterChange(local);

  const handleReset = () => {
    const empty: ListingFilter = {};
    setLocal(empty);
    onFilterChange(empty);
  };

  const inputStyle: React.CSSProperties = {
    border: '1px solid #d1d5db',
    borderRadius: 4,
    padding: '6px 10px',
    fontSize: '0.875rem',
    width: '100%',
    boxSizing: 'border-box',
  };

  const labelStyle: React.CSSProperties = {
    fontSize: '0.8rem',
    fontWeight: 600,
    color: '#374151',
    display: 'block',
    marginBottom: 4,
  };

  return (
    <div style={{ background: '#f9fafb', border: '1px solid #e5e7eb', borderRadius: 8, padding: 16 }}>
      <h4 style={{ margin: '0 0 12px', fontSize: '0.9rem', color: '#111827' }}>Filtres & tri</h4>

      {/* Sort */}
      <div style={{ marginBottom: 12 }}>
        <label style={labelStyle}>Trier par</label>
        <select
          style={inputStyle}
          value={sort}
          onChange={(e) => onSortChange(e.target.value as SortOrder)}
        >
          <option value="score_desc">Score (meilleur d'abord)</option>
          <option value="price_asc">Prix croissant</option>
          <option value="price_desc">Prix décroissant</option>
          <option value="price_per_m2_asc">Prix/m² croissant</option>
          <option value="date_desc">Date (plus récent)</option>
        </select>
      </div>

      {/* Min score */}
      <div style={{ marginBottom: 12 }}>
        <label style={labelStyle}>Score minimum</label>
        <input
          type="number"
          style={inputStyle}
          min={0}
          max={100}
          placeholder="ex. 60"
          value={local.minScore ?? ''}
          onChange={(e) => setLocal({ ...local, minScore: e.target.value ? Number(e.target.value) : undefined })}
        />
      </div>

      {/* Max price */}
      <div style={{ marginBottom: 12 }}>
        <label style={labelStyle}>Prix maximum (€)</label>
        <input
          type="number"
          style={inputStyle}
          min={0}
          placeholder="ex. 300000"
          value={local.maxPrice ?? ''}
          onChange={(e) => setLocal({ ...local, maxPrice: e.target.value ? Number(e.target.value) : undefined })}
        />
      </div>

      {/* Min area */}
      <div style={{ marginBottom: 12 }}>
        <label style={labelStyle}>Surface minimale (m²)</label>
        <input
          type="number"
          style={inputStyle}
          min={0}
          placeholder="ex. 40"
          value={local.minArea ?? ''}
          onChange={(e) => setLocal({ ...local, minArea: e.target.value ? Number(e.target.value) : undefined })}
        />
      </div>

      {/* Source */}
      <div style={{ marginBottom: 16 }}>
        <label style={labelStyle}>Source</label>
        <select
          style={inputStyle}
          value={local.source ?? ''}
          onChange={(e) => setLocal({ ...local, source: e.target.value || undefined })}
        >
          <option value="">Toutes</option>
          <option value="LeBonCoin">LeBonCoin</option>
          <option value="SeLoger">SeLoger</option>
          <option value="Fixture">Fixture</option>
        </select>
      </div>

      <div style={{ display: 'flex', gap: 8 }}>
        <button
          style={{ flex: 1, background: '#2563eb', color: '#fff', border: 'none', borderRadius: 4, padding: '8px 0', cursor: 'pointer', fontWeight: 600 }}
          onClick={handleApply}
        >
          Appliquer
        </button>
        <button
          style={{ flex: 1, background: '#f3f4f6', color: '#374151', border: '1px solid #d1d5db', borderRadius: 4, padding: '8px 0', cursor: 'pointer' }}
          onClick={handleReset}
        >
          Réinitialiser
        </button>
      </div>
    </div>
  );
};

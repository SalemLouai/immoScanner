import React, { useState } from 'react';
import type { SearchCriteria } from '../types';
import { createSearch, triggerScraping } from '../api/client';

interface SearchFormProps {
  onCreated: (searchId: string) => void;
}

const defaultCriteria: SearchCriteria = {
  city: '',
  postalCode: '',
  propertyType: 'Any',
};

/** Form for creating a new search and triggering scraping. */
export const SearchForm: React.FC<SearchFormProps> = ({ onCreated }) => {
  const [criteria, setCriteria] = useState<SearchCriteria>(defaultCriteria);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const inputStyle: React.CSSProperties = {
    border: '1px solid #d1d5db',
    borderRadius: 4,
    padding: '8px 12px',
    fontSize: '0.875rem',
    width: '100%',
    boxSizing: 'border-box',
  };

  const labelStyle: React.CSSProperties = {
    fontSize: '0.85rem',
    fontWeight: 600,
    color: '#374151',
    display: 'block',
    marginBottom: 4,
  };

  const fieldStyle: React.CSSProperties = { marginBottom: 12 };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const { searchId } = await createSearch(criteria);
      await triggerScraping(searchId);
      setCriteria(defaultCriteria);
      onCreated(searchId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Une erreur est survenue.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 0 }}>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, marginBottom: 12 }}>
        <div style={fieldStyle}>
          <label style={labelStyle}>Ville *</label>
          <input
            required
            style={inputStyle}
            placeholder="Paris"
            value={criteria.city}
            onChange={(e) => setCriteria({ ...criteria, city: e.target.value })}
          />
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Code postal *</label>
          <input
            required
            pattern="\d{5}"
            style={inputStyle}
            placeholder="75001"
            value={criteria.postalCode}
            onChange={(e) => setCriteria({ ...criteria, postalCode: e.target.value })}
          />
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Type de bien</label>
          <select
            style={inputStyle}
            value={criteria.propertyType}
            onChange={(e) => setCriteria({ ...criteria, propertyType: e.target.value as SearchCriteria['propertyType'] })}
          >
            <option value="Any">Tous</option>
            <option value="Apartment">Appartement</option>
            <option value="House">Maison</option>
          </select>
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Chambres min.</label>
          <input
            type="number"
            style={inputStyle}
            min={1}
            placeholder="1"
            value={criteria.minRooms ?? ''}
            onChange={(e) => setCriteria({ ...criteria, minRooms: e.target.value ? Number(e.target.value) : undefined })}
          />
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Prix min. (€)</label>
          <input
            type="number"
            style={inputStyle}
            min={0}
            placeholder="50000"
            value={criteria.minPrice ?? ''}
            onChange={(e) => setCriteria({ ...criteria, minPrice: e.target.value ? Number(e.target.value) : undefined })}
          />
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Prix max. (€)</label>
          <input
            type="number"
            style={inputStyle}
            min={0}
            placeholder="500000"
            value={criteria.maxPrice ?? ''}
            onChange={(e) => setCriteria({ ...criteria, maxPrice: e.target.value ? Number(e.target.value) : undefined })}
          />
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Surface min. (m²)</label>
          <input
            type="number"
            style={inputStyle}
            min={0}
            placeholder="30"
            value={criteria.minArea ?? ''}
            onChange={(e) => setCriteria({ ...criteria, minArea: e.target.value ? Number(e.target.value) : undefined })}
          />
        </div>
        <div style={fieldStyle}>
          <label style={labelStyle}>Surface max. (m²)</label>
          <input
            type="number"
            style={inputStyle}
            min={0}
            placeholder="200"
            value={criteria.maxArea ?? ''}
            onChange={(e) => setCriteria({ ...criteria, maxArea: e.target.value ? Number(e.target.value) : undefined })}
          />
        </div>
      </div>

      {error && (
        <div style={{ color: '#dc2626', fontSize: '0.875rem', marginBottom: 12 }}>{error}</div>
      )}

      <button
        type="submit"
        disabled={loading}
        style={{
          background: '#2563eb',
          color: '#fff',
          border: 'none',
          borderRadius: 6,
          padding: '10px 0',
          fontSize: '0.95rem',
          fontWeight: 600,
          cursor: loading ? 'not-allowed' : 'pointer',
          opacity: loading ? 0.7 : 1,
        }}
      >
        {loading ? 'Création en cours...' : 'Lancer la recherche'}
      </button>
    </form>
  );
};

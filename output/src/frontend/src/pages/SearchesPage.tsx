import React, { useCallback, useEffect, useState } from 'react';
import { listSearches, triggerScraping } from '../api/client';
import type { SavedSearch } from '../types';
import { SearchForm } from '../components/SearchForm';

interface SearchesPageProps {
  onSelectSearch: (searchId: string) => void;
}

/** Page displaying saved searches with the ability to create new ones. */
export const SearchesPage: React.FC<SearchesPageProps> = ({ onSelectSearch }) => {
  const [searches, setSearches] = useState<SavedSearch[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await listSearches();
      setSearches(data);
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const handleCreated = async (searchId: string) => {
    setShowForm(false);
    await load();
    onSelectSearch(searchId);
  };

  const handleReRun = async (searchId: string) => {
    try {
      await triggerScraping(searchId);
      await load();
    } catch {
      // ignore
    }
  };

  const statusColor = (status: string) =>
    status === 'Completed' ? '#16a34a'
    : status === 'InProgress' ? '#ca8a04'
    : status === 'Failed' ? '#dc2626'
    : '#6b7280';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <h2 style={{ margin: 0, fontSize: '1.25rem', color: '#111827' }}>Mes recherches</h2>
        <button
          style={{ background: '#2563eb', color: '#fff', border: 'none', borderRadius: 6, padding: '8px 16px', cursor: 'pointer', fontWeight: 600 }}
          onClick={() => setShowForm(!showForm)}
        >
          {showForm ? 'Annuler' : '+ Nouvelle recherche'}
        </button>
      </div>

      {showForm && (
        <div style={{ background: '#fff', border: '1px solid #e5e7eb', borderRadius: 8, padding: 20, marginBottom: 20 }}>
          <h3 style={{ margin: '0 0 16px', fontSize: '1rem' }}>Nouvelle recherche</h3>
          <SearchForm onCreated={handleCreated} />
        </div>
      )}

      {loading ? (
        <div style={{ textAlign: 'center', color: '#9ca3af', padding: 40 }}>Chargement...</div>
      ) : searches.length === 0 ? (
        <div style={{ textAlign: 'center', color: '#9ca3af', padding: 40 }}>
          Aucune recherche. Créez-en une pour commencer.
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {searches.map((s) => (
            <div
              key={s.id}
              style={{
                border: '1px solid #e5e7eb',
                borderRadius: 8,
                padding: 16,
                background: '#fff',
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
              }}
            >
              <div
                style={{ flex: 1, cursor: 'pointer' }}
                onClick={() => onSelectSearch(s.id)}
              >
                <div style={{ fontWeight: 600, color: '#111827', marginBottom: 4 }}>
                  {s.city} ({s.postalCode}) — {s.propertyType}
                </div>
                <div style={{ fontSize: '0.8rem', color: '#6b7280' }}>
                  <span style={{ color: statusColor(s.status), fontWeight: 600 }}>{s.status}</span>
                  {' · '}
                  {s.listingCount} annonce{s.listingCount !== 1 ? 's' : ''}
                  {' · '}
                  {new Date(s.createdAt).toLocaleDateString('fr-FR')}
                </div>
              </div>
              <button
                style={{ background: '#f3f4f6', border: '1px solid #d1d5db', borderRadius: 4, padding: '6px 12px', cursor: 'pointer', fontSize: '0.8rem' }}
                onClick={() => void handleReRun(s.id)}
                title="Relancer le scraping"
              >
                Relancer
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

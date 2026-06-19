import React, { useState } from 'react';
import { SearchesPage } from './pages/SearchesPage';
import { ListingsPage } from './pages/ListingsPage';
import { ListingDetailPage } from './pages/ListingDetailPage';

type View =
  | { type: 'searches' }
  | { type: 'listings'; searchId: string }
  | { type: 'detail'; listingId: string; searchId: string };

/** Root application component with client-side view routing. */
const App: React.FC = () => {
  const [view, setView] = useState<View>({ type: 'searches' });

  return (
    <div style={{ minHeight: '100vh', background: '#f3f4f6', fontFamily: 'system-ui, -apple-system, sans-serif' }}>
      {/* Nav */}
      <header style={{ background: '#1e3a5f', color: '#fff', padding: '12px 24px', display: 'flex', alignItems: 'center', gap: 16 }}>
        <span
          style={{ fontWeight: 700, fontSize: '1.1rem', cursor: 'pointer' }}
          onClick={() => setView({ type: 'searches' })}
        >
          ImmoScorer
        </span>
        {view.type !== 'searches' && (
          <>
            <span style={{ color: '#93c5fd' }}>/</span>
            <span
              style={{ color: '#93c5fd', cursor: 'pointer', fontSize: '0.9rem' }}
              onClick={() =>
                view.type === 'detail'
                  ? setView({ type: 'listings', searchId: view.searchId })
                  : setView({ type: 'searches' })
              }
            >
              {view.type === 'listings' ? 'Annonces' : 'Détail'}
            </span>
          </>
        )}
      </header>

      <main style={{ maxWidth: 1100, margin: '0 auto', padding: '24px 20px' }}>
        {view.type === 'searches' && (
          <SearchesPage
            onSelectSearch={(id) => setView({ type: 'listings', searchId: id })}
          />
        )}
        {view.type === 'listings' && (
          <ListingsPage
            searchId={view.searchId}
            onBack={() => setView({ type: 'searches' })}
            onSelectListing={(id) =>
              setView({ type: 'detail', listingId: id, searchId: view.searchId })
            }
          />
        )}
        {view.type === 'detail' && (
          <ListingDetailPage
            listingId={view.listingId}
            onBack={() =>
              setView({ type: 'listings', searchId: view.searchId })
            }
          />
        )}
      </main>
    </div>
  );
};

export default App;

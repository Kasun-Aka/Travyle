import { useState } from 'react';
import Login from './components/Login';
import { SupportDashboard } from './pages/SupportDashboard';
import { VouchersPage } from './pages/VouchersPage';
import { CustomerReviewsPage } from './pages/CustomerReviewsPage';

type ViewMode = 'login' | 'support' | 'vouchers' | 'reviews';

function App() {
  const [currentView, setCurrentView] = useState<ViewMode>('support');

  if (currentView === 'login') {
    return (
      <div>
        <Login />
        <div style={{ position: 'fixed', bottom: 20, right: 20, zIndex: 999 }}>
          <button 
            onClick={() => setCurrentView('support')}
            style={{ 
              background: '#4F46E5', 
              color: 'white', 
              border: 'none', 
              padding: '10px 16px', 
              borderRadius: 8, 
              fontWeight: 600,
              cursor: 'pointer',
              boxShadow: '0 4px 6px -1px rgba(0,0,0,0.1)'
            }}
          >
            Go to Support Admin Console &rarr;
          </button>
        </div>
      </div>
    );
  }

  if (currentView === 'vouchers') {
    return (
      <VouchersPage
        onNavigateSupport={() => setCurrentView('support')}
        onNavigateReviews={() => setCurrentView('reviews')}
        onLogout={() => setCurrentView('login')}
      />
    );
  }

  if (currentView === 'reviews') {
    return (
      <CustomerReviewsPage 
        onNavigateSupport={() => setCurrentView('support')}
        onNavigateVouchers={() => setCurrentView('vouchers')}
        onLogout={() => setCurrentView('login')}
      />
    );
  }

  return (
    <SupportDashboard 
      onNavigateVouchers={() => setCurrentView('vouchers')}
      onNavigateReviews={() => setCurrentView('reviews')}
      onLogout={() => setCurrentView('login')}
    />
  );
}

export default App;

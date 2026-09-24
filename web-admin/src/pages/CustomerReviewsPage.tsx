import React, { useState, useEffect } from 'react';
import type { CustomerReviewItem } from '../types/support';
import { supportApi } from '../services/supportApi';
import './CustomerReviews.css';

interface CustomerReviewsPageProps {
  onNavigateSupport: () => void;
  onNavigateVouchers?: () => void;
  onLogout?: () => void;
}

export const CustomerReviewsPage: React.FC<CustomerReviewsPageProps> = ({ 
  onNavigateSupport, 
  onNavigateVouchers, 
  onLogout 
}) => {
  const [reviews, setReviews] = useState<CustomerReviewItem[]>([]);
  const [loading, setLoading] = useState(true);
  const defaultTourId = '33333333-3333-3333-3333-333333333333';

  useEffect(() => {
    const fetchReviews = async () => {
      setLoading(true);
      try {
        const data = await supportApi.getReviewsByTour(defaultTourId);
        setReviews(data);
      } catch (err) {
        console.error('Failed to fetch reviews:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchReviews();
  }, []);

  const renderStars = (rating: number) => {
    return '★'.repeat(rating) + '☆'.repeat(5 - rating);
  };

  return (
    <div className="support-dashboard-layout">
      {/* Sidebar Navigation */}
      <aside className="dashboard-sidebar">
        <div className="sidebar-brand">
          <div className="sidebar-logo">T</div>
          <div className="brand-text">
            <h2>Travyle</h2>
            <span>Support Admin</span>
          </div>
        </div>

        <nav className="sidebar-nav">
          <button className="nav-item" onClick={onNavigateSupport}>
            <span>🎫</span> Support Tickets
          </button>
          <button className="nav-item" onClick={onNavigateVouchers}>
            <span>🎁</span> Goodwill Vouchers
          </button>
          <button className="nav-item active">
            <span>⭐</span> Customer Reviews
          </button>
        </nav>

        <div className="sidebar-footer">
          <div style={{ marginBottom: 8 }}>Support Operations v1.0</div>
          {onLogout && (
            <button 
              onClick={onLogout}
              style={{ background: 'transparent', border: '1px solid #334155', color: '#94A3B8', padding: '6px 12px', borderRadius: 6, cursor: 'pointer', width: '100%' }}
            >
              Sign Out
            </button>
          )}
        </div>
      </aside>

      {/* Main Content */}
      <main className="dashboard-main">
        <div className="main-header">
          <div className="header-title">
            <h1>Customer Reviews & Quality Ratings</h1>
            <p>Monitor verified feedback and ratings submitted by travelers across tours.</p>
          </div>
        </div>

        {loading ? (
          <p style={{ color: '#64748B' }}>Loading verified traveler reviews...</p>
        ) : (
          <div className="reviews-grid">
            {reviews.length === 0 ? (
              <p style={{ color: '#64748B' }}>No reviews submitted yet.</p>
            ) : (
              reviews.map((r) => (
                <div key={r.id} className="review-card">
                  <div className="review-card-header">
                    <div className="reviewer-info">
                      <div className="reviewer-avatar">
                        {(r.userName || 'T').charAt(0).toUpperCase()}
                      </div>
                      <div className="reviewer-name">{r.userName || 'Traveler'}</div>
                    </div>
                    <div className="review-stars">{renderStars(r.rating)}</div>
                  </div>

                  <p className="review-comment">"{r.comment}"</p>

                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    {r.isVerified && <span className="verified-tag">✓ Verified Traveler</span>}
                    <span style={{ fontSize: 12, color: '#94A3B8' }}>
                      {new Date(r.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </main>
    </div>
  );
};

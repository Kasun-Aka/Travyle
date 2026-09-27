import React, { useState, useEffect } from 'react';
import type { CustomerReviewItem } from '../types/support';
import { supportApi } from '../services/supportApi';
import './CustomerReviews.css';

import { useNavigate } from 'react-router-dom';

interface CustomerReviewsPageProps {
  onNavigateSupport?: () => void;
  onNavigateVouchers?: () => void;
  onLogout?: () => void;
}

export const CustomerReviewsPage: React.FC<CustomerReviewsPageProps> = ({ 
  onNavigateSupport, 
  onNavigateVouchers, 
  onLogout 
}) => {
  const navigate = useNavigate();
  const handleGoSupport = onNavigateSupport || (() => navigate('/support/tickets'));
  const handleGoVouchers = onNavigateVouchers || (() => navigate('/support/vouchers'));
  const handleLogout = onLogout || (() => navigate('/login'));
  const [reviews, setReviews] = useState<CustomerReviewItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchReviews = async () => {
      setLoading(true);
      try {
        const data = await supportApi.getReviewsByTour();
        setReviews(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error('Failed to fetch reviews:', err);
        setReviews([]);
      } finally {
        setLoading(false);
      }
    };
    fetchReviews();
  }, []);

  const handleToggleVerify = async (reviewId: string, currentStatus: boolean) => {
    try {
      const updated = await supportApi.toggleReviewVerification(reviewId, !currentStatus);
      setReviews((prev) => prev.map((r) => (r.id === reviewId ? { ...r, isVerified: updated.isVerified } : r)));
    } catch (err) {
      console.error('Failed to update review verification status:', err);
      alert('Error updating review verification status.');
    }
  };

  const handleDeleteReview = async (reviewId: string) => {
    if (!window.confirm('Are you sure you want to delete this customer review?')) return;
    try {
      await supportApi.deleteReview(reviewId);
      setReviews((prev) => prev.filter((r) => r.id !== reviewId));
    } catch (err) {
      console.error('Failed to delete review:', err);
      alert('Error deleting review.');
    }
  };

  const renderStars = (rating: number) => {
    const valid = Math.max(0, Math.min(5, Math.floor(rating || 0)));
    return '★'.repeat(valid) + '☆'.repeat(5 - valid);
  };

  const safeReviews = Array.isArray(reviews) ? reviews : [];

  return (
    <div className="support-dashboard-layout" style={{ padding: 24 }}>
      {/* Feature Sub-Navigation Header */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 20, borderBottom: '1px solid #E2E8F0', paddingBottom: 12 }}>
        <button
          onClick={handleGoSupport}
          style={{ padding: '8px 16px', borderRadius: 8, background: '#F1F5F9', color: '#475569', border: 'none', fontWeight: 600, cursor: 'pointer' }}
        >
          🎫 Support Queue
        </button>
        <button
          onClick={handleGoVouchers}
          style={{ padding: '8px 16px', borderRadius: 8, background: '#F1F5F9', color: '#475569', border: 'none', fontWeight: 600, cursor: 'pointer' }}
        >
          🎁 Goodwill Vouchers
        </button>
        <button
          style={{ padding: '8px 16px', borderRadius: 8, background: '#4F46E5', color: 'white', border: 'none', fontWeight: 600, cursor: 'pointer' }}
        >
          ⭐ Customer Reviews
        </button>
      </div>

      {/* Main Content */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
        <div className="main-header">
          <div className="header-title">
            <h1>Customer Reviews & Quality Ratings</h1>
            <p>Monitor verified feedback and ratings submitted by travelers across tours.</p>
          </div>
          {onLogout && (
            <button className="btn-review" onClick={handleLogout}>
              Sign Out
            </button>
          )}
        </div>

        {loading ? (
          <p style={{ color: '#64748B' }}>Loading verified traveler reviews...</p>
        ) : (
          <div className="reviews-grid">
            {safeReviews.length === 0 ? (
              <p style={{ color: '#64748B' }}>No reviews submitted yet.</p>
            ) : (
              safeReviews.map((r) => (
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

                  <p className="review-comment">"{r.comment || ''}"</p>

                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <span className={`verified-tag ${r.isVerified ? '' : 'unverified'}`} style={{ background: r.isVerified ? '#DCFCE7' : '#F1F5F9', color: r.isVerified ? '#16A34A' : '#64748B', padding: '2px 8px', borderRadius: 12, fontSize: 12, fontWeight: 600 }}>
                        {r.isVerified ? '✓ Verified Traveler' : 'Unverified'}
                      </span>
                      <button
                        onClick={() => handleToggleVerify(r.id, r.isVerified)}
                        style={{ background: 'none', border: 'none', color: '#4F46E5', fontSize: 12, fontWeight: 600, cursor: 'pointer', textDecoration: 'underline' }}
                      >
                        {r.isVerified ? 'Mark Unverified' : 'Verify Review'}
                      </button>
                    </div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                      <span style={{ fontSize: 12, color: '#94A3B8' }}>
                        {r.createdAt ? new Date(r.createdAt).toLocaleDateString() : ''}
                      </span>
                      <button
                        onClick={() => handleDeleteReview(r.id)}
                        style={{ background: 'none', border: 'none', color: '#EF4444', fontSize: 12, fontWeight: 600, cursor: 'pointer' }}
                        title="Delete Review"
                      >
                        🗑️ Delete
                      </button>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </div>
    </div>
  );
};

import React, { useState, useEffect } from 'react';
import type { CustomerReviewItem } from '../types/support';
import { supportApi } from '../services/supportApi';
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
    <div className="flex flex-col min-h-full w-full bg-[#F8F9FB] font-sans p-6">
      {/* Feature Sub-Navigation Header */}
      <div className="flex gap-3 mb-5 border-b border-slate-200 pb-3">
        <button
          onClick={handleGoSupport}
          className="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 border-none font-semibold cursor-pointer transition-colors hover:bg-slate-200"
        >
          🎫 Support Queue
        </button>
        <button
          onClick={handleGoVouchers}
          className="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 border-none font-semibold cursor-pointer transition-colors hover:bg-slate-200"
        >
          🎁 Goodwill Vouchers
        </button>
        <button
          className="px-4 py-2 rounded-lg bg-indigo-600 text-white border-none font-semibold cursor-pointer"
        >
          ⭐ Customer Reviews
        </button>
      </div>

      {/* Main Content */}
      <div className="flex flex-col gap-7">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="m-0 mb-1 text-[26px] font-extrabold text-slate-900">Customer Reviews & Quality Ratings</h1>
            <p className="m-0 text-[14px] text-slate-500">Monitor verified feedback and ratings submitted by travelers across tours.</p>
          </div>
          {onLogout && (
            <button className="px-[14px] py-[8px] rounded-md border border-indigo-200 bg-indigo-50 text-indigo-600 hover:bg-indigo-600 hover:text-white text-[13px] font-semibold cursor-pointer transition-colors" onClick={handleLogout}>
              Sign Out
            </button>
          )}
        </div>

        {loading ? (
          <p className="text-slate-500">Loading verified traveler reviews...</p>
        ) : (
          <div className="grid grid-cols-[repeat(auto-fill,minmax(320px,1fr))] gap-5">
            {safeReviews.length === 0 ? (
              <p className="text-slate-500 col-span-full">No reviews submitted yet.</p>
            ) : (
              safeReviews.map((r) => (
                <div key={r.id} className="flex flex-col gap-3 bg-white rounded-xl p-6 border border-slate-200 shadow-sm">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2.5">
                      <div className="w-9 h-9 bg-indigo-50 text-indigo-600 rounded-full flex items-center justify-center font-bold text-[14px]">
                        {(r.userName || 'T').charAt(0).toUpperCase()}
                      </div>
                      <div className="font-bold text-slate-900 text-[14px]">{r.userName || 'Traveler'}</div>
                    </div>
                    <div className="text-amber-500 text-[16px] tracking-widest">{renderStars(r.rating)}</div>
                  </div>

                  <p className="text-[14px] leading-relaxed text-slate-600 m-0">"{r.comment || ''}"</p>

                  <div className="flex justify-between items-center mt-2">
                    <div className="flex items-center gap-2">
                      <span className={`inline-flex items-center gap-1 text-[11px] font-semibold px-2 py-0.5 rounded-full ${r.isVerified ? 'bg-green-100 text-green-700' : 'bg-slate-100 text-slate-500'}`}>
                        {r.isVerified ? '✓ Verified Traveler' : 'Unverified'}
                      </span>
                      <button
                        onClick={() => handleToggleVerify(r.id, r.isVerified)}
                        className="bg-transparent border-none text-indigo-600 text-[12px] font-semibold cursor-pointer underline hover:text-indigo-800"
                      >
                        {r.isVerified ? 'Mark Unverified' : 'Verify Review'}
                      </button>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-[12px] text-slate-400">
                        {r.createdAt ? new Date(r.createdAt).toLocaleDateString() : ''}
                      </span>
                      <button
                        onClick={() => handleDeleteReview(r.id)}
                        className="bg-transparent border-none text-red-500 text-[12px] font-semibold cursor-pointer hover:text-red-700 p-1"
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

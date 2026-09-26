import React, { useState, useEffect, useCallback } from 'react';
import type { VoucherItem } from '../types/support';
import { supportApi } from '../services/supportApi';
import './SupportDashboard.css';
import './VouchersPage.css';

import { useNavigate } from 'react-router-dom';

interface VouchersPageProps {
  onNavigateSupport?: () => void;
  onNavigateReviews?: () => void;
  onLogout?: () => void;
}

export const VouchersPage: React.FC<VouchersPageProps> = ({
  onNavigateSupport,
  onNavigateReviews,
  onLogout,
}) => {
  const navigate = useNavigate();
  const handleGoSupport = onNavigateSupport || (() => navigate('/support/tickets'));
  const handleGoReviews = onNavigateReviews || (() => navigate('/support/reviews'));
  const handleLogout = onLogout || (() => navigate('/login'));
  const [vouchers, setVouchers] = useState<VoucherItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [showIssueModal, setShowIssueModal] = useState(false);

  // Form state for direct voucher issuance
  const [formUserId] = useState('11111111-1111-1111-1111-111111111111');
  const [formAmount, setFormAmount] = useState(50);
  const [formReason, setFormReason] = useState('Goodwill compensation for tour delay');
  const [formExpiryDays, setFormExpiryDays] = useState(90);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const fetchVouchers = useCallback(async () => {
    setLoading(true);
    try {
      const data = await supportApi.getAllVouchers(statusFilter);
      setVouchers(Array.isArray(data) ? data : []);
    } catch (err) {
      console.error('Failed to load vouchers:', err);
      setVouchers([]);
    } finally {
      setLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    fetchVouchers();
  }, [fetchVouchers]);

  // Statistics
  const safeVouchers = Array.isArray(vouchers) ? vouchers : [];
  const totalCount = safeVouchers.length;
  const draftCount = safeVouchers.filter((v) => v?.status?.toLowerCase() === 'draft').length;
  const activeCount = safeVouchers.filter((v) => v?.status?.toLowerCase() === 'active').length;
  const totalValue = safeVouchers.reduce((acc, v) => acc + (v?.amount || 0), 0);

  const handleApproveVoucher = async (voucherId: string) => {
    try {
      await supportApi.approveVoucher(voucherId, {
        notes: 'Signed and approved from Goodwill Vouchers Management console.',
        sendNotification: true,
      });
      fetchVouchers();
    } catch (err) {
      console.error('Error approving voucher:', err);
      alert('Failed to approve voucher.');
    }
  };

  const handleRejectVoucher = async (voucherId: string) => {
    const reason = prompt('Enter reason for declining this goodwill voucher:', 'Over-compensation threshold exceeded');
    if (reason === null) return;
    try {
      await supportApi.rejectVoucher(voucherId, { reason });
      fetchVouchers();
    } catch (err) {
      console.error('Error rejecting voucher:', err);
      alert('Failed to decline voucher.');
    }
  };

  const handleDeleteVoucher = async (voucherId: string) => {
    if (!window.confirm('Are you sure you want to delete this voucher?')) return;
    try {
      await supportApi.deleteVoucher(voucherId);
      fetchVouchers();
    } catch (err) {
      console.error('Error deleting voucher:', err);
      alert('Failed to delete voucher.');
    }
  };

  const handleIssueSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    try {
      await supportApi.issueVoucher({
        userId: formUserId,
        amount: formAmount,
        reason: formReason,
        expiryDays: formExpiryDays,
      });
      setShowIssueModal(false);
      fetchVouchers();
    } catch (err) {
      console.error('Failed to issue voucher:', err);
      alert('Error creating voucher.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const copyCode = (code: string) => {
    navigator.clipboard.writeText(code);
    alert(`Voucher code "${code}" copied to clipboard!`);
  };

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
          style={{ padding: '8px 16px', borderRadius: 8, background: '#4F46E5', color: 'white', border: 'none', fontWeight: 600, cursor: 'pointer' }}
        >
          🎁 Goodwill Vouchers
        </button>
        <button
          onClick={handleGoReviews}
          style={{ padding: '8px 16px', borderRadius: 8, background: '#F1F5F9', color: '#475569', border: 'none', fontWeight: 600, cursor: 'pointer' }}
        >
          ⭐ Customer Reviews
        </button>
      </div>

      {/* Main Content Area */}
      <div style={{ display: 'flex', flexDirection: 'column', gap: 28 }}>
        {/* Header */}
        <div className="main-header">
          <div className="header-title">
            <h1>Goodwill Voucher Issuance & Sign-Off Panel</h1>
            <p>Issue, review, and authorize goodwill discount vouchers generated by the AI Support Agent.</p>
          </div>
          <div className="vouchers-header-actions">
            <button className="btn-issue-voucher" onClick={() => setShowIssueModal(true)}>
              + Issue New Voucher
            </button>
            <button className="btn-primary" onClick={fetchVouchers}>
              🔄 Refresh List
            </button>
            {onLogout && (
              <button className="btn-review" onClick={handleLogout}>
                Sign Out
              </button>
            )}
          </div>
        </div>

        {/* Stat Cards */}
        <div className="stats-grid">
          <div className="stat-card">
            <span className="stat-label">Total Vouchers Issued</span>
            <span className="stat-value">{totalCount}</span>
            <span className="stat-subtext" style={{ color: '#64748B' }}>Total generated</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Pending Admin Approval</span>
            <span className="stat-value stat-warning">{draftCount}</span>
            <span className="stat-subtext stat-warning">AI Drafts awaiting sign-off</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Active in Wallets</span>
            <span className="stat-value stat-success">{activeCount}</span>
            <span className="stat-subtext stat-success">Ready for redemption</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Total Goodwill Value</span>
            <span className="stat-value" style={{ color: '#4F46E5' }}>${totalValue.toFixed(2)}</span>
            <span className="stat-subtext" style={{ color: '#64748B' }}>Compensation distributed</span>
          </div>
        </div>

        {/* Controls / Filter Bar */}
        <div className="controls-bar">
          <div className="filter-group">
            <div className="priority-tabs">
              {['ALL', 'Draft', 'Active', 'Redeemed', 'Expired'].map((st) => (
                <button
                  key={st}
                  className={`tab-btn ${statusFilter === st ? 'active' : ''}`}
                  onClick={() => setStatusFilter(st)}
                >
                  {st === 'Draft' ? '⏳ Draft / Pending' : st}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Vouchers Table */}
        <div className="table-card">
          <table className="ticket-table">
            <thead>
              <tr>
                <th>Voucher Code</th>
                <th>Traveler Recipient</th>
                <th>Amount</th>
                <th>Reason / Trigger</th>
                <th>Status</th>
                <th>Expires On</th>
                <th style={{ textAlign: 'right' }}>Admin Sign-Off Action</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#64748B' }}>
                    Loading goodwill vouchers...
                  </td>
                </tr>
              ) : vouchers.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#64748B' }}>
                    No vouchers found for selected filter.
                  </td>
                </tr>
              ) : (
                vouchers.map((v) => {
                  const isDraft = v.status.toLowerCase() === 'draft';
                  const isActive = v.status.toLowerCase() === 'active';

                  return (
                    <tr key={v.id}>
                      <td>
                        <span className="voucher-code-pill">
                          {v.code}
                          <button
                            className="btn-copy-mini"
                            onClick={() => copyCode(v.code)}
                            title="Copy code"
                          >
                            📋
                          </button>
                        </span>
                      </td>
                      <td>
                        <div style={{ fontWeight: 600, color: '#0F172A' }}>
                          {v.userName || 'Sarah Jenkins (Traveler)'}
                        </div>
                      </td>
                      <td>
                        <span style={{ fontSize: 16, fontWeight: 800, color: '#15803D' }}>
                          ${v.amount.toFixed(2)}
                        </span>
                      </td>
                      <td>
                        <span style={{ fontSize: 13, color: '#475569' }}>{v.reason}</span>
                      </td>
                      <td>
                        <span
                          className={`badge ${
                            isDraft
                              ? 'badge-draft'
                              : isActive
                              ? 'badge-active'
                              : 'badge-redeemed'
                          }`}
                        >
                          {isDraft ? '⏳ Pending Sign-Off' : v.status}
                        </span>
                      </td>
                      <td>
                        <span style={{ fontSize: 13, color: '#64748B' }}>
                          {v.expiresAt ? new Date(v.expiresAt).toLocaleDateString() : '90 Days'}
                        </span>
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end', alignItems: 'center' }}>
                          {isDraft ? (
                            <>
                              <button
                                className="btn-sign-approve"
                                onClick={() => handleApproveVoucher(v.id)}
                              >
                                ✓ Approve
                              </button>
                              <button
                                style={{ padding: '6px 12px', background: '#FEE2E2', color: '#DC2626', border: '1px solid #FCA5A5', borderRadius: 8, cursor: 'pointer', fontWeight: 600, fontSize: 13 }}
                                onClick={() => handleRejectVoucher(v.id)}
                              >
                                ✕ Decline
                              </button>
                            </>
                          ) : (
                            <span style={{ fontSize: 12, color: isActive ? '#16A34A' : '#64748B', fontWeight: 600 }}>
                              {isActive ? '✓ Active' : v.status}
                            </span>
                          )}
                          <button
                            style={{ background: 'none', border: 'none', color: '#EF4444', fontSize: 13, cursor: 'pointer', marginLeft: 4 }}
                            onClick={() => handleDeleteVoucher(v.id)}
                            title="Delete Voucher"
                          >
                            🗑️
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Direct Voucher Issue Modal */}
      {showIssueModal && (
        <div className="modal-overlay" onClick={() => setShowIssueModal(false)}>
          <div className="ticket-modal-container" style={{ maxWidth: 540 }} onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <div className="modal-header-left">
                <h2 className="modal-header-title">🎁 Issue Goodwill Voucher</h2>
              </div>
              <button className="modal-close-btn" onClick={() => setShowIssueModal(false)}>
                &times;
              </button>
            </div>

            <form onSubmit={handleIssueSubmit} style={{ padding: 24, display: 'flex', flexDirection: 'column', gap: 16 }}>
              <div className="voucher-form-group">
                <label>Voucher Amount ($ USD)</label>
                <input
                  type="number"
                  min="5"
                  max="500"
                  value={formAmount}
                  onChange={(e) => setFormAmount(Number(e.target.value))}
                  required
                />
              </div>

              <div className="voucher-form-group">
                <label>Reason / Compensation Context</label>
                <input
                  type="text"
                  value={formReason}
                  onChange={(e) => setFormReason(e.target.value)}
                  placeholder="e.g. Tour delay, vehicle breakdown, missed sunset stop"
                  required
                />
              </div>

              <div className="voucher-form-group">
                <label>Validity Duration (Days)</label>
                <input
                  type="number"
                  min="7"
                  max="365"
                  value={formExpiryDays}
                  onChange={(e) => setFormExpiryDays(Number(e.target.value))}
                  required
                />
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 12, marginTop: 12 }}>
                <button
                  type="button"
                  onClick={() => setShowIssueModal(false)}
                  style={{
                    padding: '10px 18px',
                    border: '1px solid #CBD5E1',
                    background: 'white',
                    borderRadius: 8,
                    cursor: 'pointer',
                    fontWeight: 600,
                  }}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="btn-issue-voucher"
                >
                  {isSubmitting ? 'Issuing...' : 'Issue & Activate Voucher'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

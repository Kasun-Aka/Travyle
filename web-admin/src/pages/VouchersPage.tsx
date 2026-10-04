import React, { useState, useEffect, useCallback } from 'react';
import type { VoucherItem } from '../types/support';
import { supportApi } from '../services/supportApi';
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
          className="px-4 py-2 rounded-lg bg-indigo-600 text-white border-none font-semibold cursor-pointer"
        >
          🎁 Goodwill Vouchers
        </button>
        <button
          onClick={handleGoReviews}
          className="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 border-none font-semibold cursor-pointer transition-colors hover:bg-slate-200"
        >
          ⭐ Customer Reviews
        </button>
      </div>

      {/* Main Content Area */}
      <div className="flex flex-col gap-7">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="m-0 mb-1 text-[26px] font-extrabold text-slate-900">Goodwill Voucher Issuance & Sign-Off Panel</h1>
            <p className="m-0 text-[14px] text-slate-500">Issue, review, and authorize goodwill discount vouchers generated by the AI Support Agent.</p>
          </div>
          <div className="flex gap-3">
            <button className="flex items-center gap-2 px-5 py-2.5 rounded-lg bg-green-600 hover:bg-green-700 text-white border-none text-[14px] font-semibold cursor-pointer transition-colors" onClick={() => setShowIssueModal(true)}>
              + Issue New Voucher
            </button>
            <button className="flex items-center gap-2 px-5 py-2.5 rounded-lg bg-indigo-600 hover:bg-indigo-700 text-white border-none text-[14px] font-semibold cursor-pointer transition-colors" onClick={fetchVouchers}>
              🔄 Refresh List
            </button>
            {onLogout && (
              <button className="px-[14px] py-[8px] rounded-md border border-indigo-200 bg-indigo-50 text-indigo-600 hover:bg-indigo-600 hover:text-white text-[13px] font-semibold cursor-pointer transition-colors" onClick={handleLogout}>
                Sign Out
              </button>
            )}
          </div>
        </div>

        {/* Stat Cards */}
        <div className="grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-5">
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Total Vouchers Issued</span>
            <span className="text-[28px] font-extrabold text-slate-900">{totalCount}</span>
            <span className="text-[12px] font-medium text-slate-500">Total generated</span>
          </div>
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Pending Admin Approval</span>
            <span className="text-[28px] font-extrabold text-amber-600">{draftCount}</span>
            <span className="text-[12px] font-medium text-amber-600">AI Drafts awaiting sign-off</span>
          </div>
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Active in Wallets</span>
            <span className="text-[28px] font-extrabold text-green-600">{activeCount}</span>
            <span className="text-[12px] font-medium text-green-600">Ready for redemption</span>
          </div>
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Total Goodwill Value</span>
            <span className="text-[28px] font-extrabold text-indigo-600">${totalValue.toFixed(2)}</span>
            <span className="text-[12px] font-medium text-slate-500">Compensation distributed</span>
          </div>
        </div>

        {/* Controls / Filter Bar */}
        <div className="flex items-center justify-between gap-4 flex-wrap p-4 px-5 bg-white border border-slate-200 rounded-xl">
          <div className="flex items-center gap-3">
            <div className="flex gap-1 p-1 bg-slate-100 rounded-lg">
              {['ALL', 'Draft', 'Active', 'Redeemed', 'Expired'].map((st) => (
                <button
                  key={st}
                  className={`px-3 py-1.5 rounded-md border-none text-[13px] font-semibold cursor-pointer transition-colors ${statusFilter === st ? 'bg-white text-indigo-600 shadow-sm' : 'bg-transparent text-slate-500 hover:text-slate-700'}`}
                  onClick={() => setStatusFilter(st)}
                >
                  {st === 'Draft' ? '⏳ Draft / Pending' : st}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* Vouchers Table */}
        <div className="bg-white border border-slate-200 rounded-xl shadow-sm overflow-hidden">
          <table className="w-full border-collapse text-left">
            <thead>
              <tr>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Voucher Code</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Traveler Recipient</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Amount</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Reason / Trigger</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Status</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Expires On</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide text-right">Admin Sign-Off Action</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} className="text-center p-10 text-slate-500">
                    Loading goodwill vouchers...
                  </td>
                </tr>
              ) : vouchers.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center p-10 text-slate-500">
                    No vouchers found for selected filter.
                  </td>
                </tr>
              ) : (
                vouchers.map((v) => {
                  const isDraft = v.status.toLowerCase() === 'draft';
                  const isActive = v.status.toLowerCase() === 'active';

                  return (
                    <tr key={v.id} className="hover:bg-slate-50 border-b border-slate-100 last:border-0">
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                        <span className="inline-flex items-center gap-1.5 font-mono text-[13px] font-bold text-green-700 bg-green-100 px-2 py-1 rounded-md tracking-wide">
                          {v.code}
                          <button
                            className="bg-transparent border-none cursor-pointer p-0.5 text-green-600 text-[11px] hover:text-green-800"
                            onClick={() => copyCode(v.code)}
                            title="Copy code"
                          >
                            📋
                          </button>
                        </span>
                      </td>
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                        <div className="font-semibold text-slate-900">
                          {v.userName || 'Sarah Jenkins (Traveler)'}
                        </div>
                      </td>
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                        <span className="text-[16px] font-extrabold text-green-700">
                          ${v.amount.toFixed(2)}
                        </span>
                      </td>
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                        <span className="text-[13px] text-slate-600">{v.reason}</span>
                      </td>
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                        <span
                          className={`inline-block px-2.5 py-1 rounded-md text-[12px] font-bold ${
                            isDraft
                              ? 'bg-amber-100 text-amber-800'
                              : isActive
                              ? 'bg-green-100 text-green-700'
                              : 'bg-slate-200 text-slate-600'
                          }`}
                        >
                          {isDraft ? '⏳ Pending Sign-Off' : v.status}
                        </span>
                      </td>
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                        <span className="text-[13px] text-slate-500">
                          {v.expiresAt ? new Date(v.expiresAt).toLocaleDateString() : '90 Days'}
                        </span>
                      </td>
                      <td className="px-5 py-4 align-middle text-[14px] text-slate-700 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          {isDraft ? (
                            <>
                              <button
                                className="px-3 py-1.5 rounded-md bg-green-600 hover:bg-green-700 text-white border-none text-[13px] font-semibold cursor-pointer transition-colors"
                                onClick={() => handleApproveVoucher(v.id)}
                              >
                                ✓ Approve
                              </button>
                              <button
                                className="px-3 py-1.5 rounded-md bg-red-100 text-red-600 border border-red-300 hover:bg-red-200 text-[13px] font-semibold cursor-pointer transition-colors"
                                onClick={() => handleRejectVoucher(v.id)}
                              >
                                ✕ Decline
                              </button>
                            </>
                          ) : (
                            <span className={`text-[12px] font-semibold ${isActive ? 'text-green-600' : 'text-slate-500'}`}>
                              {isActive ? '✓ Active' : v.status}
                            </span>
                          )}
                          <button
                            className="bg-transparent border-none text-red-500 text-[13px] cursor-pointer p-1.5 hover:bg-red-50 rounded ml-1"
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
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 p-4" onClick={() => setShowIssueModal(false)}>
          <div className="bg-white rounded-xl shadow-xl w-full max-w-[540px] flex flex-col max-h-[90vh] overflow-hidden" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between p-5 border-b border-slate-200">
              <h2 className="m-0 text-[18px] font-bold text-slate-900">🎁 Issue Goodwill Voucher</h2>
              <button className="bg-transparent border-none text-slate-400 text-[24px] cursor-pointer hover:text-slate-600" onClick={() => setShowIssueModal(false)}>
                &times;
              </button>
            </div>

            <form onSubmit={handleIssueSubmit} className="flex flex-col gap-4 p-6 overflow-y-auto">
              <div className="flex flex-col gap-1.5">
                <label className="text-[13px] font-semibold text-slate-700">Voucher Amount ($ USD)</label>
                <input
                  type="number"
                  min="5"
                  max="500"
                  value={formAmount}
                  onChange={(e) => setFormAmount(Number(e.target.value))}
                  required
                  className="px-3.5 py-2.5 border border-slate-300 rounded-lg text-[14px] text-slate-900 outline-none focus:border-indigo-600 font-inherit"
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[13px] font-semibold text-slate-700">Reason / Compensation Context</label>
                <input
                  type="text"
                  value={formReason}
                  onChange={(e) => setFormReason(e.target.value)}
                  placeholder="e.g. Tour delay, vehicle breakdown, missed sunset stop"
                  required
                  className="px-3.5 py-2.5 border border-slate-300 rounded-lg text-[14px] text-slate-900 outline-none focus:border-indigo-600 font-inherit"
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label className="text-[13px] font-semibold text-slate-700">Validity Duration (Days)</label>
                <input
                  type="number"
                  min="7"
                  max="365"
                  value={formExpiryDays}
                  onChange={(e) => setFormExpiryDays(Number(e.target.value))}
                  required
                  className="px-3.5 py-2.5 border border-slate-300 rounded-lg text-[14px] text-slate-900 outline-none focus:border-indigo-600 font-inherit"
                />
              </div>

              <div className="flex justify-end gap-3 mt-3">
                <button
                  type="button"
                  onClick={() => setShowIssueModal(false)}
                  className="px-[18px] py-[10px] border border-slate-300 bg-white rounded-lg text-slate-700 text-[14px] font-semibold cursor-pointer hover:bg-slate-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-[18px] py-[10px] bg-green-600 hover:bg-green-700 text-white border-none rounded-lg text-[14px] font-semibold cursor-pointer transition-colors disabled:opacity-70"
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

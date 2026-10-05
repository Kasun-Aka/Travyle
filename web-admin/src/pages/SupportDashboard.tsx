import React, { useState, useEffect, useCallback } from 'react';
import type { SupportTicketItem, TicketPriority, TicketStatus } from '../types/support';
import { supportApi } from '../services/supportApi';
import { TicketDetailModal } from '../components/support/TicketDetailModal';
import AnalyticsStrip from '../components/support/AnalyticsStrip';
import { useNavigate } from 'react-router-dom';

interface SupportDashboardProps {
  onNavigateVouchers?: () => void;
  onNavigateReviews?: () => void;
  onLogout?: () => void;
}

export const SupportDashboard: React.FC<SupportDashboardProps> = ({ 
  onNavigateVouchers, 
  onNavigateReviews, 
  onLogout 
}) => {
  const navigate = useNavigate();
  const handleGoVouchers = onNavigateVouchers || (() => navigate('/support/vouchers'));
  const handleGoReviews = onNavigateReviews || (() => navigate('/support/reviews'));
  const handleGoLogout = onLogout || (() => navigate('/login'));
  const [tickets, setTickets] = useState<SupportTicketItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedTicket, setSelectedTicket] = useState<SupportTicketItem | null>(null);

  // Filters
  const [activePriority, setActivePriority] = useState<string>('ALL');
  const [activeStatus, setActiveStatus] = useState<string>('ALL');
  const [searchQuery, setSearchQuery] = useState('');

  const fetchTickets = useCallback(async () => {
    setLoading(true);
    try {
      const response = await supportApi.getTickets({
        priority: activePriority !== 'ALL' ? (activePriority as TicketPriority) : undefined,
        status: activeStatus !== 'ALL' ? (activeStatus as TicketStatus) : undefined,
        search: searchQuery || undefined,
        pageSize: 50,
      });
      const items = Array.isArray(response?.items) ? response.items : (Array.isArray(response) ? response : []);
      setTickets(items);
    } catch (err) {
      console.error('Failed to load tickets:', err);
      setTickets([]);
    } finally {
      setLoading(false);
    }
  }, [activePriority, activeStatus, searchQuery]);

  useEffect(() => {
    fetchTickets();
  }, [fetchTickets]);

  // Statistics calculation
  const safeTickets = Array.isArray(tickets) ? tickets : [];
  const totalCount = safeTickets.length;
  const highPriorityCount = safeTickets.filter((t) => t?.priority === 'High' || t?.priority === 'Critical').length;
  const pendingApprovalCount = safeTickets.filter((t) => t?.status === 'Pending_Admin_Voucher_Approval').length;
  const resolvedCount = safeTickets.filter((t) => t?.status === 'Resolved' || t?.status === 'Closed').length;

  const handleTicketUpdated = (updated: SupportTicketItem) => {
    setTickets((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
    setSelectedTicket(updated);
  };

  const handleDeleteTicket = async (ticketId: string) => {
    if (!window.confirm('Are you sure you want to delete this support ticket?')) return;
    try {
      await supportApi.deleteTicket(ticketId);
      setTickets((prev) => prev.filter((t) => t.id !== ticketId));
      if (selectedTicket?.id === ticketId) {
        setSelectedTicket(null);
      }
    } catch (err) {
      console.error('Failed to delete ticket:', err);
      alert('Error deleting ticket.');
    }
  };

  const getPriorityBadgeClass = (priority: TicketPriority) => {
    switch (priority) {
      case 'Critical': return 'bg-red-100 text-red-600';
      case 'High': return 'bg-orange-100 text-orange-700';
      case 'Medium': return 'bg-amber-100 text-amber-700';
      case 'Low': return 'bg-slate-100 text-slate-600';
      default: return 'bg-slate-100 text-slate-600';
    }
  };

  const getStatusBadge = (status: TicketStatus) => {
    const baseClass = "inline-flex items-center gap-[6px] px-[10px] py-[4px] rounded-full text-[12px] font-semibold";
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return <span className={`${baseClass} bg-amber-100 text-amber-800`}>⏳ Approval Needed</span>;
      case 'Pending_AI_Triage':
        return <span className={`${baseClass} bg-amber-100 text-amber-800`}>🤖 AI Triaging</span>;
      case 'In_Review':
        return <span className={`${baseClass} bg-blue-50 text-blue-800`}>🔍 In Review</span>;
      case 'Resolved':
        return <span className={`${baseClass} bg-green-100 text-green-700`}>✓ Resolved</span>;
      case 'Closed':
        return <span className={`${baseClass} bg-slate-200 text-slate-600`}>Closed</span>;
      case 'Rejected':
        return <span className={`${baseClass} bg-red-100 text-red-600`}>✕ Rejected</span>;
      default:
        return <span className={baseClass}>{status || 'Unknown'}</span>;
    }
  };

  return (
    <div className="flex flex-col min-h-full w-full bg-[#F8F9FB] font-sans p-6">
      {/* Feature Sub-Navigation Header */}
      <div className="flex gap-3 mb-5 border-b border-slate-200 pb-3">
        <button
          className="px-4 py-2 rounded-lg bg-indigo-600 text-white border-none font-semibold cursor-pointer"
        >
          🎫 Support Queue
        </button>
        <button
          onClick={handleGoVouchers}
          className="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 border-none font-semibold cursor-pointer"
        >
          🎁 Goodwill Vouchers
        </button>
        <button
          onClick={handleGoReviews}
          className="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 border-none font-semibold cursor-pointer"
        >
          ⭐ Customer Reviews
        </button>
      </div>

      {/* Header */}
      <div className="flex items-center justify-between mb-7">
        <div>
          <h1 className="m-0 mb-1 text-[26px] font-extrabold text-slate-900">Customer Support & Ticketing Queue</h1>
          <p className="m-0 text-[14px] text-slate-500">Monitor customer disputes, review AI triage insights, and sign off on goodwill vouchers.</p>
        </div>
        <div className="flex gap-3">
          <button className="flex items-center gap-2 px-5 py-2.5 rounded-lg bg-indigo-600 hover:bg-indigo-700 text-white border-none text-[14px] font-semibold cursor-pointer transition-colors" onClick={fetchTickets}>
            🔄 Refresh Queue
          </button>
          {onLogout && (
            <button className="px-[14px] py-[8px] rounded-md border border-indigo-200 bg-indigo-50 text-indigo-600 hover:bg-indigo-600 hover:text-white text-[13px] font-semibold cursor-pointer transition-colors" onClick={handleGoLogout}>
              Sign Out
            </button>
          )}
        </div>
      </div>

      {/* Feature 1: Analytics Stats Strip */}
      <AnalyticsStrip />

        {/* Stat Cards */}
        <div className="grid grid-cols-[repeat(auto-fit,minmax(220px,1fr))] gap-5 mb-7">
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Total Active Tickets</span>
            <span className="text-[28px] font-extrabold text-slate-900">{totalCount}</span>
            <span className="text-[12px] font-medium text-slate-500">In support database</span>
          </div>
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Urgent / High Severity</span>
            <span className="text-[28px] font-extrabold text-red-600">{highPriorityCount}</span>
            <span className="text-[12px] font-medium text-red-600">Requires prompt attention</span>
          </div>
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Pending Voucher Sign-off</span>
            <span className="text-[28px] font-extrabold text-amber-600">{pendingApprovalCount}</span>
            <span className="text-[12px] font-medium text-amber-600">AI $50 Voucher drafted</span>
          </div>
          <div className="flex flex-col gap-1.5 p-6 rounded-xl bg-white border border-slate-200 shadow-sm">
            <span className="text-[13px] font-semibold text-slate-500">Resolved / Satisfied</span>
            <span className="text-[28px] font-extrabold text-green-600">{resolvedCount}</span>
            <span className="text-[12px] font-medium text-green-600">Closed claims</span>
          </div>
        </div>

        {/* Controls / Filter Bar */}
        <div className="flex items-center justify-between gap-4 flex-wrap p-4 px-5 bg-white border border-slate-200 rounded-xl mb-7">
          <div className="flex items-center px-[14px] py-[8px] bg-[#F8F9FB] border border-slate-300 rounded-lg min-w-[280px]">
            <span className="mr-2 text-slate-400">🔍</span>
            <input
              className="w-full bg-transparent border-none outline-none text-[14px] text-slate-900"
              type="text"
              placeholder="Search by customer, title, issue..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>

          <div className="flex items-center gap-3">
            <div className="flex gap-1 p-1 bg-slate-100 rounded-lg">
              {['ALL', 'Critical', 'High', 'Medium', 'Low'].map((p) => (
                <button
                  key={p}
                  className={`px-3 py-1.5 rounded-md border-none text-[13px] font-semibold cursor-pointer transition-colors ${activePriority === p ? 'bg-white text-indigo-600 shadow-sm' : 'bg-transparent text-slate-500 hover:text-slate-700'}`}
                  onClick={() => setActivePriority(p)}
                >
                  {p}
                </button>
              ))}
            </div>

            <select
              className="px-[14px] py-[8px] border border-slate-300 rounded-lg text-[13px] font-medium text-slate-700 bg-white outline-none"
              value={activeStatus}
              onChange={(e) => setActiveStatus(e.target.value)}
            >
              <option value="ALL">All Statuses</option>
              <option value="Pending_Admin_Voucher_Approval">Pending Admin Approval</option>
              <option value="Pending_AI_Triage">Pending AI Triage</option>
              <option value="In_Review">In Review</option>
              <option value="Resolved">Resolved</option>
              <option value="Closed">Closed</option>
              <option value="Rejected">Rejected</option>
            </select>
          </div>
        </div>

        {/* Table Card */}
        <div className="bg-white border border-slate-200 rounded-xl shadow-sm overflow-hidden">
          <table className="w-full border-collapse text-left">
            <thead>
              <tr>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Ticket ID / Title</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Traveler</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Category</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Priority</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">AI Sentiment</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide">Status</th>
                <th className="px-5 py-3.5 bg-[#F8F9FB] border-b border-slate-200 text-[12px] font-bold text-slate-600 uppercase tracking-wide text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} className="text-center p-10 text-slate-500">
                    Loading support tickets...
                  </td>
                </tr>
              ) : tickets.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center p-10 text-slate-500">
                    No support tickets matching current filters.
                  </td>
                </tr>
              ) : (
                tickets.map((t) => (
                  <tr key={t.id} className="hover:bg-slate-50 border-b border-slate-100 last:border-0">
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                      <div className="font-bold text-slate-900 mb-1">{t.title}</div>
                      <div className="text-[12px] text-slate-500 max-w-[280px] overflow-hidden text-ellipsis whitespace-nowrap">{t.description}</div>
                    </td>
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                      <div className="font-semibold text-slate-900">{t.userName || 'Traveler'}</div>
                      <div className="text-[12px] text-slate-400">{t.userEmail}</div>
                    </td>
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                      <span className="text-[13px] bg-slate-100 px-2 py-1 rounded font-medium">
                        {t.category}
                      </span>
                    </td>
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                      <span className={`inline-block px-2.5 py-1 rounded-md text-[12px] font-bold ${getPriorityBadgeClass(t.priority)}`}>
                        {t.priority}
                      </span>
                    </td>
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700">
                      <div className="flex items-center gap-1.5">
                        <span className={`font-bold ${(t.sentimentScore ?? 0) < -0.3 ? 'text-red-600' : ((t.sentimentScore ?? 0) > 0.3 ? 'text-green-600' : 'text-slate-500')}`}>
                          {(t.sentimentScore ?? 0).toFixed(2)}
                        </span>
                        <span className="text-[11px] text-slate-400">
                          ({(t.severityTier || 'Tier_1_Low').replace('Tier_', 'T')})
                        </span>
                      </div>
                    </td>
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700">{getStatusBadge(t.status)}</td>
                    <td className="px-5 py-4 align-middle text-[14px] text-slate-700 text-right">
                      <div className="flex items-center justify-end gap-1.5">
                        <button 
                          className="px-[14px] py-[8px] rounded-md border border-indigo-200 bg-indigo-50 text-indigo-600 hover:bg-indigo-600 hover:text-white text-[13px] font-semibold cursor-pointer transition-colors"
                          onClick={() => setSelectedTicket(t)}
                        >
                          {t.status === 'Pending_Admin_Voucher_Approval' ? '🎁 Review & Sign' : 'Inspect Details'}
                        </button>
                        <button
                          className="bg-transparent border-none text-red-500 text-[13px] cursor-pointer p-1.5 hover:bg-red-50 rounded"
                          onClick={() => handleDeleteTicket(t.id)}
                          title="Delete Ticket"
                        >
                          🗑️
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

      {/* Ticket Detail Modal */}
      {selectedTicket && (
        <TicketDetailModal
          ticket={selectedTicket}
          onClose={() => setSelectedTicket(null)}
          onTicketUpdated={handleTicketUpdated}
        />
      )}
    </div>
  );
};;

import React, { useState, useEffect, useCallback } from 'react';
import type { SupportTicketItem, TicketPriority, TicketStatus } from '../types/support';
import { supportApi } from '../services/supportApi';
import { TicketDetailModal } from '../components/support/TicketDetailModal';
import './SupportDashboard.css';

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
      case 'Critical': return 'badge-critical';
      case 'High': return 'badge-high';
      case 'Medium': return 'badge-medium';
      case 'Low': return 'badge-low';
      default: return 'badge-low';
    }
  };

  const getStatusBadge = (status: TicketStatus) => {
    switch (status) {
      case 'Pending_Admin_Voucher_Approval':
        return <span className="status-badge status-pending-approval">⏳ Approval Needed</span>;
      case 'Pending_AI_Triage':
        return <span className="status-badge status-pending-approval">🤖 AI Triaging</span>;
      case 'In_Review':
        return <span className="status-badge status-in-review">🔍 In Review</span>;
      case 'Resolved':
        return <span className="status-badge status-resolved">✓ Resolved</span>;
      case 'Closed':
        return <span className="status-badge" style={{ background: '#E2E8F0', color: '#475569' }}>Closed</span>;
      case 'Rejected':
        return <span className="status-badge" style={{ background: '#FEE2E2', color: '#DC2626' }}>✕ Rejected</span>;
      default:
        return <span className="status-badge">{status || 'Unknown'}</span>;
    }
  };

  return (
    <div className="support-dashboard-layout" style={{ padding: 24 }}>
      {/* Feature Sub-Navigation Header */}
      <div style={{ display: 'flex', gap: 12, marginBottom: 20, borderBottom: '1px solid #E2E8F0', paddingBottom: 12 }}>
        <button
          style={{ padding: '8px 16px', borderRadius: 8, background: '#4F46E5', color: 'white', border: 'none', fontWeight: 600, cursor: 'pointer' }}
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
          onClick={handleGoReviews}
          style={{ padding: '8px 16px', borderRadius: 8, background: '#F1F5F9', color: '#475569', border: 'none', fontWeight: 600, cursor: 'pointer' }}
        >
          ⭐ Customer Reviews
        </button>
      </div>

      {/* Header */}
      <div className="main-header">
        <div className="header-title">
          <h1>Customer Support & Ticketing Queue</h1>
          <p>Monitor customer disputes, review AI triage insights, and sign off on goodwill vouchers.</p>
        </div>
        <div className="header-actions">
          <button className="btn-primary" onClick={fetchTickets}>
            🔄 Refresh Queue
          </button>
          {onLogout && (
            <button className="btn-review" onClick={handleGoLogout}>
              Sign Out
            </button>
          )}
        </div>
      </div>

        {/* Stat Cards */}
        <div className="stats-grid">
          <div className="stat-card">
            <span className="stat-label">Total Active Tickets</span>
            <span className="stat-value">{totalCount}</span>
            <span className="stat-subtext" style={{ color: '#64748B' }}>In support database</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Urgent / High Severity</span>
            <span className="stat-value stat-danger">{highPriorityCount}</span>
            <span className="stat-subtext stat-danger">Requires prompt attention</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Pending Voucher Sign-off</span>
            <span className="stat-value stat-warning">{pendingApprovalCount}</span>
            <span className="stat-subtext stat-warning">AI $50 Voucher drafted</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Resolved / Satisfied</span>
            <span className="stat-value stat-success">{resolvedCount}</span>
            <span className="stat-subtext stat-success">Closed claims</span>
          </div>
        </div>

        {/* Controls / Filter Bar */}
        <div className="controls-bar">
          <div className="search-box">
            <span style={{ marginRight: 8, color: '#94A3B8' }}>🔍</span>
            <input
              type="text"
              placeholder="Search by customer, title, issue..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
            />
          </div>

          <div className="filter-group">
            <div className="priority-tabs">
              {['ALL', 'Critical', 'High', 'Medium', 'Low'].map((p) => (
                <button
                  key={p}
                  className={`tab-btn ${activePriority === p ? 'active' : ''}`}
                  onClick={() => setActivePriority(p)}
                >
                  {p}
                </button>
              ))}
            </div>

            <select
              className="status-dropdown"
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
        <div className="table-card">
          <table className="ticket-table">
            <thead>
              <tr>
                <th>Ticket ID / Title</th>
                <th>Traveler</th>
                <th>Category</th>
                <th>Priority</th>
                <th>AI Sentiment</th>
                <th>Status</th>
                <th style={{ textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#64748B' }}>
                    Loading support tickets...
                  </td>
                </tr>
              ) : tickets.length === 0 ? (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 40, color: '#64748B' }}>
                    No support tickets matching current filters.
                  </td>
                </tr>
              ) : (
                tickets.map((t) => (
                  <tr key={t.id}>
                    <td>
                      <div className="ticket-row-title">{t.title}</div>
                      <div className="ticket-row-desc">{t.description}</div>
                    </td>
                    <td>
                      <div style={{ fontWeight: 600, color: '#0F172A' }}>{t.userName || 'Traveler'}</div>
                      <div style={{ fontSize: 12, color: '#94A3B8' }}>{t.userEmail}</div>
                    </td>
                    <td>
                      <span style={{ fontSize: 13, background: '#F1F5F9', padding: '4px 8px', borderRadius: 4, fontWeight: 500 }}>
                        {t.category}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${getPriorityBadgeClass(t.priority)}`}>
                        {t.priority}
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                        <span style={{ 
                          fontWeight: 700, 
                          color: (t.sentimentScore ?? 0) < -0.3 ? '#DC2626' : ((t.sentimentScore ?? 0) > 0.3 ? '#16A34A' : '#64748B') 
                        }}>
                          {(t.sentimentScore ?? 0).toFixed(2)}
                        </span>
                        <span style={{ fontSize: 11, color: '#94A3B8' }}>
                          ({(t.severityTier || 'Tier_1_Low').replace('Tier_', 'T')})
                        </span>
                      </div>
                    </td>
                    <td>{getStatusBadge(t.status)}</td>
                    <td style={{ textAlign: 'right' }}>
                      <div style={{ display: 'flex', gap: 6, justifyContent: 'flex-end', alignItems: 'center' }}>
                        <button 
                          className="btn-review"
                          onClick={() => setSelectedTicket(t)}
                        >
                          {t.status === 'Pending_Admin_Voucher_Approval' ? '🎁 Review & Sign' : 'Inspect Details'}
                        </button>
                        <button
                          style={{ background: 'none', border: 'none', color: '#EF4444', fontSize: 13, cursor: 'pointer', padding: '4px 6px' }}
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
};

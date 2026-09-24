import React, { useState } from 'react';
import type { SupportTicketItem, TicketStatus } from '../../types/support';
import { supportApi } from '../../services/supportApi';
import './TicketDetailModal.css';

interface TicketDetailModalProps {
  ticket: SupportTicketItem;
  onClose: () => void;
  onTicketUpdated: (updatedTicket: SupportTicketItem) => void;
}

export const TicketDetailModal: React.FC<TicketDetailModalProps> = ({
  ticket,
  onClose,
  onTicketUpdated,
}) => {
  const [currentTicket, setCurrentTicket] = useState<SupportTicketItem>(ticket);
  const [loadingAction, setLoadingAction] = useState(false);
  const [customAmount, setCustomAmount] = useState<number>(50);
  const [adminNotes, setAdminNotes] = useState('');
  const [statusSelection, setStatusSelection] = useState<TicketStatus>(ticket.status);

  // Check if there is a pending draft voucher
  const draftVoucher = currentTicket.vouchers?.find((v) => v.status === 'Draft');
  const activeVoucher = currentTicket.vouchers?.find((v) => v.status === 'Active');

  const handleApproveVoucher = async () => {
    if (!draftVoucher) return;
    setLoadingAction(true);
    try {
      await supportApi.approveVoucher(draftVoucher.id, {
        adjustedAmount: customAmount,
        notes: adminNotes || 'Approved by Customer Support Admin.',
        sendNotification: true,
      });
      // Refresh ticket details
      const refreshed = await supportApi.getTicketById(currentTicket.id);
      setCurrentTicket(refreshed);
      onTicketUpdated(refreshed);
    } catch (err) {
      console.error('Failed to approve voucher:', err);
      alert('Error approving voucher. Please check backend connection.');
    } finally {
      setLoadingAction(false);
    }
  };

  const handleAutoResolve = async () => {
    setLoadingAction(true);
    try {
      await supportApi.autoResolveClaim(currentTicket.id);
      const refreshed = await supportApi.getTicketById(currentTicket.id);
      setCurrentTicket(refreshed);
      onTicketUpdated(refreshed);
    } catch (err) {
      console.error('Failed to auto-resolve claim:', err);
      alert('Error running AI Auto-Triage.');
    } finally {
      setLoadingAction(false);
    }
  };

  const handleStatusChange = async (newStatus: TicketStatus) => {
    setStatusSelection(newStatus);
    setLoadingAction(true);
    try {
      const updated = await supportApi.updateTicketStatus(currentTicket.id, {
        status: newStatus,
        resolutionSummary: adminNotes || `Status manually changed to ${newStatus}.`,
      });
      setCurrentTicket(updated);
      onTicketUpdated(updated);
    } catch (err) {
      console.error('Failed to update status:', err);
    } finally {
      setLoadingAction(false);
    }
  };

  const getSentimentClass = (score: number) => {
    if (score <= -0.3) return 'sentiment-neg';
    if (score >= 0.3) return 'sentiment-pos';
    return 'sentiment-neu';
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="ticket-modal-container" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="modal-header">
          <div className="modal-header-left">
            <span className="modal-ticket-id">#{currentTicket.id.slice(0, 8)}</span>
            <h2 className="modal-header-title">{currentTicket.title}</h2>
          </div>
          <button className="modal-close-btn" onClick={onClose}>&times;</button>
        </div>

        {/* Modal Body */}
        <div className="modal-body">
          {/* Metadata Grid */}
          <div className="ticket-meta-grid">
            <div className="meta-item">
              <span className="meta-label">Customer / Traveler</span>
              <span className="meta-value">{currentTicket.userName || 'Traveler'}</span>
            </div>
            <div className="meta-item">
              <span className="meta-label">Category</span>
              <span className="meta-value">{currentTicket.category}</span>
            </div>
            <div className="meta-item">
              <span className="meta-label">Priority</span>
              <span className="meta-value">{currentTicket.priority}</span>
            </div>
            <div className="meta-item">
              <span className="meta-label">Current Status</span>
              <select 
                value={statusSelection} 
                onChange={(e) => handleStatusChange(e.target.value as TicketStatus)}
                style={{ fontWeight: 600, padding: '4px 8px', borderRadius: 6, border: '1px solid #CBD5E1' }}
              >
                <option value="Pending_AI_Triage">Pending AI Triage</option>
                <option value="Pending_Admin_Voucher_Approval">Pending Admin Approval</option>
                <option value="In_Review">In Review</option>
                <option value="Resolved">Resolved</option>
                <option value="Closed">Closed</option>
                <option value="Rejected">Rejected</option>
              </select>
            </div>
          </div>

          {/* Description & Attachment */}
          <div className="ticket-desc-section">
            <h3 className="section-heading">Description of Issue</h3>
            <p className="ticket-desc-text">{currentTicket.description}</p>
            {currentTicket.attachmentUrl && (
              <div className="attachment-preview">
                <span className="meta-label">Attached Photo Evidence</span>
                <img 
                  src={currentTicket.attachmentUrl} 
                  alt="Customer evidence" 
                  className="attachment-img" 
                  onClick={() => window.open(currentTicket.attachmentUrl, '_blank')}
                />
              </div>
            )}
          </div>

          {/* AI Sentiment & Triage Card */}
          <div className="ai-triage-card">
            <div className="ai-card-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                <span className="ai-badge">🤖 Support & Quality Agent</span>
                <span style={{ fontSize: 13, color: '#6D28D9', fontWeight: 600 }}>Automated Triage Analysis</span>
              </div>
              <button 
                className="btn-rerun-triage" 
                onClick={handleAutoResolve}
                disabled={loadingAction}
              >
                {loadingAction ? 'Processing...' : 'Re-run AI Analysis'}
              </button>
            </div>

            <div className="ai-grid">
              <div className="ai-grid-box">
                <div className="ai-box-label">Sentiment Score</div>
                <div className={`ai-box-value ${getSentimentClass(currentTicket.sentimentScore)}`}>
                  {currentTicket.sentimentScore.toFixed(2)} / 1.00
                </div>
              </div>
              <div className="ai-grid-box">
                <div className="ai-box-label">Severity Tier</div>
                <div className="ai-box-value">{currentTicket.severityTier || 'Tier_1_Low'}</div>
              </div>
              <div className="ai-grid-box">
                <div className="ai-box-label">AI Recommendation</div>
                <div className="ai-box-value" style={{ fontSize: 13 }}>
                  {draftVoucher ? 'Draft $50 Goodwill Voucher' : (activeVoucher ? 'Voucher Active' : 'Standard Resolution')}
                </div>
              </div>
            </div>

            {currentTicket.aiReasoning && (
              <p className="ai-reasoning-text">
                <strong>Analysis Log:</strong> {currentTicket.aiReasoning}
              </p>
            )}
          </div>

          {/* Voucher Sign-off Panel */}
          {draftVoucher ? (
            <div className="voucher-signoff-panel">
              <div className="voucher-panel-header">
                <h3 className="section-heading" style={{ color: '#15803D' }}>
                  🎁 Goodwill Voucher Sign-Off Panel
                </h3>
                <span style={{ fontSize: 12, fontWeight: 700, color: '#16A34A', background: '#DCFCE7', padding: '4px 10px', borderRadius: 20 }}>
                  AWAITING ADMIN APPROVAL
                </span>
              </div>

              <div className="voucher-details-box">
                <div>
                  <div style={{ fontSize: 12, color: '#64748B', marginBottom: 4 }}>DRAFTED VOUCHER CODE</div>
                  <span className="voucher-code-badge">{draftVoucher.code}</span>
                  <div style={{ fontSize: 12, color: '#64748B', marginTop: 6 }}>Reason: {draftVoucher.reason}</div>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div style={{ fontSize: 12, color: '#64748B', marginBottom: 4 }}>APPROVED VALUE</div>
                  <span className="voucher-amount-tag">${customAmount.toFixed(2)}</span>
                </div>
              </div>

              <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
                <input 
                  type="number" 
                  value={customAmount} 
                  onChange={(e) => setCustomAmount(Number(e.target.value))}
                  placeholder="Voucher Amount" 
                  style={{ width: 120, padding: '10px 12px', borderRadius: 8, border: '1px solid #CBD5E1' }}
                />
                <input 
                  type="text" 
                  value={adminNotes} 
                  onChange={(e) => setAdminNotes(e.target.value)}
                  placeholder="Optional Admin Approval Note" 
                  style={{ flex: 1, padding: '10px 12px', borderRadius: 8, border: '1px solid #CBD5E1' }}
                />
                <button 
                  className="btn-approve-voucher" 
                  onClick={handleApproveVoucher}
                  disabled={loadingAction}
                >
                  {loadingAction ? 'Activating...' : '✓ Approve & Issue Voucher'}
                </button>
              </div>
            </div>
          ) : activeVoucher ? (
            <div className="voucher-signoff-panel" style={{ background: '#F8FAFC', borderColor: '#E2E8F0' }}>
              <h3 className="section-heading" style={{ color: '#16A34A' }}>
                ✓ Active Goodwill Voucher Issued
              </h3>
              <div className="voucher-details-box">
                <div>
                  <span className="voucher-code-badge">{activeVoucher.code}</span>
                  <div style={{ fontSize: 12, color: '#64748B', marginTop: 4 }}>{activeVoucher.reason}</div>
                </div>
                <span className="voucher-amount-tag">${activeVoucher.amount.toFixed(2)}</span>
              </div>
            </div>
          ) : null}

          {/* Audit Trace Reviewer */}
          <div className="audit-trace-container">
            <h3 className="section-heading">Chronological Audit Trace ({currentTicket.auditLogs?.length || 0} events)</h3>
            <div className="timeline">
              {currentTicket.auditLogs && currentTicket.auditLogs.length > 0 ? (
                currentTicket.auditLogs.map((log) => (
                  <div key={log.id} className="timeline-item">
                    <div className={`timeline-dot ${log.actorRole.toLowerCase()}`} />
                    <div className="timeline-content">
                      <div className="timeline-header">
                        <div>
                          <span className="actor-pill">{log.actorRole}</span>
                          <span className="timeline-action">{log.action}</span>
                        </div>
                        <span className="timeline-time">{new Date(log.timestamp).toLocaleTimeString()}</span>
                      </div>
                      <p className="timeline-details">{log.details}</p>
                    </div>
                  </div>
                ))
              ) : (
                <p style={{ color: '#94A3B8', fontSize: 13 }}>No audit entries recorded yet.</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

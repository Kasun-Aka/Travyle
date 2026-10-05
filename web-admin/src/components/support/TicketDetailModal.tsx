import React, { useState, useEffect } from 'react';
import type { SupportTicketItem, TicketStatus, TicketPriority } from '../../types/support';
import { supportApi } from '../../services/supportApi';
import { supportApi as extraSupportApi, type UserSupportActivity } from '../../api/support';

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
  const [adminNotes, setAdminNotes] = useState<string>('');
  const [statusSelection, setStatusSelection] = useState<TicketStatus>(ticket.status);
  const [draftReply, setDraftReply] = useState<string>(ticket.draftReplyMessage || '');

  useEffect(() => {
    if (currentTicket.draftReplyMessage) {
      setDraftReply(currentTicket.draftReplyMessage);
    }
  }, [currentTicket.draftReplyMessage]);

  // Feature 2: User Support & Review Correlation State
  const [userActivity, setUserActivity] = useState<UserSupportActivity | null>(null);
  const [loadingActivity, setLoadingActivity] = useState<boolean>(false);

  const handleSendReply = async () => {
    if (!draftReply.trim()) {
      alert('Please enter a reply message before sending.');
      return;
    }
    setLoadingAction(true);
    try {
      const updated = await supportApi.sendCustomerReply(currentTicket.id, draftReply.trim());
      setCurrentTicket(updated);
      onTicketUpdated(updated);
      alert('Reply message sent successfully to traveler via email notification.');
    } catch (err) {
      console.error('Failed to send customer reply:', err);
      alert('Error sending reply message to traveler.');
    } finally {
      setLoadingAction(false);
    }
  };

  useEffect(() => {
    if (currentTicket.userId) {
      setLoadingActivity(true);
      extraSupportApi.getUserActivity(currentTicket.userId)
        .then((res) => setUserActivity(res.data))
        .catch((err) => console.warn('User activity fetch notice:', err))
        .finally(() => setLoadingActivity(false));
    }
  }, [currentTicket.userId]);

  // Edit ticket state
  const [isEditing, setIsEditing] = useState(false);
  const [editTitle, setEditTitle] = useState(ticket.title);
  const [editDesc, setEditDesc] = useState(ticket.description);
  const [editCategory, setEditCategory] = useState(ticket.category);
  const [editPriority, setEditPriority] = useState<TicketPriority>(ticket.priority);

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

  const handleRejectVoucher = async () => {
    if (!draftVoucher) return;
    setLoadingAction(true);
    try {
      await supportApi.rejectVoucher(draftVoucher.id, {
        reason: adminNotes || 'Declined by Customer Support Admin.',
      });
      const refreshed = await supportApi.getTicketById(currentTicket.id);
      setCurrentTicket(refreshed);
      onTicketUpdated(refreshed);
    } catch (err) {
      console.error('Failed to decline voucher:', err);
      alert('Error declining voucher. Please check backend connection.');
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
        resolutionSummary: `Status manually changed to ${newStatus}.`,
      });
      setCurrentTicket(updated);
      onTicketUpdated(updated);
    } catch (err) {
      console.error('Failed to update status:', err);
    } finally {
      setLoadingAction(false);
    }
  };

  const handleSaveEdit = async () => {
    setLoadingAction(true);
    try {
      const updated = await supportApi.updateTicket(currentTicket.id, {
        title: editTitle,
        description: editDesc,
        category: editCategory,
        priority: editPriority,
      });
      setCurrentTicket(updated);
      onTicketUpdated(updated);
      setIsEditing(false);
    } catch (err) {
      console.error('Failed to edit ticket:', err);
      alert('Failed to edit ticket. Note: Closed tickets cannot be edited.');
    } finally {
      setLoadingAction(false);
    }
  };

  const getSentimentClass = (score: number) => {
    if (score <= -0.3) return 'text-red-600';
    if (score >= 0.3) return 'text-green-600';
    return 'text-slate-600';
  };

  return (
    <div className="fixed inset-0 bg-slate-900/65 backdrop-blur-[4px] flex items-center justify-center z-[1000] p-5 animate-[fadeIn_0.2s_ease-out]" onClick={onClose}>
      <div className="bg-white rounded-2xl w-full max-w-[860px] max-h-[90vh] flex flex-col shadow-[0_20px_25px_-5px_rgba(0,0,0,0.1),0_10px_10px_-5px_rgba(0,0,0,0.04)] overflow-hidden" onClick={(e) => e.stopPropagation()}>
        {/* Modal Header */}
        <div className="px-7 py-6 border-b border-slate-200 flex items-center justify-between bg-[#F8F9FB]">
          <div className="flex items-center gap-3 flex-1">
            <span className="text-[13px] font-bold text-indigo-600 bg-indigo-50 px-2.5 py-1 rounded-md">#{currentTicket.id ? currentTicket.id.slice(0, 8) : ''}</span>
            {isEditing ? (
              <input
                type="text"
                value={editTitle}
                onChange={(e) => setEditTitle(e.target.value)}
                className="text-[18px] font-bold px-2 py-1 rounded-md border border-slate-300 w-[90%] outline-none focus:border-indigo-500"
              />
            ) : (
              <h2 className="m-0 text-[18px] font-bold text-slate-900">{currentTicket.title}</h2>
            )}
          </div>
          <div className="flex items-center gap-2">
            {currentTicket.status !== 'Closed' && (
              isEditing ? (
                <>
                  <button onClick={handleSaveEdit} disabled={loadingAction} className="px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white border-none rounded-md cursor-pointer font-semibold transition-colors">Save</button>
                  <button onClick={() => setIsEditing(false)} className="px-3 py-1.5 bg-slate-200 hover:bg-slate-300 text-slate-700 border-none rounded-md cursor-pointer font-semibold transition-colors">Cancel</button>
                </>
              ) : (
                <button onClick={() => setIsEditing(true)} className="px-3 py-1.5 bg-slate-100 hover:bg-slate-200 text-slate-700 border border-slate-300 rounded-md cursor-pointer font-semibold text-[13px] transition-colors">✏️ Edit Claim</button>
              )
            )}
            <button className="bg-transparent border-none text-[24px] leading-none text-slate-400 cursor-pointer px-2 py-1 rounded-md transition-all hover:bg-slate-200 hover:text-slate-900" onClick={onClose}>&times;</button>
          </div>
        </div>

        {/* Modal Body */}
        <div className="p-7 overflow-y-auto flex flex-col gap-6">
          {/* Metadata Grid */}
          <div className="grid grid-cols-[repeat(auto-fit,minmax(180px,1fr))] gap-4 bg-[#F8F9FB] rounded-xl px-5 py-4 border border-slate-200">
            <div className="flex flex-col gap-1">
              <span className="text-[12px] font-semibold text-slate-500 uppercase tracking-wide">Customer / Traveler</span>
              <span className="text-[14px] font-semibold text-slate-900">{currentTicket.userName || 'Traveler'}</span>
            </div>
            <div className="flex flex-col gap-1">
              <span className="text-[12px] font-semibold text-slate-500 uppercase tracking-wide">Category</span>
              {isEditing ? (
                <select value={editCategory} onChange={(e) => setEditCategory(e.target.value)} className="px-2 py-1 rounded-md border border-slate-300 bg-white outline-none">
                  <option value="TourDelay">TourDelay</option>
                  <option value="TourQuality">TourQuality</option>
                  <option value="Safety">Safety</option>
                  <option value="Billing">Billing</option>
                  <option value="GuideConduct">GuideConduct</option>
                  <option value="General">General</option>
                </select>
              ) : (
                <span className="text-[14px] font-semibold text-slate-900">{currentTicket.category}</span>
              )}
            </div>
            <div className="flex flex-col gap-1">
              <span className="text-[12px] font-semibold text-slate-500 uppercase tracking-wide">Priority</span>
              {isEditing ? (
                <select value={editPriority} onChange={(e) => setEditPriority(e.target.value as TicketPriority)} className="px-2 py-1 rounded-md border border-slate-300 bg-white outline-none">
                  <option value="Critical">Critical</option>
                  <option value="High">High</option>
                  <option value="Medium">Medium</option>
                  <option value="Low">Low</option>
                </select>
              ) : (
                <span className="text-[14px] font-semibold text-slate-900">{currentTicket.priority}</span>
              )}
            </div>
            <div className="flex flex-col gap-1">
              <span className="text-[12px] font-semibold text-slate-500 uppercase tracking-wide">Current Status</span>
              <select 
                value={statusSelection} 
                onChange={(e) => handleStatusChange(e.target.value as TicketStatus)}
                className="font-semibold px-2 py-1 rounded-md border border-slate-300 bg-white outline-none"
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
          <div className="flex flex-col gap-2">
            <h3 className="text-[15px] font-bold text-slate-800 flex items-center gap-2 m-0">Description of Issue</h3>
            {isEditing ? (
              <textarea
                value={editDesc}
                onChange={(e) => setEditDesc(e.target.value)}
                rows={4}
                className="w-full p-2.5 rounded-lg border border-slate-300 text-[14px] outline-none focus:border-indigo-500"
              />
            ) : (
              <p className="text-[14px] leading-relaxed text-slate-700 bg-white border border-slate-200 rounded-lg p-4 m-0">{currentTicket.description}</p>
            )}
            {currentTicket.attachmentUrl && (
              <div className="mt-2.5 flex flex-col gap-1.5">
                <span className="text-[12px] font-semibold text-slate-500 uppercase tracking-wide">Attached Photo Evidence</span>
                <img 
                  src={currentTicket.attachmentUrl} 
                  alt="Customer evidence" 
                  className="max-w-[240px] h-[140px] object-cover rounded-lg border border-slate-300 cursor-pointer transition-transform hover:scale-[1.02]" 
                  onClick={() => window.open(currentTicket.attachmentUrl, '_blank')}
                />
              </div>
            )}
          </div>

          {/* AI Sentiment & Triage Card */}
          <div className="bg-gradient-to-br from-violet-50 to-violet-100 border border-violet-200 rounded-xl p-5 flex flex-col gap-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <span className="inline-flex items-center gap-1.5 bg-violet-600 text-white text-[12px] font-bold px-2.5 py-1 rounded-full uppercase tracking-wide">🤖 Support & Quality Agent</span>
                <span className="text-[13px] text-violet-700 font-semibold">Automated Triage Analysis</span>
              </div>
              <button 
                className="bg-indigo-600 hover:bg-indigo-700 text-white border-none px-4 py-2 rounded-lg text-[13px] font-semibold cursor-pointer transition-colors disabled:opacity-70" 
                onClick={handleAutoResolve}
                disabled={loadingAction}
              >
                {loadingAction ? 'Processing...' : 'Re-run AI Analysis'}
              </button>
            </div>

            <div className="grid grid-cols-[repeat(auto-fit,minmax(200px,1fr))] gap-3">
              <div className="bg-white/80 p-3 px-4 rounded-lg border border-violet-200/40">
                <div className="text-[12px] text-violet-700 font-semibold mb-1">Sentiment Score</div>
                <div className={`text-[15px] font-bold ${getSentimentClass(currentTicket.sentimentScore ?? 0)}`}>
                  {(currentTicket.sentimentScore ?? 0).toFixed(2)} / 1.00
                </div>
              </div>
              <div className="bg-white/80 p-3 px-4 rounded-lg border border-violet-200/40">
                <div className="text-[12px] text-violet-700 font-semibold mb-1">Severity Tier</div>
                <div className="text-[15px] font-bold text-slate-900">{currentTicket.severityTier || 'Tier_1_Low'}</div>
              </div>
              <div className="bg-white/80 p-3 px-4 rounded-lg border border-violet-200/40">
                <div className="text-[12px] text-violet-700 font-semibold mb-1">AI Recommendation</div>
                <div className="text-[13px] font-bold text-slate-900">
                  {draftVoucher ? 'Draft $50 Goodwill Voucher' : (activeVoucher ? 'Voucher Active' : 'Standard Resolution')}
                </div>
              </div>
            </div>

            {currentTicket.aiReasoning && (
              <p className="text-[13px] text-violet-900 bg-white/60 p-3 rounded-lg border-l-4 border-violet-600 m-0 leading-relaxed">
                <strong>Analysis Log:</strong> {currentTicket.aiReasoning}
              </p>
            )}
          </div>

          {/* AI-Drafted Customer Response Panel */}
          <div className="bg-indigo-50/70 border border-indigo-200 rounded-xl p-5 flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="text-[16px]">✉️</span>
                <h3 className="text-[15px] font-bold text-indigo-900 m-0">
                  AI-Drafted Customer Response
                </h3>
              </div>
              <span className="text-[11px] font-semibold text-indigo-700 bg-indigo-100 px-2.5 py-0.5 rounded-full uppercase">
                Review & Edit Before Sending
              </span>
            </div>
            <p className="text-[12px] text-indigo-700 m-0 leading-relaxed">
              Review or customize the empathetic AI-drafted reply message below. Clicking <strong>Send Reply</strong> dispatches the email notification directly to the traveler.
            </p>
            <textarea
              value={draftReply}
              onChange={(e) => setDraftReply(e.target.value)}
              rows={4}
              placeholder="AI response draft will appear here..."
              className="w-full p-3 rounded-lg border border-indigo-200 text-[14px] leading-relaxed text-slate-800 bg-white outline-none focus:border-indigo-500 shadow-inner"
            />
            <div className="flex justify-end">
              <button
                onClick={handleSendReply}
                disabled={loadingAction || !draftReply.trim()}
                className="inline-flex items-center gap-2 bg-indigo-600 hover:bg-indigo-700 text-white border-none px-5 py-2.5 rounded-lg text-[13px] font-bold cursor-pointer transition-all disabled:opacity-60 disabled:cursor-not-allowed shadow-sm"
              >
                {loadingAction ? 'Sending...' : '✉️ Send Reply to Traveler'}
              </button>
            </div>
          </div>

          {/* Voucher Sign-off Panel */}
          {draftVoucher ? (
            <div className="bg-green-50 border border-green-200 rounded-xl p-5 flex flex-col gap-4">
              <div className="flex items-center justify-between">
                <h3 className="text-[15px] font-bold text-green-700 m-0">
                  🎁 Goodwill Voucher Sign-Off Panel
                </h3>
                <span className="text-[12px] font-bold text-green-600 bg-green-100 px-2.5 py-1 rounded-full uppercase">
                  AWAITING ADMIN APPROVAL
                </span>
              </div>

              <div className="bg-white border border-green-300 rounded-lg p-4 flex justify-between items-center">
                <div>
                  <div className="text-[12px] text-slate-500 mb-1">DRAFTED VOUCHER CODE</div>
                  <span className="font-mono text-[18px] font-bold text-green-700 bg-green-100 px-3 py-1.5 rounded-md tracking-wide">{draftVoucher.code}</span>
                  <div className="text-[12px] text-slate-500 mt-1.5">Reason: {draftVoucher.reason}</div>
                </div>
                <div className="text-right">
                  <div className="text-[12px] text-slate-500 mb-1">APPROVED VALUE</div>
                  <span className="text-[22px] font-extrabold text-green-700">${customAmount.toFixed(2)}</span>
                </div>
              </div>

              <div className="flex gap-3 items-center">
                <input 
                  type="number" 
                  value={customAmount} 
                  onChange={(e) => setCustomAmount(Number(e.target.value))}
                  placeholder="Voucher Amount" 
                  className="w-[120px] px-3 py-2.5 rounded-lg border border-slate-300 outline-none focus:border-green-500"
                />
                <input 
                  type="text" 
                  value={adminNotes} 
                  onChange={(e) => setAdminNotes(e.target.value)}
                  placeholder="Optional Admin Approval Note" 
                  className="flex-1 px-3 py-2.5 rounded-lg border border-slate-300 outline-none focus:border-green-500"
                />
                <button 
                  className="inline-flex items-center gap-2 bg-green-600 hover:bg-green-700 text-white border-none px-6 py-3 rounded-lg text-[14px] font-bold cursor-pointer transition-all disabled:opacity-60 disabled:cursor-not-allowed shadow-sm hover:shadow-md" 
                  onClick={handleApproveVoucher}
                  disabled={loadingAction}
                >
                  {loadingAction ? 'Processing...' : '✓ Approve & Issue'}
                </button>
                <button 
                  className="px-4 py-2.5 bg-red-100 hover:bg-red-200 text-red-600 border border-red-300 rounded-lg cursor-pointer font-semibold transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
                  onClick={handleRejectVoucher}
                  disabled={loadingAction}
                >
                  ✕ Decline
                </button>
              </div>
            </div>
          ) : activeVoucher ? (
            <div className="bg-[#F8FAFC] border border-[#E2E8F0] rounded-xl p-5 flex flex-col gap-4">
              <h3 className="text-[15px] font-bold text-green-600 m-0">
                ✓ Active Goodwill Voucher Issued
              </h3>
              <div className="bg-white border border-green-300 rounded-lg p-4 flex justify-between items-center">
                <div>
                  <span className="font-mono text-[18px] font-bold text-green-700 bg-green-100 px-3 py-1.5 rounded-md tracking-wide">{activeVoucher.code}</span>
                  <div className="text-[12px] text-slate-500 mt-1">{activeVoucher.reason}</div>
                </div>
                <span className="text-[22px] font-extrabold text-green-700">${activeVoucher.amount.toFixed(2)}</span>
              </div>
            </div>
          ) : null}

          {/* Feature 2: Traveler Support & Review Correlation View */}
          <div className="bg-slate-50 border border-slate-200 rounded-xl p-5 flex flex-col gap-3">
            <div className="flex items-center justify-between">
              <h3 className="text-[15px] font-bold text-slate-900 m-0 flex items-center gap-2">
                👤 Traveler Activity & Review Correlation
              </h3>
              <span className="text-[12px] font-semibold text-slate-500 bg-slate-200/60 px-2.5 py-1 rounded-full">Joined by User ID</span>
            </div>

            {loadingActivity ? (
              <div className="text-[13px] text-slate-400 py-2">Loading traveler history & reviews...</div>
            ) : userActivity ? (
              <div className="flex flex-col gap-3">
                {/* Past Reviews */}
                <div>
                  <h4 className="text-[12px] font-bold text-slate-600 uppercase tracking-wide mb-2 m-0">
                    Traveler Reviews Submitted ({userActivity.reviews?.length || 0})
                  </h4>
                  {userActivity.reviews && userActivity.reviews.length > 0 ? (
                    <div className="flex flex-col gap-2">
                      {userActivity.reviews.map((r) => (
                        <div key={r.id} className="bg-white border border-slate-200 p-3 rounded-lg flex flex-col gap-1">
                          <div className="flex items-center justify-between">
                            <span className={`text-[12px] font-bold px-2 py-0.5 rounded ${r.rating <= 2 ? 'bg-red-100 text-red-700 border border-red-200' : r.rating >= 4 ? 'bg-emerald-100 text-emerald-700 border border-emerald-200' : 'bg-amber-100 text-amber-700 border border-amber-200'}`}>
                              {'⭐'.repeat(r.rating)} ({r.rating}/5 Stars)
                            </span>
                            <span className="text-[11px] text-slate-400">{new Date(r.createdAt).toLocaleDateString()}</span>
                          </div>
                          <p className="text-[13px] text-slate-700 m-0 leading-relaxed font-medium">"{r.comment}"</p>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="text-[13px] text-slate-400 italic m-0">No reviews submitted yet by this traveler.</p>
                  )}
                </div>

                {/* Ticket History */}
                <div>
                  <h4 className="text-[12px] font-bold text-slate-600 uppercase tracking-wide mb-2 m-0">
                    Associated Support Tickets ({userActivity.tickets?.length || 0})
                  </h4>
                  <div className="flex flex-wrap gap-2">
                    {userActivity.tickets?.map((t) => (
                      <div key={t.id} className={`text-[12px] px-2.5 py-1.5 rounded-md border ${t.id === currentTicket.id ? 'bg-indigo-50 border-indigo-300 font-bold text-indigo-700' : 'bg-white border-slate-200 text-slate-600'}`}>
                        #{t.title} ({t.status})
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-[13px] text-slate-400 m-0">Traveler history unavailable.</p>
            )}
          </div>

          {/* Audit Trace Reviewer */}
          <div className="flex flex-col gap-3">
            <h3 className="text-[15px] font-bold text-slate-800 m-0">Chronological Audit Trace ({currentTicket.auditLogs?.length || 0} events)</h3>
            <div className="relative border-l-2 border-slate-200 ml-2.5 pl-5 flex flex-col gap-4">
              {currentTicket.auditLogs && currentTicket.auditLogs.length > 0 ? (
                currentTicket.auditLogs.map((log) => (
                  <div key={log.id} className="relative">
                    <div className={`absolute -left-[27px] top-1 w-3 h-3 rounded-full border-2 border-white shadow-[0_0_0_2px_#E2E8F0] ${log.actorRole.toLowerCase() === 'ai' ? 'bg-violet-600' : log.actorRole.toLowerCase() === 'admin' ? 'bg-green-600' : log.actorRole.toLowerCase() === 'traveler' ? 'bg-sky-600' : 'bg-indigo-600'}`} />
                    <div className="bg-[#F8F9FB] border border-slate-200 rounded-lg px-4 py-3">
                      <div className="flex items-center justify-between mb-1.5">
                        <div className="flex items-center">
                          <span className="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-slate-200 text-slate-700 mr-1.5">{log.actorRole}</span>
                          <span className="text-[13px] font-bold text-slate-900">{log.action}</span>
                        </div>
                        <span className="text-[11px] text-slate-400">{new Date(log.timestamp).toLocaleTimeString()}</span>
                      </div>
                      <p className="text-[13px] text-slate-600 m-0 leading-relaxed">{log.details}</p>
                    </div>
                  </div>
                ))
              ) : (
                <p className="text-slate-400 text-[13px] m-0">No audit entries recorded yet.</p>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

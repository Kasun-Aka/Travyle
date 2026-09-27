export type TicketPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export type TicketStatus = 
  | 'Pending_AI_Triage'
  | 'Pending_Admin_Voucher_Approval'
  | 'In_Review'
  | 'Resolved'
  | 'Closed'
  | 'Rejected';

export type VoucherStatus = 'Draft' | 'Active' | 'Redeemed' | 'Expired' | 'Revoked';

export interface AuditLogItem {
  id: string;
  supportTicketId?: string;
  action: string;
  actorRole: string;
  actorId?: string;
  details: string;
  metadataJson?: string;
  timestamp: string;
}

export interface VoucherItem {
  id: string;
  code: string;
  userId: string;
  userName?: string;
  supportTicketId?: string;
  amount: number;
  reason: string;
  status: VoucherStatus;
  approvedByAdminId?: string;
  issuedAt?: string;
  expiresAt?: string;
  redeemedAt?: string;
  createdAt: string;
}

export interface SupportTicketItem {
  id: string;
  userId: string;
  userName?: string;
  userEmail?: string;
  bookingId?: string;
  tourId?: string;
  title: string;
  description: string;
  category: string;
  priority: TicketPriority;
  status: TicketStatus;
  attachmentUrl?: string;
  sentimentScore: number;
  severityTier: string;
  aiReasoning?: string;
  resolutionSummary?: string;
  createdAt: string;
  updatedAt: string;
  auditLogs: AuditLogItem[];
  vouchers: VoucherItem[];
}

export interface PaginatedTicketsResponse {
  items: SupportTicketItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CustomerReviewItem {
  id: string;
  userId: string;
  userName?: string;
  tourId: string;
  rating: number;
  comment: string;
  isVerified: boolean;
  createdAt: string;
}

export interface AutoResolveResult {
  ticketId: string;
  sentimentScore: number;
  severityTier: string;
  sentimentSummary: string;
  aiReasoning: string;
  recommendedAction: string;
  voucherDrafted: boolean;
  voucherAmount?: number;
  voucherCode?: string;
  newStatus: TicketStatus;
  generatedAuditLogs: AuditLogItem[];
}

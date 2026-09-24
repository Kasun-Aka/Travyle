import axios from 'axios';
import type { 
  SupportTicketItem, 
  PaginatedTicketsResponse, 
  TicketPriority, 
  TicketStatus, 
  AutoResolveResult, 
  VoucherItem, 
  CustomerReviewItem 
} from '../types/support';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const supportApi = {
  // Support Tickets
  getTickets: async (params?: {
    page?: number;
    pageSize?: number;
    priority?: TicketPriority;
    status?: TicketStatus;
    search?: string;
  }): Promise<PaginatedTicketsResponse> => {
    const response = await api.get<PaginatedTicketsResponse>('/support/tickets', { params });
    return response.data;
  },

  getTicketById: async (id: string): Promise<SupportTicketItem> => {
    const response = await api.get<SupportTicketItem>(`/support/tickets/${id}`);
    return response.data;
  },

  createTicket: async (data: {
    title: string;
    description: string;
    category?: string;
    priority?: TicketPriority;
    bookingId?: string;
    tourId?: string;
    attachmentUrl?: string;
    userId?: string;
  }): Promise<SupportTicketItem> => {
    const response = await api.post<SupportTicketItem>('/support/tickets', data);
    return response.data;
  },

  updateTicketStatus: async (
    id: string, 
    data: { status: TicketStatus; resolutionSummary?: string; adminId?: string }
  ): Promise<SupportTicketItem> => {
    const response = await api.put<SupportTicketItem>(`/support/tickets/${id}/status`, data);
    return response.data;
  },

  autoResolveClaim: async (id: string): Promise<AutoResolveResult> => {
    const response = await api.post<AutoResolveResult>(`/support/tickets/${id}/auto-resolve-claim`);
    return response.data;
  },

  // Goodwill Vouchers
  issueVoucher: async (data: {
    userId: string;
    supportTicketId?: string;
    amount: number;
    reason: string;
    expiryDays?: number;
  }): Promise<VoucherItem> => {
    const response = await api.post<VoucherItem>('/support/vouchers', data);
    return response.data;
  },

  approveVoucher: async (
    voucherId: string, 
    data: { adjustedAmount?: number; notes?: string; sendNotification?: boolean; adminId?: string }
  ): Promise<VoucherItem> => {
    const response = await api.put<VoucherItem>(`/support/vouchers/${voucherId}/approve`, data);
    return response.data;
  },

  getAllVouchers: async (status?: string): Promise<VoucherItem[]> => {
    const response = await api.get<VoucherItem[]>('/support/vouchers', { params: { status } });
    return response.data;
  },

  getUserVouchers: async (userId: string): Promise<VoucherItem[]> => {
    const response = await api.get<VoucherItem[]>(`/support/vouchers/user/${userId}`);
    return response.data;
  },

  // Customer Reviews
  getReviewsByTour: async (tourId: string): Promise<CustomerReviewItem[]> => {
    const response = await api.get<CustomerReviewItem[]>(`/support/reviews/${tourId}`);
    return response.data;
  },

  createReview: async (data: {
    tourId: string;
    rating: number;
    comment: string;
    userId?: string;
  }): Promise<CustomerReviewItem> => {
    const response = await api.post<CustomerReviewItem>('/support/reviews', data);
    return response.data;
  },
};

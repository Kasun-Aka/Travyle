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

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? import.meta.env.VITE_API_URL ?? 'http://localhost:5085/api';

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
    const response = await api.get('/support/tickets', { params });
    const data = response.data as any;
    if (Array.isArray(data)) {
      return { items: data, totalCount: data.length, page: 1, pageSize: 50, totalPages: 1 };
    }
    return {
      items: Array.isArray(data?.items) ? data.items : (Array.isArray(data?.value) ? data.value : []),
      totalCount: data?.totalCount ?? 0,
      page: data?.page ?? 1,
      pageSize: data?.pageSize ?? 50,
      totalPages: data?.totalPages ?? 1,
    };
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

  updateTicket: async (
    id: string,
    data: { title: string; description: string; category?: string; priority?: TicketPriority; attachmentUrl?: string }
  ): Promise<SupportTicketItem> => {
    const response = await api.put<SupportTicketItem>(`/support/tickets/${id}`, data);
    return response.data;
  },

  updateTicketStatus: async (
    id: string, 
    data: { status: TicketStatus; resolutionSummary?: string; adminId?: string }
  ): Promise<SupportTicketItem> => {
    const response = await api.put<SupportTicketItem>(`/support/tickets/${id}/status`, data);
    return response.data;
  },

  deleteTicket: async (id: string): Promise<void> => {
    await api.delete(`/support/tickets/${id}`);
  },

  autoResolveClaim: async (id: string): Promise<AutoResolveResult> => {
    const response = await api.post<AutoResolveResult>(`/support/tickets/${id}/auto-resolve-claim`);
    return response.data;
  },

  // Goodwill Vouchers
  getVoucherById: async (id: string): Promise<VoucherItem> => {
    const response = await api.get<VoucherItem>(`/support/vouchers/${id}`);
    return response.data;
  },

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

  rejectVoucher: async (
    voucherId: string, 
    data: { reason?: string; adminId?: string }
  ): Promise<VoucherItem> => {
    const response = await api.put<VoucherItem>(`/support/vouchers/${voucherId}/reject`, data);
    return response.data;
  },

  deleteVoucher: async (id: string): Promise<void> => {
    await api.delete(`/support/vouchers/${id}`);
  },

  getAllVouchers: async (status?: string): Promise<VoucherItem[]> => {
    const response = await api.get('/support/vouchers', { params: { status } });
    const data = response.data as any;
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.value)) return data.value;
    if (Array.isArray(data?.items)) return data.items;
    return [];
  },

  getUserVouchers: async (userId: string): Promise<VoucherItem[]> => {
    const response = await api.get(`/support/vouchers/user/${userId}`);
    const data = response.data as any;
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.value)) return data.value;
    return [];
  },

  // Customer Reviews
  getReviewsByTour: async (tourId?: string): Promise<CustomerReviewItem[]> => {
    const endpoint = tourId ? `/support/reviews/${tourId}` : '/support/reviews';
    const response = await api.get(endpoint);
    const data = response.data as any;
    if (Array.isArray(data)) return data;
    if (Array.isArray(data?.value)) return data.value;
    if (Array.isArray(data?.items)) return data.items;
    return [];
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

  toggleReviewVerification: async (id: string, isVerified: boolean): Promise<CustomerReviewItem> => {
    const response = await api.put<CustomerReviewItem>(`/support/reviews/${id}/verify`, null, {
      params: { isVerified },
    });
    return response.data;
  },

  deleteReview: async (id: string): Promise<void> => {
    await api.delete(`/support/reviews/${id}`);
  },
};

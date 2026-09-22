import api from './client';

export interface Destination {
  id: string;
  name: string;
  region: string;
  description: string;
  tags: string[];
  latitude: number;
  longitude: number;
  imageUrl: string;
  averageRating: number;
}

export interface DestinationListResponse {
  totalCount: number;
  page: number;
  pageSize: number;
  items: Destination[];
}

export interface CreateDestinationPayload {
  name: string;
  region: string;
  description: string;
  tags: string[];
  imageUrl: string;
  latitude?: number;
  longitude?: number;
}

export const destinationsApi = {
  list: (params?: { search?: string; region?: string; tags?: string; page?: number; pageSize?: number }) =>
    api.get<DestinationListResponse>('/destinations', { params }),

  getById: (id: string) =>
    api.get<Destination>(`/destinations/${id}`),

  create: (payload: CreateDestinationPayload) =>
    api.post<Destination>('/destinations', payload),

  update: (id: string, payload: CreateDestinationPayload) =>
    api.put<Destination>(`/destinations/${id}`, payload),

  delete: (id: string) =>
    api.delete(`/destinations/${id}`),
};

import api from './client';

export interface SyncRequest {
  firebaseUid: string;
  email: string;
  fullName: string;
  role: string;
}

export interface User {
  id: string;
  firebaseUid: string;
  email: string;
  fullName: string;
  role: string;
  createdAt: string;
}

export const authApi = {
  sync: (data: SyncRequest) => api.post<User>('/auth/sync', data),
  updateUser: (data: { email: string; fullName: string }) => api.put<User>('/auth/user', data),
};

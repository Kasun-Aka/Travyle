// Central Axios instance pointing at the ASP.NET Core backend
import axios from 'axios';
import { auth } from '../lib/firebase';

const baseURL = import.meta.env.VITE_API_URL || import.meta.env.VITE_API_BASE_URL || 'http://localhost:5085/api';

const api = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(async (config) => {
  const token = await auth?.currentUser?.getIdToken();
  if (token) config.headers.set('Authorization', `Bearer ${token}`);
  return config;
});

export default api;

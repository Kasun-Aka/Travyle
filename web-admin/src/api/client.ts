// Central Axios instance pointing at the ASP.NET Core backend
import axios from 'axios';
import { auth } from '../lib/firebase';

const api = axios.create({
  baseURL: 'http://localhost:5085/api',
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

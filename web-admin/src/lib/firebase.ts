import { initializeApp } from 'firebase/app';
import { getAuth, type Auth } from 'firebase/auth';

const sanitize = (val: string | undefined) => val?.replace(/^["']|["']$/g, '').trim();

const firebaseConfig = {
  apiKey: sanitize(import.meta.env.VITE_FIREBASE_API_KEY),
  authDomain: sanitize(import.meta.env.VITE_FIREBASE_AUTH_DOMAIN),
  projectId: sanitize(import.meta.env.VITE_FIREBASE_PROJECT_ID),
  appId: sanitize(import.meta.env.VITE_FIREBASE_APP_ID),
};

export const firebaseConfigured = Object.values(firebaseConfig).every(
  (value) => typeof value === 'string' && value.length > 0,
);

export const auth: Auth | null = firebaseConfigured
  ? getAuth(initializeApp(firebaseConfig))
  : null;

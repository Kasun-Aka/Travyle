import { createContext, useContext, useEffect, useState } from 'react';
import { type User as FirebaseUser, onAuthStateChanged, signOut } from 'firebase/auth';
import { auth } from '../lib/firebase';
import { authApi, type User } from '../api/auth';

interface AuthContextType {
  currentUser: FirebaseUser | null;
  dbUser: User | null;
  loading: boolean;
  logout: () => Promise<void>;
  refreshUser: () => Promise<User | null>;
}

const AuthContext = createContext<AuthContextType>({
  currentUser: null,
  dbUser: null,
  loading: true,
  logout: async () => { },
  refreshUser: async () => null,
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [currentUser, setCurrentUser] = useState<FirebaseUser | null>(null);
  const [dbUser, setDbUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  const syncUserWithBackend = async (user: FirebaseUser): Promise<User> => {
    try {
      const res = await authApi.sync({
        firebaseUid: user.uid,
        email: user.email || '',
        fullName: user.displayName || user.email?.split('@')[0] || 'Admin',
        role: dbUser.role || 'Admin'
      });
      return res.data;
    } catch (error) {
      console.warn("Could not sync user with backend DB, using fallback admin profile:", error);
      return {
        id: user.uid,
        firebaseUid: user.uid,
        email: user.email || '',
        fullName: user.displayName || user.email?.split('@')[0] || 'Admin',
        role: 'Admin',
        createdAt: new Date().toISOString(),
      };
    }
  };

  useEffect(() => {
    if (!auth) {
      setLoading(false);
      return;
    }

    const unsubscribe = onAuthStateChanged(auth, async (user) => {
      setLoading(true);
      if (user && user.email) {
        try {
          const synced = await syncUserWithBackend(user);
          // Batch updates together so components receive both at the same time
          setCurrentUser(user);
          setDbUser(synced);
        } catch (err) {
          console.error("Auth initialization error:", err);
          setCurrentUser(user);
          setDbUser(null);
        }
      } else {
        setCurrentUser(null);
        setDbUser(null);
      }
      setLoading(false);
    });

    return unsubscribe;
  }, []);

  const logout = async () => {
    if (auth) {
      await signOut(auth);
    }
    setCurrentUser(null);
    setDbUser(null);
  };

  const refreshUser = async (): Promise<User | null> => {
    if (auth?.currentUser) {
      const synced = await syncUserWithBackend(auth.currentUser);
      setDbUser(synced);
      return synced;
    }
    return null;
  };

  return (
    <AuthContext.Provider value={{ currentUser, dbUser, loading, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  );
};
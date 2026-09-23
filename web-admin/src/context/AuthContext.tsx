import { createContext, useContext, useEffect, useState } from 'react';
import { User as FirebaseUser, onAuthStateChanged, signOut } from 'firebase/auth';
import { auth } from '../lib/firebase';
import { authApi, User } from '../api/auth';

interface AuthContextType {
  currentUser: FirebaseUser | null;
  dbUser: User | null;
  loading: boolean;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType>({
  currentUser: null,
  dbUser: null,
  loading: true,
  logout: async () => {},
});

export const useAuth = () => useContext(AuthContext);

export const AuthProvider = ({ children }: { children: React.ReactNode }) => {
  const [currentUser, setCurrentUser] = useState<FirebaseUser | null>(null);
  const [dbUser, setDbUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, async (user) => {
      setCurrentUser(user);
      if (user && user.email) {
        try {
          const res = await authApi.sync({
            firebaseUid: user.uid,
            email: user.email,
            fullName: user.displayName || user.email.split('@')[0],
            role: 'Admin', // Web app is Admin
          });
          setDbUser(res.data);
        } catch (error) {
          console.error("Failed to sync user with DB", error);
        }
      } else {
        setDbUser(null);
      }
      setLoading(false);
    });

    return unsubscribe;
  }, []);

  const logout = async () => {
    await signOut(auth);
  };

  return (
    <AuthContext.Provider value={{ currentUser, dbUser, loading, logout }}>
      {!loading && children}
    </AuthContext.Provider>
  );
};

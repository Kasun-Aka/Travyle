import { useState, useEffect } from 'react';
import { createUserWithEmailAndPassword, updateProfile } from 'firebase/auth';
import { auth } from '../lib/firebase';
import { authApi } from '../api/auth';
import { useAuth } from '../context/AuthContext';
import { useNavigate, Link } from 'react-router-dom';
import { Loader2, AlertCircle } from 'lucide-react';

export default function Signup() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [fullName, setFullName] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { currentUser, dbUser, loading: authLoading } = useAuth();

  // If already authenticated as Admin/Operator, navigate to dashboard
  useEffect(() => {
    if (!authLoading && currentUser && dbUser && ["admin", "tour operator"].includes(dbUser.role?.toLowerCase())) {
      navigate('/welcome', { replace: true });
    }
  }, [currentUser, dbUser, authLoading, navigate]);

  const handleSignup = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      if (!auth) {
        throw new Error('Firebase is not configured. Add the VITE_FIREBASE_* values to web-admin/.env.');
      }
      const userCredential = await createUserWithEmailAndPassword(auth, email, password);
      await updateProfile(userCredential.user, { displayName: fullName });

      try {
        await authApi.sync({
          firebaseUid: userCredential.user.uid,
          email: userCredential.user.email || email,
          fullName: fullName || email.split('@')[0],
          role: 'Admin',
        });
      } catch (syncErr) {
        console.warn("Backend sync notice:", syncErr);
      }

      navigate('/welcome');
    } catch (err: any) {
      console.error("Signup error:", err);
      let msg = 'Failed to create account';
      if (err.code === 'auth/email-already-in-use') {
        msg = 'This email is already registered. Please sign in instead.';
      } else if (err.code === 'auth/weak-password') {
        msg = 'Password should be at least 6 characters.';
      } else if (err.code === 'auth/invalid-email') {
        msg = 'Please enter a valid email address.';
      } else if (err.message) {
        msg = err.message;
      }
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen w-screen bg-[#F8F9FB]">
      {/* Sidebar matching the dashboard */}
      <aside className="hidden md:flex w-[260px] bg-[#171B2F] text-white flex-col px-6 py-[30px]">
        <div className="flex items-center gap-4 mb-[60px]">
          <div className="w-10 h-10 bg-indigo-600 rounded-full flex items-center justify-center font-bold text-xl text-white">T</div>
          <div className="flex flex-col">
            <h1 className="m-0 text-xl font-bold tracking-wide">Travyle</h1>
            <p className="mt-[2px] mb-0 text-xs text-slate-400">Admin console</p>
          </div>
        </div>

        <div className="mt-auto opacity-50">
          <div className="h-px bg-slate-700 mb-4"></div>
          <p className="text-xs text-slate-400">Secure Access Portal</p>
        </div>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 flex flex-col items-center justify-center p-10">
        <div className="bg-white w-full max-w-[440px] rounded-xl p-12 shadow-sm border border-slate-200">
          <div className="mb-8">
            <h2 className="m-0 mb-2 text-[28px] font-bold text-slate-900">Create an account</h2>
            <p className="m-0 text-slate-500 text-[15px]">Join Travyle Admin Dashboard</p>
          </div>

          {error && (
            <div className="mb-6 bg-red-50 text-red-600 p-3 rounded-lg text-sm flex items-center gap-2 border border-red-100">
              <AlertCircle size={16} className="shrink-0" />
              <p>{error}</p>
            </div>
          )}

          <form className="flex flex-col gap-5" onSubmit={handleSignup}>
            <div className="flex flex-col gap-2">
              <label htmlFor="fullName" className="text-sm font-semibold text-slate-700">Full Name</label>
              <input
                type="text"
                id="fullName"
                required
                value={fullName}
                onChange={e => setFullName(e.target.value)}
                placeholder="John Doe"
                className="px-4 py-3 border border-slate-300 rounded-lg text-[15px] text-slate-900 transition-all duration-200 focus:outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20 font-inherit"
              />
            </div>

            <div className="flex flex-col gap-2">
              <label htmlFor="email" className="text-sm font-semibold text-slate-700">Email address</label>
              <input
                type="email"
                id="email"
                required
                value={email}
                onChange={e => setEmail(e.target.value)}
                placeholder="admin@travyle.com"
                className="px-4 py-3 border border-slate-300 rounded-lg text-[15px] text-slate-900 transition-all duration-200 focus:outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20 font-inherit"
              />
            </div>

            <div className="flex flex-col gap-2">
              <label htmlFor="password" className="text-sm font-semibold text-slate-700">Password</label>
              <input
                type="password"
                id="password"
                required
                minLength={6}
                value={password}
                onChange={e => setPassword(e.target.value)}
                placeholder="••••••••"
                className="px-4 py-3 border border-slate-300 rounded-lg text-[15px] text-slate-900 transition-all duration-200 focus:outline-none focus:border-indigo-600 focus:ring-2 focus:ring-indigo-600/20 font-inherit"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="mt-2 bg-indigo-600 text-white border-none rounded-lg p-3.5 text-[15px] font-semibold cursor-pointer transition-colors duration-200 flex justify-center items-center gap-2 font-inherit hover:bg-indigo-700 disabled:opacity-50"
            >
              {loading ? <Loader2 size={18} className="animate-spin" /> : 'Sign Up'}
            </button>
          </form>

          <p className="text-center text-sm text-slate-500 mt-8">
            Already have an account? <Link to="/login" className="text-indigo-600 font-semibold hover:underline">Sign in</Link>
          </p>
        </div>
      </main>
    </div>
  );
}

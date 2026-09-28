import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import App from './App.tsx';
import Welcome from './pages/Welcome';
import TravelTrends from './pages/TravelTrends';
import MasterTourPackages from './pages/MasterTourPackages';
import TravelerConcierge from './pages/TravelerConcierge';
import LiveTourOperations from './pages/LiveTourOperations';
import GuideAssignmentMatrix from './pages/GuideAssignmentMatrix';
import StaffAccessControl from './pages/StaffAccessControl';
import { SupportDashboard } from './pages/SupportDashboard';
import { VouchersPage } from './pages/VouchersPage';
import { CustomerReviewsPage } from './pages/CustomerReviewsPage';
import DashboardLayout from './layouts/DashboardLayout';
import './index.css';
import './admin-theme.css';
import { AuthProvider, useAuth } from './context/AuthContext';
import Login from './pages/Login';
import Signup from './pages/Signup';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { currentUser, dbUser, loading } = useAuth();

  if (loading) {
    return (
      <div className="flex min-h-screen w-screen items-center justify-center bg-[#071b33] text-white">
        <div className="flex flex-col items-center gap-3">
          <div className="w-10 h-10 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-sm text-slate-300">Loading admin console...</p>
        </div>
      </div>
    );
  }

  if (!currentUser) return <Navigate to="/login" replace />;

  const isStaff = dbUser && ["Admin", "Operator"].some(r => r.toLowerCase() === dbUser.role?.toLowerCase());
  if (!isStaff) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
};

const AppRoutes = () => (
  <Routes>
    <Route path="/login" element={<Login />} />
    <Route path="/signup" element={<Signup />} />
    
    <Route element={<ProtectedRoute><DashboardLayout /></ProtectedRoute>}>
      <Route path="/" element={<Navigate to="/welcome" replace />} />
      <Route path="/welcome" element={<Welcome />} />
      <Route path="/trends" element={<TravelTrends />} />
      <Route path="/catalog/packages" element={<MasterTourPackages />} />
      <Route path="/catalog/concierge" element={<TravelerConcierge />} />
      <Route path="/operations/live-operations" element={<LiveTourOperations />} />
      <Route path="/operations/guide-assignments" element={<GuideAssignmentMatrix />} />
      <Route path="/operations/staff-access" element={<StaffAccessControl />} />
      
      {/* Support & Customer Quality Routes */}
      <Route path="/support/tickets" element={<SupportDashboard />} />
      <Route path="/support/vouchers" element={<VouchersPage />} />
      <Route path="/support/reviews" element={<CustomerReviewsPage />} />
    </Route>

    <Route path="/bookings" element={<ProtectedRoute><App /></ProtectedRoute>} />
    <Route path="*" element={<Navigate to="/welcome" replace />} />
  </Routes>
);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>,
);
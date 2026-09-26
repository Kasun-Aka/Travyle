import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import App from './App.tsx';
import Welcome from './pages/Welcome';
import TravelTrends from './pages/TravelTrends';
import MasterTourPackages from './pages/MasterTourPackages';
import LiveTourOperations from './pages/LiveTourOperations';
import GuideAssignmentMatrix from './pages/GuideAssignmentMatrix';
import StaffAccessControl from './pages/StaffAccessControl';
import DashboardLayout from './layouts/DashboardLayout';
import './index.css';
import './admin-theme.css';
import { AuthProvider, useAuth } from './context/AuthContext';
import Login from './pages/Login';
import Signup from './pages/Signup';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { currentUser, dbUser, loading } = useAuth();

  if (loading) return null;
  if (!currentUser) return <Navigate to="/login" replace />;
  if (!dbUser || !["Admin", "Operator"].includes(dbUser.role)) {
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
      <Route path="/operations/live-operations" element={<LiveTourOperations />} />
      <Route path="/operations/guide-assignments" element={<GuideAssignmentMatrix />} />
      <Route path="/operations/staff-access" element={<StaffAccessControl />} />
    </Route>
    <Route path="/bookings" element={<ProtectedRoute><App /></ProtectedRoute>} />
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
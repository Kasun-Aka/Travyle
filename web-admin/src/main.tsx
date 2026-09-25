import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import App from './App.tsx';
import Login from './pages/Login';
import Signup from './pages/Signup';
import Welcome from './pages/Welcome';
import TravelTrends from './pages/TravelTrends';
import MasterTourPackages from './pages/MasterTourPackages';
import LiveTourOperations from './pages/LiveTourOperations';
import GuideAssignmentMatrix from './pages/GuideAssignmentMatrix';
import StaffAccessControl from './pages/StaffAccessControl';
import DashboardLayout from './layouts/DashboardLayout';
import { AuthProvider } from './context/AuthContext';
import './index.css';
import './admin-theme.css';

const AppRoutes = () => (
  <Routes>
    <Route path="/login" element={<Login />} />
    <Route path="/signup" element={<Signup />} />
    <Route element={<DashboardLayout />}>
      <Route path="/" element={<Navigate to="/welcome" replace />} />
      <Route path="/welcome" element={<Welcome />} />
      <Route path="/trends" element={<TravelTrends />} />
      <Route path="/catalog/packages" element={<MasterTourPackages />} />
      <Route path="/operations/live-operations" element={<LiveTourOperations />} />
      <Route path="/operations/guide-assignments" element={<GuideAssignmentMatrix />} />
      <Route path="/operations/staff-access" element={<StaffAccessControl />} />
    </Route>
    <Route path="/bookings" element={<App />} />
  </Routes>
);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
);
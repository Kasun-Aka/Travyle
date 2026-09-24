import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import DashboardLayout from './layouts/DashboardLayout'
import MasterTourPackages from './pages/MasterTourPackages'
import TravelTrends from './pages/TravelTrends'
import GuideAssignmentMatrix from './pages/GuideAssignmentMatrix'
import LiveTourOperations from './pages/LiveTourOperations'
import StaffAccessControl from './pages/StaffAccessControl'
import { AuthProvider, useAuth } from './context/AuthContext'
import Login from './pages/Login'
import Signup from './pages/Signup'
import './index.css'

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { currentUser } = useAuth();
  if (!currentUser) return <Navigate to="/login" replace />;
  return <>{children}</>;
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/catalog/packages" replace />} />
          <Route path="/dashboard" element={<Navigate to="/catalog/packages" replace />} />
          <Route path="/login" element={<Login />} />
          <Route path="/signup" element={<Signup />} />
          <Route element={<ProtectedRoute><DashboardLayout /></ProtectedRoute>}>
            <Route path="/catalog/packages" element={<MasterTourPackages />} />
            <Route path="/trends" element={<TravelTrends />} />
            <Route path="/operations/live-operations" element={<LiveTourOperations />} />
            <Route path="/operations/guide-assignments" element={<GuideAssignmentMatrix />} />
            <Route path="/operations/staff-access" element={<StaffAccessControl />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>,
)

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import MasterTourPackages from './pages/MasterTourPackages'
import TravelTrends from './pages/TravelTrends'
import TravelerConcierge from './pages/TravelerConcierge'
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
          <Route path="/login" element={<Login />} />
          <Route path="/signup" element={<Signup />} />
          <Route path="/catalog/packages" element={<ProtectedRoute><MasterTourPackages /></ProtectedRoute>} />
          <Route path="/catalog/concierge" element={<ProtectedRoute><TravelerConcierge /></ProtectedRoute>} />
          <Route path="/trends" element={<ProtectedRoute><TravelTrends /></ProtectedRoute>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>,
)

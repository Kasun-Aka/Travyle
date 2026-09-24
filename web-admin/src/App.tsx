import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import GuideAssignmentMatrix from './pages/GuideAssignmentMatrix';
import LiveTourOperations from './pages/LiveTourOperations';
import StaffAccessControl from './pages/StaffAccessControl';
import { SupportDashboard } from './pages/SupportDashboard';
import { VouchersPage } from './pages/VouchersPage';
import { CustomerReviewsPage } from './pages/CustomerReviewsPage';
import Login from './components/Login';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/" element={<DashboardLayout />}>
          <Route index element={<Navigate to="/support/tickets" replace />} />
          {/* Operations routes */}
          <Route path="operations/guide-assignments" element={<GuideAssignmentMatrix />} />
          <Route path="operations/live-operations" element={<LiveTourOperations />} />
          <Route path="operations/staff-access" element={<StaffAccessControl />} />
          
          {/* Component 4: Support & Customer Quality routes */}
          <Route path="support/tickets" element={<SupportDashboard />} />
          <Route path="support/vouchers" element={<VouchersPage />} />
          <Route path="support/reviews" element={<CustomerReviewsPage />} />
          
          <Route path="*" element={<div style={{padding: '2rem'}}>Page under construction</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;


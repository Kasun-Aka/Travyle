
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import GuideAssignmentMatrix from './pages/GuideAssignmentMatrix';
import LiveTourOperations from './pages/LiveTourOperations';
import StaffAccessControl from './pages/StaffAccessControl';

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<DashboardLayout />}>
          <Route index element={<Navigate to="/operations/guide-assignments" replace />} />
          <Route path="operations/guide-assignments" element={<GuideAssignmentMatrix />} />
          <Route path="operations/live-operations" element={<LiveTourOperations />} />
          <Route path="operations/staff-access" element={<StaffAccessControl />} />
          <Route path="*" element={<div style={{padding: '2rem'}}>Page under construction</div>} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;

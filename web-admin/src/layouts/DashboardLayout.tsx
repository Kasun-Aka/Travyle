import React from 'react';
import { Outlet } from 'react-router-dom';
import { Search, Bell } from 'lucide-react';
import Sidebar from '../components/Sidebar';
import './DashboardLayout.css';

const DashboardLayout = () => {
  return (
    <div className="dashboard-layout">
      <Sidebar />
      <main className="dashboard-main">
        <div className="topbar">
          <div className="search-container">
            <Search className="search-icon" size={18} />
            <input type="text" className="search-input" placeholder="Search bookings, tours, tickets..." />
          </div>
          <div className="topbar-actions">
            <div className="status-pill healthy">
              <span className="dot"></span>
              API healthy
            </div>
            <div className="status-pill warning">
              <span className="dot"></span>
              Weather quota 78%
            </div>
            <button className="notification-btn">
              <Bell size={18} />
              <span className="notification-badge">5</span>
            </button>
          </div>
        </div>
        <div className="page-content">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default DashboardLayout;

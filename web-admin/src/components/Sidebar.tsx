import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  LayoutDashboard, 
  TrendingUp, 
  Map, 
  Calendar, 
  Activity, 
  Users, 
  Headphones 
} from 'lucide-react';
import './Sidebar.css';

const Sidebar = () => {
  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <div className="logo-icon">T</div>
        <div className="logo-text">
          <h1>Travyle</h1>
          <p>Admin console</p>
        </div>
      </div>

      <div className="sidebar-content">
        <div className="nav-section">
          <div className="nav-section-title">Overview</div>
          <NavLink to="/" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <LayoutDashboard size={20} className="nav-icon" />
            <span>Dashboard</span>
          </NavLink>
          <NavLink to="/trends" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <TrendingUp size={20} className="nav-icon" />
            <span>Travel trends</span>
          </NavLink>
        </div>

        <div className="nav-section">
          <div className="nav-section-title">Catalog</div>
          <NavLink to="/tours" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Map size={20} className="nav-icon" />
            <span>Tour packages</span>
          </NavLink>
        </div>

        <div className="nav-section">
          <div className="nav-section-title">Commerce</div>
          <NavLink to="/bookings" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Calendar size={20} className="nav-icon" />
            <span>Bookings & escrow</span>
            <span className="badge-count">2</span>
          </NavLink>
        </div>

        <div className="nav-section">
          <div className="nav-section-title">Operations</div>
          <NavLink to="/operations/live-operations" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Activity size={20} className="nav-icon" />
            <span>Live operations</span>
            <span className="badge-count">2</span>
          </NavLink>
          <NavLink to="/operations/guide-assignments" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Users size={20} className="nav-icon" />
            <span>Guide assignments</span>
          </NavLink>
          <NavLink to="/operations/staff-access" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Users size={20} className="nav-icon" />
            <span>Staff & access</span>
          </NavLink>
        </div>

        <div className="nav-section">
          <div className="nav-section-title">Customer quality</div>
          <NavLink to="/support" className={({isActive}) => isActive ? "nav-item active" : "nav-item"}>
            <Headphones size={20} className="nav-icon" />
            <span>Support queue</span>
            <span className="badge-count">3</span>
          </NavLink>
        </div>
      </div>

      <div className="sidebar-footer">
        <div className="user-profile">
          <div className="user-avatar">AD</div>
          <div className="user-info">
            <div className="user-name">Amara De Silva</div>
            <div className="user-role">System administrator</div>
          </div>
        </div>
        <div className="system-status">
          <span>Platform</span>
          <div className="status-indicator">
            <span className="status-dot"></span>
            All systems live
          </div>
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;

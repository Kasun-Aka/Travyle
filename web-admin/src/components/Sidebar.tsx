
import { NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  LayoutDashboard, 
  TrendingUp, 
  Map, 
  CreditCard, 
  Activity, 
  Users, 
  Headset 
} from 'lucide-react';

export default function Sidebar() {
  const { currentUser, logout } = useAuth();
  return (
    <aside className="admin-sidebar">
      {/* Brand Header */}
      <div className="sidebar-brand">
        <div className="sidebar-brand-mark">
          T
        </div>
        <div className="sidebar-brand-copy">
          <h1>Travyle</h1>
          <p>Admin console</p>
        </div>
      </div>

      {/* Navigation */}
      <div className="sidebar-scroll">
        
        {/* Overview Group */}
        <div className="sidebar-group">
          <h2>Overview</h2>
          <ul>
            <li>
              <NavLink
                to="/welcome"
                className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              >
                <LayoutDashboard size={18} />
                <span>Welcome</span>
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/trends"
                className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
              >
                <TrendingUp size={18} />
                <span>Travel trends</span>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Catalog Group */}
        <div className="sidebar-group">
          <h2>Catalog</h2>
          <ul>
            <li>
              <NavLink
                to="/catalog/packages"
                className={({ isActive }) => `sidebar-link sidebar-link-split ${isActive ? 'active' : ''}`}
              >
                <div className="sidebar-link-content">
                  <Map size={18} />
                  <span>Tour packages</span>
                </div>
                <span className="sidebar-chevron">&gt;</span>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Commerce Group */}
        <div className="sidebar-group">
          <h2>Commerce</h2>
          <ul>
            <li>
              <NavLink
                to="/bookings"
                className={({ isActive }) => `sidebar-link sidebar-link-split ${isActive ? 'active' : ''}`}
              >
                <div className="sidebar-link-content">
                  <CreditCard size={18} />
                  <span>Booking</span>
                </div>
                <span className="sidebar-count">2</span>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Operations Group */}
        <div className="sidebar-group">
          <h2>Operations</h2>
          <ul>
            <li>
              <NavLink to="/operations/live-operations" className={({ isActive }) => `sidebar-link sidebar-link-split ${isActive ? 'active' : ''}`}>
                <div className="sidebar-link-content">
                  <Activity size={18} />
                  <span>Live operations</span>
                </div>
                <span className="sidebar-count">2</span>
              </NavLink>
            </li>
            <li>
              <NavLink to="/operations/guide-assignments" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
                <Users size={18} />
                <span>Guide assignments</span>
              </NavLink>
            </li>
            <li>
              <NavLink to="/operations/staff-access" className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}>
                <div className="sidebar-link-content">
                  <Headset size={18} />
                  <span>Staff Access</span>
                </div>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Customer Quality Group */}
        <div className="sidebar-group">
          <h2>Customer quality</h2>
          <ul>
            <li>
              <a href="#" className="sidebar-link sidebar-link-split">
                <div className="sidebar-link-content">
                  <Headset size={18} />
                  <span>Support queue</span>
                </div>
                <span className="sidebar-count">3</span>
              </a>
            </li>
          </ul>
        </div>
      </div>

      {/* User Profile */}
      <div className="sidebar-footer">
        <div className="sidebar-user">
          <div className="sidebar-user-avatar">
            {currentUser?.email?.substring(0, 2) || 'AD'}
          </div>
          <div className="sidebar-user-copy">
            <p>{currentUser?.displayName || currentUser?.email || 'Amara De Silva'}</p>
            <span>System administrator</span>
          </div>
        </div>
        
        <div className="sidebar-status">
          <div>
            <span>Platform</span>
            <div className="sidebar-status-live">
              <i />
              <b>All systems live</b>
            </div>
          </div>
          <button 
            onClick={logout}
            className="sidebar-signout"
          >
            Sign Out
          </button>
        </div>
      </div>
    </aside>
  );
}

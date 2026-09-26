
import { NavLink } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { 
  LayoutDashboard, 
  TrendingUp, 
  Map, 
  CreditCard, 
  Activity, 
  Users, 
  Headset,
  Sparkles
} from 'lucide-react';

const linkBase = 'flex items-center gap-3 px-3 py-2 rounded-lg transition-colors text-sm font-medium';
const activeClass = 'bg-white text-sidebar font-bold';
const inactiveClass = 'text-gray-300 hover:text-white hover:bg-white/10';

export default function Sidebar() {
  const { currentUser, logout } = useAuth();
  return (
    <div className="w-64 bg-sidebar h-screen text-white flex flex-col">
      {/* Brand Header */}
      <div className="p-6 flex items-center gap-3">
        <div className="w-10 h-10 bg-brand-600 rounded-full flex items-center justify-center font-bold text-lg">
          T
        </div>
        <div>
          <h1 className="font-bold text-lg leading-tight">Travyle</h1>
          <p className="text-xs text-gray-400">Admin console</p>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex-1 overflow-y-auto px-4 py-4 space-y-8">
        
        {/* Overview Group */}
        <div>
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 px-2">Overview</h2>
          <ul className="space-y-1">
            <li>
              <NavLink
                to="/dashboard"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}
              >
                <LayoutDashboard size={18} />
                <span>Dashboard</span>
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/trends"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}
              >
                <TrendingUp size={18} />
                <span>Travel trends</span>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Catalog Group */}
        <div>
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 px-2">Catalog</h2>
          <ul className="space-y-1">
            <li>
              <NavLink
                to="/catalog/packages"
                className={({ isActive }) => `${linkBase} flex justify-between ${isActive ? activeClass : inactiveClass}`}
              >
                <div className="flex items-center gap-3">
                  <Map size={18} />
                  <span>Tour packages</span>
                </div>
                <span className="text-xs font-bold">&gt;</span>
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/catalog/concierge"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}
              >
                <div className="flex items-center gap-3 text-brand-300">
                  <Sparkles size={18} />
                  <span className="text-brand-300">AI Concierge</span>
                </div>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Commerce Group */}
        <div>
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 px-2">Commerce</h2>
          <ul className="space-y-1">
            <li>
              <a href="#" className={`${linkBase} ${inactiveClass} flex justify-between`}>
                <div className="flex items-center gap-3">
                  <CreditCard size={18} />
                  <span>Bookings &amp; escrow</span>
                </div>
                <span className="bg-white/10 text-xs px-2 py-0.5 rounded-full">2</span>
              </a>
            </li>
          </ul>
        </div>

        {/* Operations Group */}
        <div>
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 px-2">Operations</h2>
          <ul className="space-y-1">
            <li>
              <a href="#" className={`${linkBase} ${inactiveClass} flex justify-between`}>
                <div className="flex items-center gap-3">
                  <Activity size={18} />
                  <span>Live operations</span>
                </div>
                <span className="bg-white/10 text-xs px-2 py-0.5 rounded-full">2</span>
              </a>
            </li>
            <li>
              <a href="#" className={`${linkBase} ${inactiveClass}`}>
                <Users size={18} />
                <span>Guide assignments</span>
              </a>
            </li>
          </ul>
        </div>

        {/* Customer Quality Group */}
        <div>
          <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-3 px-2">Customer quality</h2>
          <ul className="space-y-1">
            <li>
              <a href="#" className={`${linkBase} ${inactiveClass} flex justify-between`}>
                <div className="flex items-center gap-3">
                  <Headset size={18} />
                  <span>Support queue</span>
                </div>
                <span className="bg-white/10 text-xs px-2 py-0.5 rounded-full">3</span>
              </a>
            </li>
          </ul>
        </div>
      </div>

      {/* User Profile */}
      <div className="p-4 mt-auto border-t border-white/10">
        <div className="flex items-center gap-3 p-2 bg-white/5 rounded-lg mb-4">
          <div className="w-10 h-10 bg-brand-500 rounded-full flex items-center justify-center font-bold text-sm uppercase">
            {currentUser?.email?.substring(0, 2) || 'AD'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-bold leading-tight truncate">{currentUser?.displayName || currentUser?.email || 'Amara De Silva'}</p>
            <p className="text-xs text-gray-400">System administrator</p>
          </div>
        </div>
        
        <div className="flex flex-col gap-3 text-xs px-2 mb-2">
          <div className="flex items-center justify-between">
            <span className="text-gray-400">Platform</span>
            <div className="flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-emerald-500"></span>
              <span className="font-medium text-gray-200">All systems live</span>
            </div>
          </div>
          <button 
            onClick={logout}
            className="text-left text-gray-400 hover:text-white transition-colors py-1 w-fit font-medium"
          >
            Sign Out
          </button>
        </div>
      </div>
    </div>
  );
}

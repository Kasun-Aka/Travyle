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

export default function Sidebar() {
  const { currentUser, logout } = useAuth();

  const linkBase = "flex items-center gap-[11px] min-h-[42px] px-[12px] rounded-[11px] text-[#b9d4e8] no-underline text-[13px] font-semibold transition-all hover:text-white hover:bg-[#5caade24] hover:translate-x-[2px] lg:justify-start justify-center lg:px-[12px] px-0";
  const activeClass = "text-[#09233f] bg-white shadow-[0_8px_18px_rgba(0,0,0,0.12)] hover:bg-white hover:text-[#09233f] hover:translate-x-0";
  const inactiveClass = "";

  return (
    <aside className="w-[76px] lg:w-[264px] shrink-0 min-h-screen flex flex-col text-[#dcecf8] bg-gradient-to-b from-[#0c294b] to-[#071b33] shadow-[14px_0_34px_rgba(8,32,58,0.08)]">
      {/* Brand Header */}
      <div className="flex items-center justify-center lg:justify-start gap-[12px] p-[22px_12px_28px] lg:p-[28px_24px_34px]">
        <div className="grid w-[42px] h-[42px] place-items-center rounded-[13px] text-[#0c294b] bg-[#d7f0ff] font-serif font-extrabold text-[24px]">
          T
        </div>
        <div className="hidden lg:block">
          <h1 className="m-0 text-white text-[18px] tracking-[-0.02em]">Travyle</h1>
          <p className="m-0 mt-1 text-[#91b3ce] text-[10px] font-bold tracking-[0.14em] uppercase">Admin console</p>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex-1 overflow-y-auto px-[10px] lg:px-[14px] pb-[20px]">

        {/* Overview Group */}
        <div className="mb-[28px]">
          <h2 className="hidden lg:block m-[0_12px_10px] text-[#6e9fc1] text-[10px] font-extrabold tracking-[0.14em] uppercase">Overview</h2>
          <ul className="grid gap-1 m-0 p-0 list-none">
            <li>
              <NavLink
                to="/welcome"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}
              >
                <LayoutDashboard size={18} className="shrink-0" />
                <span className="hidden lg:inline">Welcome</span>
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/trends"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}
              >
                <TrendingUp size={18} className="shrink-0" />
                <span className="hidden lg:inline">Travel trends</span>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Catalog Group */}
        <div className="mb-[28px]">
          <h2 className="hidden lg:block m-[0_12px_10px] text-[#6e9fc1] text-[10px] font-extrabold tracking-[0.14em] uppercase">Catalog</h2>
          <ul className="grid gap-1 m-0 p-0 list-none">
            <li>
              <NavLink
                to="/catalog/packages"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass} lg:justify-between`}
              >
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <Map size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Tour packages</span>
                </div>
                <span className="hidden lg:inline text-[#8bb2d0] text-[18px] leading-none">&gt;</span>
              </NavLink>
            </li>
            <li>
              <NavLink
                to="/catalog/concierge"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}
              >
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0 text-brand-300">
                  <Sparkles size={18} className="shrink-0" />
                  <span className="hidden lg:inline text-brand-300">AI Concierge</span>
                </div>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Commerce Group */}
        <div className="mb-[28px]">
          <h2 className="hidden lg:block m-[0_12px_10px] text-[#6e9fc1] text-[10px] font-extrabold tracking-[0.14em] uppercase">Commerce</h2>
          <ul className="grid gap-1 m-0 p-0 list-none">
            <li>
              <NavLink
                to="/bookings"
                className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass} lg:justify-between`}
              >
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <CreditCard size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Booking</span>
                </div>

              </NavLink>
            </li>
          </ul>
        </div>

        {/* Operations Group */}
        <div className="mb-[28px]">
          <h2 className="hidden lg:block m-[0_12px_10px] text-[#6e9fc1] text-[10px] font-extrabold tracking-[0.14em] uppercase">Operations</h2>
          <ul className="grid gap-1 m-0 p-0 list-none">
            <li>
              <NavLink to="/operations/live-operations" className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass} lg:justify-between`}>
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <Activity size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Live operations</span>
                </div>
              </NavLink>
            </li>
            <li>
              <NavLink to="/operations/guide-assignments" className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}>
                <Users size={18} className="shrink-0" />
                <span className="hidden lg:inline">Guide assignments</span>
              </NavLink>
            </li>
            <li>
              <NavLink to="/operations/staff-access" className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass}`}>
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <Headset size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Staff Access</span>
                </div>
              </NavLink>
            </li>
          </ul>
        </div>

        {/* Customer Quality Group */}
        <div className="mb-[28px]">
          <h2 className="hidden lg:block m-[0_12px_10px] text-[#6e9fc1] text-[10px] font-extrabold tracking-[0.14em] uppercase">Customer quality</h2>
          <ul className="grid gap-1 m-0 p-0 list-none">
            <li>
              <NavLink to="/support/tickets" className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass} lg:justify-between`}>
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <Headset size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Support queue</span>
                </div>
              </NavLink>
            </li>
            <li>
              <NavLink to="/support/vouchers" className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass} lg:justify-between`}>
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <CreditCard size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Goodwill Vouchers</span>
                </div>
              </NavLink>
            </li>
            <li>
              <NavLink to="/support/reviews" className={({ isActive }) => `${linkBase} ${isActive ? activeClass : inactiveClass} lg:justify-between`}>
                <div className="flex items-center gap-[11px] lg:gap-[11px] gap-0">
                  <Activity size={18} className="shrink-0" />
                  <span className="hidden lg:inline">Customer Reviews</span>
                </div>
              </NavLink>
            </li>
          </ul>
        </div>
      </div>

      {/* User Profile */}
      <div className="p-[14px_10px] lg:p-[18px_18px_22px] border-t border-[#aad3ee24]">
        <div className="flex items-center justify-center lg:justify-start gap-[11px] p-2 lg:p-[12px] rounded-[13px] bg-white/5">
          <div className="grid w-[36px] h-[36px] shrink-0 place-items-center rounded-[11px] text-[#0c3964] bg-[#c9eaff] text-[12px] font-extrabold uppercase">
            {currentUser?.email?.substring(0, 2) || 'AD'}
          </div>
          <div className="min-w-0 hidden lg:block">
            <p className="m-0 overflow-hidden text-white text-[12px] font-extrabold text-ellipsis whitespace-nowrap">{currentUser?.displayName || currentUser?.email || 'Amara De Silva'}</p>
            <span className="block mt-[3px] text-[#91b3ce] text-[10px]">System administrator</span>
          </div>
        </div>

        <div className="hidden lg:grid gap-[13px] pt-[16px] px-1 text-[#8eafc7] text-[11px]">
          <div className="flex items-center justify-between gap-2">
            <span>Platform</span>
            <div className="flex items-center gap-[6px] text-[#cfe7f6]">
              <i className="w-[7px] h-[7px] rounded-full bg-[#55d39a] shadow-[0_0_0_4px_rgba(85,211,154,0.13)]" />
              <b className="text-[10px] font-bold">All systems live</b>
            </div>
          </div>
          <button
            onClick={logout}
            className="w-fit p-0 text-[#8eafc7] bg-transparent text-[11px] font-bold border-none cursor-pointer hover:text-white"
          >
            Sign Out
          </button>
        </div>
      </div>
    </aside>
  );
}

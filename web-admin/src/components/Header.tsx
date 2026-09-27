
import { Search, Bell } from 'lucide-react';

export default function Header() {
  return (
    <div className="h-20 bg-gray-50 flex items-center justify-between px-8 border-b border-gray-200/60 sticky top-0 z-10">
      <div className="flex-1 max-w-xl relative">
        <Search className="absolute left-4 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
        <input 
          type="text" 
          placeholder="Search bookings, tours, tickets..." 
          className="w-full bg-white border-none pl-12 pr-4 py-3 rounded-full text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 shadow-sm"
        />
      </div>

      <div className="flex items-center gap-4">
        {/* API Status Pill */}
        <div className="flex items-center gap-2 bg-emerald-50 text-emerald-700 px-3 py-1.5 rounded-full border border-emerald-100">
          <div className="w-2 h-2 rounded-full bg-emerald-500"></div>
          <span className="text-xs font-bold tracking-wide uppercase">API healthy</span>
        </div>

        {/* Weather Quota Pill */}
        <div className="flex items-center gap-2 bg-amber-50 text-amber-700 px-3 py-1.5 rounded-full border border-amber-100">
          <div className="w-2 h-2 rounded-full bg-amber-500"></div>
          <span className="text-xs font-bold tracking-wide uppercase">Weather quota 78%</span>
        </div>

        {/* Notifications */}
        <button className="w-10 h-10 rounded-full bg-white shadow-sm flex items-center justify-center relative ml-2">
          <Bell size={20} className="text-gray-600" />
          <span className="absolute -top-1 -right-1 w-4 h-4 bg-red-500 rounded-full text-[10px] text-white flex items-center justify-center font-bold border border-white">
            5
          </span>
        </button>
      </div>
    </div>
  );
}

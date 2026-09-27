import { Outlet } from 'react-router-dom';
import { Search, Bell } from 'lucide-react';
import Sidebar from '../components/Sidebar';

const DashboardLayout = () => {
  return (
    <div className="flex min-h-screen bg-[#f4f8fc]">
      <Sidebar />
      <main className="flex-1 flex flex-col min-w-0">
        <div className="flex justify-between items-center h-[74px] px-[16px] sm:px-[34px] bg-white border-b border-[#dce8f2] shadow-[0_3px_16px_rgba(16,45,77,0.04)] sticky top-0 z-10">
          <div className="relative w-full sm:w-[min(430px,42vw)]">
            <Search className="absolute left-[14px] top-1/2 -translate-y-1/2 text-[#8da0b3]" size={18} />
            <input type="text" className="w-full h-[42px] pl-[40px] pr-[16px] bg-[#f1f6fa] border border-transparent rounded-[11px] text-[14px] text-[#102d4d] placeholder:text-[#8da0b3] outline-none transition-all focus:bg-white focus:border-[#2f86c9] focus:ring-[3px] focus:ring-[#2f86c9]/20" placeholder="Search bookings, tours, tickets..." />
          </div>
          <div className="hidden sm:flex items-center gap-[10px]">
            <div className="inline-flex items-center gap-[6px] px-[12px] py-[6px] rounded-full text-[11px] font-semibold text-[#288d6b] bg-[#eaf8f2]">
              <span className="w-[6px] h-[6px] rounded-full bg-[#288d6b]"></span>
              API healthy
            </div>
            <div className="inline-flex items-center gap-[6px] px-[12px] py-[6px] rounded-full text-[11px] font-semibold text-[#a16e1e] bg-[#fff5dd]">
              <span className="w-[6px] h-[6px] rounded-full bg-[#a16e1e]"></span>
              Weather quota 78%
            </div>
            <button className="relative w-[40px] h-[40px] rounded-full border border-[#dce8f2] flex items-center justify-center text-[#6d849c] bg-white hover:bg-[#f9fafb] transition-all cursor-pointer">
              <Bell size={18} />
              <span className="absolute -top-[2px] -right-[2px] w-[18px] h-[18px] rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center border-2 border-white">5</span>
            </button>
          </div>
        </div>
        <div className="w-[min(1240px,100%)] min-w-0 mx-auto p-[20px_14px_32px] sm:p-[32px_38px_48px] overflow-x-hidden">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default DashboardLayout;

import { useState, useEffect } from 'react';
import { UserPlus, Shield, Activity, Users } from 'lucide-react';

const StaffAccessControl = () => {
  const [staff, setStaff] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('http://localhost:5085/api/admin/staff')
      .then(r => r.json())
      .then(data => {
        setStaff(data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  return (
    <div className="p-8 max-w-[1200px] mx-auto w-full bg-gray-50/50 min-h-screen">
      <div className="mb-6">
        <div className="inline-flex items-center gap-2 bg-indigo-50 text-indigo-600 px-3 py-1 rounded-full text-xs font-semibold mb-4">
          <span className="w-1 h-1 bg-indigo-600 rounded-full"></span>
          System
        </div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2 tracking-tight">Staff & Access Control</h1>
        <p className="text-gray-500 max-w-[800px] leading-relaxed mb-4">
          Provision Guide and Operator accounts against Firebase, manage role assignments, and monitor health.
        </p>
        <button className="inline-flex items-center justify-center gap-2 mb-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold py-2 px-4 rounded-lg transition-colors shadow-sm">
          <UserPlus size={16} /> <span>Provision staff account</span>
        </button>
        <div className="flex gap-3 mt-4 flex-wrap">
          <div className="inline-flex items-center bg-white border border-gray-200 rounded-full py-1 pr-3 pl-1 text-xs font-mono text-gray-500">
            <span className="font-bold text-[0.7rem] px-2 py-0.5 rounded-full mr-2 bg-green-100 text-green-700">POST</span>/api/admin/staff
          </div>
          <div className="inline-flex items-center bg-white border border-gray-200 rounded-full py-1 pr-3 pl-1 text-xs font-mono text-gray-500">
            <span className="font-bold text-[0.7rem] px-2 py-0.5 rounded-full mr-2 bg-indigo-100 text-indigo-700">GET</span>/api/admin/staff
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 mb-6 overflow-hidden">
        <div className="p-5 border-b border-gray-100">
          <div className="flex items-center gap-2">
            <Users size={20} className="text-gray-500" />
            <h2 className="text-lg font-bold text-gray-800">Staff accounts</h2>
          </div>
          <p className="text-sm text-gray-500 mt-1">{staff.length} provisioned Guide and Operator accounts</p>
        </div>
        
        {loading ? (
          <div className="p-10 text-center text-gray-500">Loading staff data...</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 border-b border-gray-100 text-gray-500">
                <tr>
                  <th className="font-semibold p-4">Staff member</th>
                  <th className="font-semibold p-4">Role</th>
                  <th className="font-semibold p-4">Provisioned</th>
                  <th className="font-semibold p-4">Status</th>
                  <th className="text-right font-semibold p-4">Access</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {staff.map((s) => (
                  <tr key={s.id} className="hover:bg-gray-50/50 transition-colors">
                    <td className="p-4">
                      <div className="font-bold text-gray-800 mb-0.5">{s.fullName}</div>
                      <div className="text-xs text-gray-400">{s.firebaseUid} • {s.email}</div>
                    </td>
                    <td className="p-4">
                      <span className={`inline-flex px-3 py-1 rounded-full text-xs font-semibold ${s.role.toLowerCase().includes('operator') ? 'bg-indigo-50 text-indigo-700 border border-indigo-100' : 'bg-purple-50 text-purple-700 border border-purple-100'}`}>
                        {s.role}
                      </span>
                    </td>
                    <td className="p-4 text-gray-500 text-sm">
                      {new Date(s.createdAt).toLocaleDateString()}
                    </td>
                    <td className="p-4">
                      <span className="inline-flex px-2 py-1 rounded-md text-xs font-bold bg-green-50 text-green-700 border border-green-100">Active</span>
                    </td>
                    <td className="text-right p-4">
                      <button className="text-sm font-medium text-gray-500 hover:text-gray-900 transition-colors mr-4">Edit role</button>
                      <button className="text-sm font-medium border border-gray-200 text-gray-600 hover:bg-gray-50 px-3 py-1.5 rounded-lg transition-colors">Suspend</button>
                    </td>
                  </tr>
                ))}
                {staff.length === 0 && (
                  <tr>
                    <td colSpan={5} className="p-10 text-center text-gray-500">No staff accounts found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="p-5">
          <div className="flex items-center gap-2">
            <Activity size={20} className="text-gray-500" />
            <h2 className="text-lg font-bold text-gray-800">Shared foundation health</h2>
          </div>
          <p className="text-sm text-gray-500 mt-1 mb-4">GET /api/health and third-party integration status</p>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div className="bg-white border border-gray-100 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-800 text-sm">Web API (ASP.NET Core)</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase bg-green-50 text-green-700 border border-green-200">OK</span>
              </div>
              <div className="text-xs text-gray-500">GET /api/health • 200 OK</div>
            </div>
            <div className="bg-white border border-gray-100 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-800 text-sm">PostgreSQL (Supabase)</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase bg-green-50 text-green-700 border border-green-200">OK</span>
              </div>
              <div className="text-xs text-gray-500">Latency 42 ms</div>
            </div>
            <div className="bg-white border border-gray-100 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-800 text-sm">Agentic AI Orchestrator</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase bg-green-50 text-green-700 border border-green-200">OK</span>
              </div>
              <div className="text-xs text-gray-500">4 agents online</div>
            </div>
            <div className="bg-white border border-gray-100 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-800 text-sm">Stripe Sandbox</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase bg-green-50 text-green-700 border border-green-200">OK</span>
              </div>
              <div className="text-xs text-gray-500">Escrow webhooks healthy</div>
            </div>
            <div className="bg-white border border-gray-100 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-800 text-sm">OpenWeatherMap</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase bg-yellow-50 text-yellow-700 border border-yellow-200">Watch</span>
              </div>
              <div className="text-xs text-gray-500">Rate limit 78% used</div>
            </div>
            <div className="bg-white border border-gray-100 rounded-lg p-4 shadow-sm hover:shadow-md transition-shadow">
              <div className="flex justify-between items-center mb-2">
                <span className="font-semibold text-gray-800 text-sm">SendGrid / Twilio</span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full uppercase bg-green-50 text-green-700 border border-green-200">OK</span>
              </div>
              <div className="text-xs text-gray-500">Queue 3 pending</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StaffAccessControl;

import { useState, useEffect } from 'react';
import { ChevronDown, Zap, Calendar, Users, X } from 'lucide-react';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? import.meta.env.VITE_API_URL ?? 'http://localhost:5085/api';

const GuideAssignmentMatrix = () => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  const [modalOpen, setModalOpen] = useState(false);
  const [modalData, setModalData] = useState<any>(null);
  const [unassignedSchedules, setUnassignedSchedules] = useState<any[]>([]);
  const [modalLoading, setModalLoading] = useState(false);

  const fetchMatrix = () => {
    fetch(`${API_BASE}/admin/guide-matrix`)
      .then(r => r.json())
      .then(d => {
        setData(d);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  };

  useEffect(() => {
    fetchMatrix();
  }, []);

  const handleCellClick = (guide: any, slotIndex: number, currentSlot: any) => {
    const d = new Date();
    d.setDate(d.getDate() + slotIndex);
    
    setModalData({
      guideId: guide.id,
      guideName: guide.name,
      date: d,
      currentSlot
    });
    setModalOpen(true);
    setModalLoading(true);

    fetch(`${API_BASE}/admin/unassigned-schedules?date=${d.toISOString()}`)
      .then(r => r.json())
      .then(data => {
        setUnassignedSchedules(data);
        setModalLoading(false);
      })
      .catch(err => {
        console.error(err);
        setModalLoading(false);
      });
  };

  const handleAssign = (scheduleId: string) => {
    fetch(`${API_BASE}/admin/assign-guide`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ guideId: modalData.guideId, bookingScheduleId: scheduleId })
    })
      .then(r => {
        if (r.ok) {
          setModalOpen(false);
          setLoading(true);
          fetchMatrix();
        } else {
          alert('Failed to assign guide');
        }
      });
  };

  const handleUnassign = (scheduleId: string) => {
    fetch(`${API_BASE}/admin/unassign-guide/${modalData.guideId}/${scheduleId}`, {
      method: 'DELETE'
    })
      .then(r => {
        if (r.ok) {
          setModalOpen(false);
          setLoading(true);
          fetchMatrix();
        } else {
          alert('Failed to unassign guide');
        }
      });
  };

  if (loading && !data) {
    return <div className="p-10 text-center text-gray-500">Loading Matrix...</div>;
  }

  const { guides, roster } = data || {};

  return (
    <div className="page-container bg-gray-50/50 min-h-screen relative">
      <div className="page-header pb-6">
        <div className="page-breadcrumb">
          <span className="dot"></span>
          Operations & staffing
        </div>
        <h1 className="page-title text-2xl font-extrabold text-gray-800">Guide assignment matrix</h1>
        <p className="page-subtitle text-gray-500">
          See the whole week at once, spot double-bookings and idle capacity, and reassign guides or drivers to specific tours and slots.
        </p>
      </div>

      <div className="card matrix-card bg-white rounded-xl shadow-sm border border-gray-100 mb-6 overflow-hidden">
        <div className="matrix-header p-5 border-b border-gray-100 flex flex-col md:flex-row md:justify-between md:items-center bg-gray-50/50">
          <div>
            <div className="matrix-week flex items-center gap-2 text-lg font-bold text-gray-800">
              <Calendar size={18} className="text-gray-500" />
              Week of {new Date().getFullYear()}-{new Date().getMonth() + 1}-{new Date().getDate()} - {new Date().getFullYear()}-{new Date().getMonth() + 1}-{new Date().getDate() + 6}
            </div>
            <div className="matrix-subtitle text-sm text-gray-500 mt-1">Click any cell to assign, reassign, or clear a guide</div>
          </div>
          <div className="matrix-badge bg-blue-50 text-blue-700 px-3 py-1.5 rounded-full text-sm font-semibold mt-4 md:mt-0">
            {guides?.length || 0} guides - 7 days
          </div>
        </div>

        <div className="matrix-table-container overflow-x-auto">
          <table className="matrix-table w-full text-sm">
            <thead className="bg-white border-b border-gray-100 text-gray-500 text-left">
              <tr>
                <th className="th-guide font-semibold p-4 border-r border-gray-50 min-w-[200px]">Guide</th>
                {[...Array(7)].map((_, i) => {
                  const d = new Date();
                  d.setDate(d.getDate() + i);
                  return (
                    <th key={i} className="font-semibold p-4 text-center">
                      {d.toLocaleDateString('en-US', { weekday: 'short' })}
                    </th>
                  );
                })}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {(guides || []).map((guide: any) => (
                <tr key={guide.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="td-guide p-4 border-r border-gray-50">
                    <div className="guide-name font-bold text-gray-800">{guide.name}</div>
                    <div className="guide-meta text-xs text-gray-400 mt-1">{guide.meta}</div>
                  </td>
                  {guide.schedule.map((slot: any, i: number) => (
                    <td key={i} onClick={() => handleCellClick(guide, i, slot)} className="text-center p-2 border-r border-gray-50/50 hover:bg-gray-100/50 cursor-pointer transition-colors">
                      {slot.label === '-' ? (
                        <span className="text-gray-300">-</span>
                      ) : (
                        <div className="slot-badge inline-block px-2 py-1 rounded-md text-xs font-bold shadow-sm bg-blue-600 text-white">
                          {slot.label}
                        </div>
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card roster-card bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="roster-header p-5 border-b border-gray-100 flex flex-col lg:flex-row lg:justify-between lg:items-center bg-gray-50/50 gap-4">
          <div className="roster-title-area">
            <div className="flex items-center gap-2">
              <Users size={20} className="text-gray-500" />
              <h2 className="roster-title text-lg font-bold text-gray-800">Guide roster</h2>
            </div>
            <div className="roster-subtitle text-sm text-gray-500 mt-1">{roster?.length || 0} active guides</div>
          </div>
          <div className="roster-filters flex flex-wrap gap-3">
            <input type="text" className="filter-input border border-gray-200 rounded-lg px-3 py-1.5 text-sm w-full md:w-64 focus:outline-none focus:ring-2 focus:ring-blue-100 transition-shadow" placeholder="Search guide, ID, or location" />
            <div className="filter-select flex items-center justify-between border border-gray-200 rounded-lg px-3 py-1.5 text-sm bg-white cursor-pointer w-full md:w-40">
              All availability <ChevronDown size={16} className="text-gray-400" />
            </div>
            <div className="filter-select flex items-center justify-between border border-gray-200 rounded-lg px-3 py-1.5 text-sm bg-white cursor-pointer w-full md:w-40">
              Any language <ChevronDown size={16} className="text-gray-400" />
            </div>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="roster-table w-full text-sm">
            <thead className="bg-white border-b border-gray-100 text-gray-500 text-left">
              <tr>
                <th className="font-semibold p-4">Guide</th>
                <th className="font-semibold p-4">Languages</th>
                <th className="font-semibold p-4">Verification</th>
                <th className="font-semibold p-4">Load this week</th>
                <th className="font-semibold p-4">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {(roster || []).map((r: any, idx: number) => (
                <tr key={idx} className="hover:bg-gray-50/50 transition-colors">
                  <td className="p-4">
                    <div className="roster-guide-name font-bold text-gray-800">{r.name}</div>
                    <div className="roster-guide-meta text-xs text-gray-400 mt-1">{r.meta}</div>
                  </td>
                  <td className="p-4">
                    <div className="lang-badges flex flex-wrap gap-1">
                      {r.languages.map((lang: string) => (
                        <span key={lang} className="lang-badge bg-gray-100 text-gray-600 px-2 py-0.5 rounded text-xs font-semibold">{lang}</span>
                      ))}
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="badge badge-success bg-green-100 text-green-700 px-2 py-1 rounded-md text-xs font-bold">{r.verification}</span>
                  </td>
                  <td className="p-4 text-gray-600 font-medium">{r.load}</td>
                  <td className="p-4">
                    <span className="badge badge-info bg-blue-100 text-blue-700 px-2 py-1 rounded-md text-xs font-bold">{r.status}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {modalOpen && modalData && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-lg overflow-hidden flex flex-col">
            <div className="p-5 border-b border-gray-100 flex justify-between items-center bg-gray-50/50">
              <div>
                <h3 className="font-bold text-lg text-gray-800">Assign Guide</h3>
                <p className="text-sm text-gray-500 mt-1">
                  {modalData.guideName} • {modalData.date.toLocaleDateString('en-US', { weekday: 'long', month: 'short', day: 'numeric' })}
                </p>
              </div>
              <button onClick={() => setModalOpen(false)} className="text-gray-400 hover:text-gray-600 p-1 rounded-full hover:bg-gray-100 transition-colors">
                <X size={20} />
              </button>
            </div>
            
            <div className="p-5 flex-1 max-h-[60vh] overflow-y-auto">
              {modalData.currentSlot.label !== '-' && (
                <div className="mb-6">
                  <h4 className="text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wider">Current Assignment</h4>
                  <div className="flex items-center justify-between bg-blue-50 border border-blue-100 p-4 rounded-lg">
                    <div className="font-bold text-blue-800">{modalData.currentSlot.label}</div>
                    <button 
                      onClick={() => handleUnassign(modalData.currentSlot.scheduleId)}
                      className="px-3 py-1.5 bg-white border border-red-200 text-red-600 rounded-lg text-sm font-semibold hover:bg-red-50 transition-colors"
                    >
                      Unassign
                    </button>
                  </div>
                </div>
              )}

              <h4 className="text-sm font-semibold text-gray-700 mb-3 uppercase tracking-wider">Unassigned Tours for this Date</h4>
              {modalLoading ? (
                <div className="text-center py-6 text-gray-500 text-sm">Loading available tours...</div>
              ) : unassignedSchedules.length === 0 ? (
                <div className="text-center py-8 bg-gray-50 rounded-lg border border-gray-100 border-dashed text-gray-500 text-sm">
                  No unassigned tours found for this date.
                </div>
              ) : (
                <div className="space-y-3">
                  {unassignedSchedules.map(schedule => (
                    <div key={schedule.id} className="flex items-center justify-between p-3 border border-gray-100 rounded-lg hover:border-blue-200 hover:shadow-sm transition-all group">
                      <div>
                        <div className="font-bold text-gray-800">{schedule.title}</div>
                        <div className="text-xs text-gray-500 mt-0.5">{schedule.time || 'All Day'}</div>
                      </div>
                      <button 
                        onClick={() => handleAssign(schedule.id)}
                        className="px-4 py-1.5 bg-blue-600 text-white rounded-lg text-sm font-semibold hover:bg-blue-700 transition-colors opacity-0 group-hover:opacity-100"
                      >
                        Assign
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default GuideAssignmentMatrix;

import { useState, useEffect } from 'react';
import { ChevronDown, Zap, Calendar, Users } from 'lucide-react';

const GuideAssignmentMatrix = () => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetch('http://localhost:5085/api/admin/guide-matrix')
      .then(r => r.json())
      .then(d => {
        setData(d);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, []);

  if (loading && !data) {
    return <div className="p-10 text-center text-gray-500">Loading Matrix...</div>;
  }

  const { guides, roster } = data || {};

  return (
    <div className="page-container bg-gray-50/50 min-h-screen">
      <div className="page-header pb-6">
        <div className="page-breadcrumb">
          <span className="dot"></span>
          Operations & staffing
        </div>
        <h1 className="page-title text-2xl font-extrabold text-gray-800">Guide assignment matrix</h1>
        <p className="page-subtitle text-gray-500">
          See the whole week at once, spot double-bookings and idle capacity, and reassign guides or drivers to specific tours and slots.
        </p>
        <div className="api-paths">
          <div className="api-path"><span className="api-method get">GET</span>/api/admin/guide-matrix</div>
          <div className="api-path"><span className="api-method put">PUT</span>/api/operations/assignments/&#123;id&#125;</div>
        </div>
      </div>

      <div className="card matrix-card bg-white rounded-xl shadow-sm border border-gray-100 mb-6 overflow-hidden">
        <div className="matrix-header p-5 border-b border-gray-100 flex flex-col md:flex-row md:justify-between md:items-center bg-gray-50/50">
          <div>
            <div className="matrix-week flex items-center gap-2 text-lg font-bold text-gray-800">
              <Calendar size={18} className="text-gray-500" />
              Week of Sep 08 - Sep 14
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
                <th className="font-semibold p-4 text-center">Mon 08</th>
                <th className="font-semibold p-4 text-center">Tue 09</th>
                <th className="font-semibold p-4 text-center">Wed 10</th>
                <th className="font-semibold p-4 text-center">Thu 11</th>
                <th className="font-semibold p-4 text-center">Fri 12</th>
                <th className="font-semibold p-4 text-center">Sat 13</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {(guides || []).map((guide: any) => (
                <tr key={guide.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="td-guide p-4 border-r border-gray-50">
                    <div className="guide-name font-bold text-gray-800">{guide.name}</div>
                    <div className="guide-meta text-xs text-gray-400 mt-1">{guide.meta}</div>
                  </td>
                  {guide.schedule.map((slot: string, i: number) => (
                    <td key={i} className="text-center p-2 border-r border-gray-50/50 hover:bg-gray-100/50 cursor-pointer transition-colors">
                      {slot === '-' ? (
                        <span className="text-gray-300">-</span>
                      ) : (
                        <div className={`slot-badge inline-block px-2 py-1 rounded-md text-xs font-bold shadow-sm ${slot.startsWith('TOUR') ? 'bg-blue-600 text-white' : 'bg-orange-100 text-orange-700'
                          }`}>
                          {slot}
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

      <div className="agent-alert-box vertical bg-gradient-to-r from-blue-50 to-indigo-50/30 p-6 rounded-xl border border-blue-100 shadow-sm flex gap-4 mb-6">
        <div className="agent-icon-bg bg-blue-100 p-3 rounded-full h-fit flex-shrink-0">
          <Zap className="agent-icon text-blue-600" size={24} />
        </div>
        <div className="agent-content flex flex-col justify-between">
          <div>
            <div className="agent-title text-blue-900 font-bold mb-2">Operations Agent</div>
            <p className="agent-text text-blue-800/80 text-sm leading-relaxed">
              SLOT-8845 (Yala, Sep 14) still has no guide and Ishara Bandara is already on TOUR-5505 that morning. Kavindi Rathnayake is verified, EN/SI/JA, and has one tour this week — the lightest load available.
            </p>
          </div>
          <div className="agent-actions mt-6 flex gap-3">
            <button className="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-semibold shadow-sm hover:bg-blue-700 transition-colors">Assign Kavindi</button>
            <button className="bg-white text-blue-600 border border-blue-200 px-4 py-2 rounded-lg text-sm font-semibold hover:bg-blue-50 transition-colors">Leave unassigned</button>
          </div>
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
    </div>
  );
};

export default GuideAssignmentMatrix;

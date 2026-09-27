import { useState, useEffect } from 'react';
import { RefreshCw, Zap, Map, AlertTriangle } from 'lucide-react';

const LiveTourOperations = () => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  const fetchData = () => {
    setLoading(true);
    fetch('http://localhost:5085/api/admin/live-operations')
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
    fetchData();
  }, []);

  if (loading && !data) {
    return <div className="p-10 text-center text-gray-500">Loading Live Operations...</div>;
  }

  const { stats, activeTours, alerts } = data || {};

  return (
    <div className="p-8 max-w-[1200px] mx-auto w-full bg-gray-50/50 min-h-screen">
      <div className="mb-6">
        <div className="inline-flex items-center gap-2 bg-indigo-50 text-indigo-600 px-3 py-1 rounded-full text-xs font-semibold mb-4">
          <span className="w-1 h-1 bg-indigo-600 rounded-full"></span>
          Operations & staffing
        </div>
        <h1 className="text-3xl font-bold text-gray-900 mb-2 tracking-tight">Live tour operations</h1>
        <p className="text-gray-500 max-w-[800px] leading-relaxed mb-4">
          Track every tour in progress against its planned route, triage weather and traffic disruptions, and approve agent-proposed stop re-ordering.
        </p>
        <button className="inline-flex items-center gap-2 bg-white border border-gray-200 text-gray-700 hover:bg-gray-50 rounded-lg px-4 py-2 text-sm font-semibold transition-colors shadow-sm mb-2" onClick={fetchData}>
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} /> {loading ? 'Refreshing...' : 'Refresh pings'}
        </button>
        <div className="flex gap-3 mt-4 flex-wrap">
          <div className="inline-flex items-center bg-white border border-gray-200 rounded-full py-1 pr-3 pl-1 text-xs font-mono text-gray-500">
            <span className="font-bold text-[0.7rem] px-2 py-0.5 rounded-full mr-2 bg-green-100 text-green-700">POST</span>/api/operations/route-logs
          </div>
          <div className="inline-flex items-center bg-white border border-gray-200 rounded-full py-1 pr-3 pl-1 text-xs font-mono text-gray-500">
            <span className="font-bold text-[0.7rem] px-2 py-0.5 rounded-full mr-2 bg-indigo-100 text-indigo-700">GET</span>/api/admin/live-operations
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <div className="bg-white p-5 rounded-xl shadow-sm border border-gray-100 hover:shadow-md transition-shadow">
          <div className="text-xs text-gray-500 font-semibold uppercase tracking-wider mb-2">Tours in progress</div>
          <div className="text-3xl font-black text-gray-800 my-1">{stats?.toursInProgress || 0}</div>
          <div className="text-orange-500 text-xs font-semibold">{stats?.flagged || 0} flagged</div>
        </div>
        <div className="bg-white p-5 rounded-xl shadow-sm border border-gray-100 hover:shadow-md transition-shadow">
          <div className="text-xs text-gray-500 font-semibold uppercase tracking-wider mb-2">Guides on duty</div>
          <div className="text-3xl font-black text-gray-800 my-1">{stats?.guidesOnDuty || 0}</div>
          <div className="text-green-500 text-xs font-semibold">{stats?.guidesAvailable || 0} available for reassignment</div>
        </div>
        <div className="bg-white p-5 rounded-xl shadow-sm border border-gray-100 hover:shadow-md transition-shadow">
          <div className="text-xs text-gray-500 font-semibold uppercase tracking-wider mb-2">Active alerts</div>
          <div className="text-3xl font-black text-gray-800 my-1">{stats?.activeAlerts || 0}</div>
          <div className="text-gray-400 text-xs font-medium">OpenWeatherMap + Traffic</div>
        </div>
        <div className="bg-white p-5 rounded-xl shadow-sm border border-gray-100 hover:shadow-md transition-shadow">
          <div className="text-xs text-gray-500 font-semibold uppercase tracking-wider mb-2">Avg. schedule drift</div>
          <div className="text-3xl font-black text-red-600 my-1">{stats?.avgScheduleDrift || "+0 min"}</div>
          <div className="text-red-400 text-xs font-semibold">vs yesterday</div>
        </div>
      </div>

      <div className="bg-white shadow-sm border border-gray-100 rounded-xl overflow-hidden mb-6">
        <div className="p-5 border-b border-gray-100">
          <div className="flex items-center gap-2 mb-1">
            <Map className="text-gray-500" size={20} />
            <h2 className="text-lg font-bold text-gray-800">Live map dashboard</h2>
          </div>
          <p className="text-gray-400 text-sm">Select a marker to inspect the tour's route log</p>
        </div>
        <div className="relative bg-slate-50 overflow-hidden h-[400px]">
          <div className="absolute top-4 left-4 bg-white/90 backdrop-blur text-xs font-bold px-3 py-1.5 rounded-full shadow-sm text-gray-700 z-10">Live map - 1276 pings</div>
          
          <div className="absolute w-[300px] h-[250px] bg-green-100 rounded-[40px] opacity-50 top-[10%] left-[5%] pointer-events-none"></div>
          <div className="absolute w-[250px] h-[250px] bg-orange-100 rounded-full opacity-50 top-[30%] left-[45%] pointer-events-none"></div>
          <div className="absolute w-[350px] h-[200px] bg-indigo-100 rounded-[40px] opacity-50 bottom-[5%] right-[5%] pointer-events-none"></div>
          
          <div className="absolute bg-white px-3 py-1.5 rounded-full shadow-md text-xs font-bold text-gray-700 flex items-center gap-1.5 transform -translate-x-1/2 -translate-y-1/2 cursor-pointer z-10 hover:scale-105 transition-transform" style={{ top: '30%', left: '60%' }}>
            <span className="w-2 h-2 rounded-full bg-orange-400"></span>
            TOUR-5502
          </div>
          <div className="absolute bg-blue-600 text-white px-3 py-1.5 rounded-full shadow-lg text-xs font-bold flex items-center gap-1.5 transform -translate-x-1/2 -translate-y-1/2 cursor-pointer z-20 scale-110 hover:scale-110 transition-transform" style={{ top: '50%', left: '70%' }}>
            TOUR-5510
          </div>
          <div className="absolute bg-white px-3 py-1.5 rounded-full shadow-md text-xs font-bold text-gray-700 flex items-center gap-1.5 transform -translate-x-1/2 -translate-y-1/2 cursor-pointer z-10 hover:scale-105 transition-transform" style={{ top: '70%', left: '40%' }}>
            <span className="w-2 h-2 rounded-full bg-green-400"></span>
            TOUR-5505
          </div>
          <div className="absolute bg-white px-3 py-1.5 rounded-full shadow-md text-xs font-bold text-gray-700 flex items-center gap-1.5 transform -translate-x-1/2 -translate-y-1/2 cursor-pointer z-10 hover:scale-105 transition-transform" style={{ top: '80%', left: '80%' }}>
            <span className="w-2 h-2 rounded-full bg-green-400"></span>
            TOUR-5508
          </div>
          
          <div className="absolute bottom-4 left-4 bg-white/90 backdrop-blur px-2.5 py-1 rounded-md text-[10px] text-gray-500 font-mono shadow-sm">Map: 12.345, 80.678 (WGS84)</div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        <div className="lg:col-span-1">
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 h-full">
            <div className="flex justify-between items-start mb-6">
              <div>
                <h2 className="text-lg font-bold text-gray-800">Route log</h2>
                <p className="text-sm text-gray-500">TOUR-5510 • Ravi Perera</p>
              </div>
              <span className="bg-orange-50 text-orange-700 px-2.5 py-1 rounded-md text-xs font-bold border border-orange-100">Disruption</span>
            </div>
            
            <div className="relative pl-4 border-l-2 border-gray-100 space-y-6">
              <div className="relative">
                <div className="absolute -left-[21px] top-1 w-3 h-3 bg-gray-300 rounded-full border-2 border-white shadow-sm"></div>
                <div>
                  <div className="text-sm font-semibold text-gray-400 line-through">Hotel pickup — Ella town</div>
                  <div className="text-xs text-gray-400 mt-0.5">08:00</div>
                </div>
              </div>
              <div className="relative">
                <div className="absolute -left-[21px] top-1 w-3 h-3 bg-blue-500 rounded-full border-2 border-white shadow-[0_0_0_4px_rgba(59,130,246,0.2)]"></div>
                <div>
                  <div className="text-sm font-bold text-gray-800">Little Adam's Peak</div>
                  <div className="text-xs font-medium text-blue-600 mt-0.5">13:00 - 38 mins in rain</div>
                </div>
              </div>
              <div className="relative">
                <div className="absolute -left-[21px] top-1 w-3 h-3 bg-white border-2 border-gray-200 rounded-full"></div>
                <div>
                  <div className="text-sm font-semibold text-gray-500">Ravana Falls</div>
                  <div className="text-xs text-gray-400 mt-0.5">14:30</div>
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <div className="lg:col-span-2">
          <div className="bg-gradient-to-br from-indigo-50/80 to-blue-50/50 p-6 rounded-xl border border-indigo-100/50 shadow-sm flex gap-5 h-full items-start">
            <div className="bg-white p-3 rounded-xl shadow-sm border border-indigo-50 flex-shrink-0">
              <Zap className="text-indigo-600" size={24} />
            </div>
            <div className="flex flex-col justify-between h-full w-full">
              <div>
                <div className="text-indigo-900 font-bold mb-2 text-sm uppercase tracking-wide">Operations Agent</div>
                <p className="text-indigo-800/80 text-sm leading-relaxed mb-4">
                  TOUR-5510 will hit the Nine Arches viewpoint inside a heavy-rain window. Re-ordering stops 4 and 6 keeps all six stops, adds 18 minutes, and moves the outdoor segment to 14:40 when the cell has cleared.
                </p>
              </div>
              <div className="flex gap-3 mt-auto flex-wrap">
                <button className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-semibold shadow-sm hover:bg-indigo-700 transition-colors">Approve re-route</button>
                <button className="bg-white text-indigo-600 border border-indigo-200 px-4 py-2 rounded-lg text-sm font-semibold hover:bg-indigo-50 transition-colors shadow-sm">Reassign guide instead</button>
                <button className="text-gray-500 hover:text-gray-700 px-4 py-2 text-sm font-medium transition-colors">Dismiss</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 mb-6 overflow-hidden">
        <div className="p-5 border-b border-gray-100">
          <div className="flex items-center gap-2 mb-1">
            <AlertTriangle className="text-orange-500" size={20} />
            <h2 className="text-lg font-bold text-gray-800">Disruption alerts</h2>
          </div>
          <p className="text-sm text-gray-500">Weather and traffic hazards with proposed route recalculations</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 border-b border-gray-100 text-gray-500">
              <tr>
                <th className="font-semibold p-4">Alert</th>
                <th className="font-semibold p-4">Severity</th>
                <th className="font-semibold p-4">Agent proposed TSP re-order</th>
                <th className="font-semibold p-4 text-right">Decision</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {(alerts || []).map((alert: any) => (
                <tr key={alert.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="p-4">
                    <div className="font-bold text-gray-800 mb-0.5">{alert.title}</div>
                    <div className="text-xs text-gray-400">{alert.id} • {alert.tourId} • {alert.source} • {alert.time}</div>
                  </td>
                  <td className="p-4">
                    <span className={`inline-flex px-2 py-1 rounded-md text-xs font-bold border ${
                      alert.severity === 'High' ? 'bg-red-50 text-red-700 border-red-100' :
                      alert.severity === 'Medium' ? 'bg-orange-50 text-orange-700 border-orange-100' :
                      'bg-gray-50 text-gray-700 border-gray-200'
                    }`}>{alert.severity}</span>
                  </td>
                  <td className="p-4 text-gray-600">{alert.proposal}</td>
                  <td className="p-4 text-right">
                    {alert.status === 'Pending' ? (
                      <div className="flex justify-end gap-2">
                        <button className="text-gray-500 hover:text-gray-700 font-medium px-3 py-1.5 transition-colors text-xs">Dismiss</button>
                        <button className="bg-indigo-50 text-indigo-600 hover:bg-indigo-100 font-semibold px-3 py-1.5 rounded-md transition-colors text-xs border border-indigo-100">Approve re-route</button>
                      </div>
                    ) : (
                      <span className="text-green-700 font-bold bg-green-50 border border-green-100 px-3 py-1.5 rounded-md text-xs">Approved</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="p-5 border-b border-gray-100">
          <h2 className="text-lg font-bold text-gray-800 mb-1">Active tour progress</h2>
          <p className="text-sm text-gray-500">All tours currently reporting GPS pings</p>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="bg-gray-50 border-b border-gray-100 text-gray-500">
              <tr>
                <th className="font-semibold p-4">Tour</th>
                <th className="font-semibold p-4 w-48">Progress</th>
                <th className="font-semibold p-4">Last ping</th>
                <th className="font-semibold p-4">State</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {(activeTours || []).map((tour: any) => (
                <tr key={tour.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="p-4">
                    <div className="font-bold text-gray-800 mb-0.5">{tour.name}</div>
                    <div className="text-xs text-gray-400">{tour.id} • {tour.guide}</div>
                  </td>
                  <td className="p-4">
                    <div className="flex justify-between items-center mb-1">
                      <div className="text-xs font-semibold text-gray-500">{tour.progress} stops</div>
                      <div className="text-xs font-bold text-gray-700">{tour.progressPercent}%</div>
                    </div>
                    <div className="bg-gray-100 rounded-full h-1.5 w-full overflow-hidden">
                      <div className={`h-full rounded-full transition-all duration-500 ${tour.status === 'Disruption' ? 'bg-orange-500' : 'bg-green-500'}`} style={{width: `${tour.progressPercent}%`}}></div>
                    </div>
                  </td>
                  <td className="p-4 text-gray-500 font-medium">{tour.lastPing}</td>
                  <td className="p-4">
                    <span className={`inline-flex px-2 py-1 rounded-md text-xs font-bold border ${
                      tour.status === 'Disruption' ? 'bg-orange-50 text-orange-700 border-orange-100' : 'bg-green-50 text-green-700 border-green-100'
                    }`}>{tour.status}</span>
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

export default LiveTourOperations;

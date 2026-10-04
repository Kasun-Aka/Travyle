import { useState, useEffect, useCallback } from 'react';
import { RefreshCw, Zap, Map, AlertTriangle } from 'lucide-react';
import { MapContainer, TileLayer, Marker, Popup, Circle } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

interface RouteStop {
  name: string;
  location: string;
  time: string;
  status: string;
}

interface RouteLogData {
  tourName: string;
  guideName: string;
  hasDisruption: boolean;
  stops: RouteStop[];
}

const API = 'http://localhost:5085/api';

const LiveTourOperations = () => {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [monitoring, setMonitoring] = useState(false);
  const [routeLog, setRouteLog] = useState<RouteLogData | null>(null);
  const [selectedTourId, setSelectedTourId] = useState<string | null>(null);

  const fetchData = useCallback(() => {
    setLoading(true);
    fetch(`${API}/admin/live-operations`)
      .then(r => r.json())
      .then(d => {
        setData(d);
        setLoading(false);
        // Auto-select first tour for route log if none selected
        if (!selectedTourId && d.activeTours?.length > 0) {
          fetchRouteLog(d.activeTours[0].id);
        }
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, [selectedTourId]);

  const fetchRouteLog = (scheduleId: string) => {
    setSelectedTourId(scheduleId);
    fetch(`${API}/admin/route-log/${scheduleId}`)
      .then(r => r.json())
      .then(d => setRouteLog(d))
      .catch(err => console.error('Route log error:', err));
  };

  const handleMonitorOperations = async () => {
    setMonitoring(true);
    try {
      const tourId = data?.activeTours?.[0]?.id || '00000000-0000-0000-0001-000000000001';
      const loc = data?.activeTours?.[0]?.currentLocation;
      const payload = {
        bookingScheduleId: tourId,
        lat: loc?.lat ? parseFloat(loc.lat) : 6.8711,
        lon: loc?.lon ? parseFloat(loc.lon) : 81.0458
      };
      const res = await fetch(`${API}/operations/monitor`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        const result = await res.json();
        alert(`AI Monitor: ${result.message || result.status || 'Scan completed'}`);
        fetchData();
      } else {
        alert('AI Monitor returned an error. Check agent server.');
      }
    } catch (err) {
      console.error(err);
      alert('Error connecting to backend. Make sure the agent server is running.');
    } finally {
      setMonitoring(false);
    }
  };

  const handleReassignGuide = () => {
    const guideId = prompt('Enter Guide ID to assign:');
    const tourId = prompt('Enter Tour ID to reassign:');
    if (!guideId || !tourId) return;
    fetch(`${API}/operations/assignments/${guideId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tourId })
    })
      .then(r => {
        if (r.ok) {
          alert('Guide reassigned successfully');
          fetchData();
        } else {
          alert('Failed to reassign guide');
        }
      })
      .catch(err => {
        console.error(err);
        alert('Error reassigning guide');
      });
  };

  useEffect(() => {
    fetchData();
  }, []);

  if (loading && !data) {
    return <div className="p-10 text-center text-gray-500">Loading Live Operations...</div>;
  }

  const { stats, activeTours, alerts } = data || {};

  // Compute map center from active tours
  const toursWithLocation = (activeTours || []).filter((t: any) => t.currentLocation);
  const mapCenter: [number, number] = toursWithLocation.length > 0
    ? [
      parseFloat(toursWithLocation[0].currentLocation.lat),
      parseFloat(toursWithLocation[0].currentLocation.lon)
    ]
    : [7.2, 80.6]; // Default: center of Sri Lanka

  // Auto-zoom based on spread
  const mapZoom = toursWithLocation.length > 2 ? 8 : toursWithLocation.length > 0 ? 13 : 8;

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
        <div className="flex gap-2 mb-2">
          <button className="inline-flex items-center gap-2 bg-white border border-gray-200 text-gray-700 hover:bg-gray-50 rounded-lg px-4 py-2 text-sm font-semibold transition-colors shadow-sm" onClick={fetchData}>
            <RefreshCw size={14} className={loading ? 'animate-spin' : ''} /> {loading ? 'Refreshing...' : 'Refresh pings'}
          </button>
          <button
            className="inline-flex items-center gap-2 bg-indigo-600 text-white hover:bg-indigo-700 rounded-lg px-4 py-2 text-sm font-semibold transition-colors shadow-sm"
            onClick={handleMonitorOperations}
            disabled={monitoring}
          >
            <Zap size={14} className={monitoring ? 'animate-pulse' : ''} /> {monitoring ? 'Running AI Scan...' : 'Scan with AI Operations Agent'}
          </button>
        </div>
      </div>

      {/* Stats Cards */}
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
          <div className="text-xs text-gray-500 font-semibold uppercase tracking-wider mb-2">GPS pings recorded</div>
          <div className="text-3xl font-black text-gray-800 my-1">{stats?.totalPings || 0}</div>
          <div className="text-blue-400 text-xs font-semibold">across all active tours</div>
        </div>
      </div>

      {/* Live Map */}
      <div className="bg-white shadow-sm border border-gray-100 rounded-xl overflow-hidden mb-6">
        <div className="p-5 border-b border-gray-100 flex justify-between items-center">
          <div className="flex items-center gap-2 mb-1">
            <Map className="text-gray-500" size={20} />
            <h2 className="text-lg font-bold text-gray-800">Live map dashboard</h2>
          </div>
          <p className="text-gray-400 text-sm">Real-time GPS tracking with weather overlay</p>
        </div>
        <div className="relative h-[400px] z-0">
          <div className="absolute top-4 left-4 bg-white/90 backdrop-blur text-xs font-bold px-3 py-1.5 rounded-full shadow-sm text-gray-700 z-[1000]">
            Live map — {stats?.totalPings || 0} pings
          </div>

          <MapContainer center={mapCenter} zoom={mapZoom} scrollWheelZoom={false} className="h-full w-full">
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />

            {/* Dynamic markers from active tours */}
            {(activeTours || []).map((tour: any) => {
              if (!tour.currentLocation) return null;
              const lat = parseFloat(tour.currentLocation.lat);
              const lon = parseFloat(tour.currentLocation.lon);
              const isDelayed = tour.status === 'Delayed';
              const shortName = tour.name?.split(' ').slice(0, 3).join(' ') || tour.id.slice(0, 8);

              return (
                <div key={tour.id}>
                  {/* Weather/status zone circle */}
                  <Circle
                    center={[lat, lon]}
                    pathOptions={{
                      fillColor: isDelayed ? 'orange' : 'green',
                      fillOpacity: 0.15,
                      color: 'transparent'
                    }}
                    radius={800}
                  />

                  {/* Tour marker */}
                  <Marker
                    position={[lat, lon]}
                    icon={L.divIcon({
                      className: 'custom-leaflet-marker',
                      html: `<div class="${isDelayed ? 'bg-white' : 'bg-blue-600 text-white'} px-3 py-1.5 rounded-full shadow-md text-xs font-bold flex items-center gap-1.5 whitespace-nowrap ${isDelayed ? 'text-gray-700' : ''}">
                        ${isDelayed ? '<span class="w-2 h-2 rounded-full bg-orange-400"></span>' : ''}
                        ${shortName}
                      </div>`,
                      iconSize: [140, 30],
                      iconAnchor: [70, 15]
                    })}
                    eventHandlers={{
                      click: () => fetchRouteLog(tour.id)
                    }}
                  >
                    <Popup>
                      <b>{tour.name}</b><br />
                      Guide: {tour.guide}<br />
                      Status: {tour.status}<br />
                      Progress: {tour.progress}
                    </Popup>
                  </Marker>
                </div>
              );
            })}
          </MapContainer>
        </div>
      </div>

      {/* Route Log + Agent Panel */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
        <div className="lg:col-span-1">
          <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 h-full">
            <div className="flex justify-between items-start mb-6">
              <div>
                <h2 className="text-lg font-bold text-gray-800">Route log</h2>
                <p className="text-sm text-gray-500">
                  {routeLog ? `${routeLog.tourName} • ${routeLog.guideName}` : 'Select a tour'}
                </p>
              </div>
              {routeLog?.hasDisruption && (
                <span className="bg-orange-50 text-orange-700 px-2.5 py-1 rounded-md text-xs font-bold border border-orange-100">Disruption</span>
              )}
            </div>

            {routeLog ? (
              <div className="relative pl-4 border-l-2 border-gray-100 space-y-6">
                {routeLog.stops.map((stop, i) => {
                  const isCompleted = stop.status === 'Completed';
                  const isInProgress = stop.status === 'InProgress';

                  return (
                    <div className="relative" key={i}>
                      <div className={`absolute -left-[21px] top-1 w-3 h-3 rounded-full border-2 border-white ${isCompleted ? 'bg-gray-300' :
                        isInProgress ? 'bg-blue-500 shadow-[0_0_0_4px_rgba(59,130,246,0.2)]' :
                          'bg-white border-gray-200'
                        }`}></div>
                      <div>
                        <div className={`text-sm font-semibold ${isCompleted ? 'text-gray-400 line-through' :
                          isInProgress ? 'font-bold text-gray-800' :
                            'text-gray-500'
                          }`}>{stop.name}</div>
                        <div className={`text-xs mt-0.5 ${isInProgress ? 'font-medium text-blue-600' : 'text-gray-400'
                          }`}>
                          {stop.time} — {stop.location}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : (
              <div className="text-sm text-gray-400 text-center py-8">Click a tour on the map or in the table below to view its route log.</div>
            )}
          </div>
        </div>

        <div className="lg:col-span-2">
          {alerts && alerts.length > 0 && (
            <div className="bg-gradient-to-br from-indigo-50/80 to-blue-50/50 p-6 rounded-xl border border-indigo-100/50 shadow-sm flex gap-5 h-full items-start">
              <div className="bg-white p-3 rounded-xl shadow-sm border border-indigo-50 flex-shrink-0">
                <Zap className="text-indigo-600" size={24} />
              </div>
              <div className="flex flex-col justify-between h-full w-full">
                <div>
                  <div className="text-indigo-900 font-bold mb-2 text-sm uppercase tracking-wide">Operations Agent</div>
                  {/* <p className="text-indigo-800/80 text-sm font-semibold mb-2">
                    {alerts[0].description}
                  </p> */}
                  <p className="text-indigo-800/60 text-xs leading-relaxed mb-4">
                    {alerts[alerts.length - 1].proposal}
                  </p>
                </div>
                {/*
                <div className="flex gap-3 mt-auto flex-wrap">
                  <button className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-semibold shadow-sm hover:bg-indigo-700 transition-colors">Approve re-route</button>
                  <button onClick={handleReassignGuide} className="bg-white text-indigo-600 border border-indigo-200 px-4 py-2 rounded-lg text-sm font-semibold hover:bg-indigo-50 transition-colors shadow-sm">Reassign guide instead</button>
                  <button className="text-gray-500 hover:text-gray-700 px-4 py-2 text-sm font-medium transition-colors">Dismiss</button>
                </div>
                */}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Disruption Alerts Table */}
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
              {(alerts || []).length === 0 && (
                <tr><td colSpan={4} className="p-8 text-center text-gray-400">No active disruption alerts</td></tr>
              )}
              {(alerts || []).map((alert: any) => (
                <tr key={alert.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="p-4">
                    <div className="font-bold text-gray-800 mb-0.5">{alert.title}</div>
                    <div className="text-xs text-gray-400">{alert.source} • {alert.time}</div>
                  </td>
                  <td className="p-4">
                    <span className={`inline-flex px-2 py-1 rounded-md text-xs font-bold border ${alert.severity === 'High' ? 'bg-red-50 text-red-700 border-red-100' :
                      alert.severity === 'Medium' ? 'bg-orange-50 text-orange-700 border-orange-100' :
                        'bg-gray-50 text-gray-700 border-gray-200'
                      }`}>{alert.severity}</span>
                  </td>
                  <td className="p-4 text-gray-600 max-w-xs">
                    <div className="text-xs">{alert.proposal}</div>
                  </td>
                  {/* <td className="p-4 text-right">
                    {alert.status === 'Pending' ? (
                      <div className="flex justify-end gap-2">
                        <button className="text-gray-500 hover:text-gray-700 font-medium px-3 py-1.5 transition-colors text-xs">Dismiss</button>
                        <button className="bg-indigo-50 text-indigo-600 hover:bg-indigo-100 font-semibold px-3 py-1.5 rounded-md transition-colors text-xs border border-indigo-100">Approve re-route</button>
                      </div>
                    ) : (
                      <span className="text-green-700 font-bold bg-green-50 border border-green-100 px-3 py-1.5 rounded-md text-xs">Approved</span>
                    )}
                  </td> */}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Active Tour Progress Table */}
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
              {(activeTours || []).length === 0 && (
                <tr><td colSpan={4} className="p-8 text-center text-gray-400">No active tours</td></tr>
              )}
              {(activeTours || []).map((tour: any) => (
                <tr
                  key={tour.id}
                  className={`hover:bg-gray-50/50 transition-colors cursor-pointer ${selectedTourId === tour.id ? 'bg-indigo-50/50' : ''}`}
                  onClick={() => fetchRouteLog(tour.id)}
                >
                  <td className="p-4">
                    <div className="font-bold text-gray-800 mb-0.5">{tour.name}</div>
                    <div className="text-xs text-gray-400">{tour.guide} • {tour.location}</div>
                  </td>
                  <td className="p-4">
                    <div className="flex justify-between items-center mb-1">
                      <div className="text-xs font-semibold text-gray-500">{tour.progress} stops</div>
                      <div className="text-xs font-bold text-gray-700">{tour.progressPercent}%</div>
                    </div>
                    <div className="bg-gray-100 rounded-full h-1.5 w-full overflow-hidden">
                      <div className={`h-full rounded-full transition-all duration-500 ${tour.status === 'Delayed' ? 'bg-orange-500' : 'bg-green-500'}`} style={{ width: `${tour.progressPercent}%` }}></div>
                    </div>
                  </td>
                  <td className="p-4 text-gray-500 font-medium">{tour.lastPing}</td>
                  <td className="p-4">
                    <span className={`inline-flex px-2 py-1 rounded-md text-xs font-bold border ${tour.status === 'Delayed' ? 'bg-orange-50 text-orange-700 border-orange-100' : 'bg-green-50 text-green-700 border-green-100'
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

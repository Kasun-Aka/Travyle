
import { RefreshCw, Zap } from 'lucide-react';
import './LiveTourOperations.css';

const LiveTourOperations = () => {
  return (
    <div className="page-container">
      <div className="page-header">
        <div className="page-breadcrumb">
          <span className="dot"></span>
          Operations & staffing
        </div>
        <h1 className="page-title">Live tour operations</h1>
        <p className="page-subtitle">
          Track every tour in progress against its planned route, triage weather and traffic disruptions, and approve agent-proposed stop re-ordering.
        </p>
        <button className="btn-outline refresh-btn">
          <RefreshCw size={14} /> Refresh pings
        </button>
        <div className="api-paths">
          <div className="api-path"><span className="api-method post">POST</span>/api/operations/route-logs</div>
          <div className="api-path"><span className="api-method get">GET</span>/api/operations/disruption-alerts</div>
          <div className="api-path"><span className="api-method post">POST</span>/api/operations/reorder-route-optimization</div>
        </div>
      </div>

      <div className="stats-grid">
        <div className="stat-card">
          <div className="stat-title">Tours in progress</div>
          <div className="stat-value">4</div>
          <div className="stat-desc">2 flagged</div>
        </div>
        <div className="stat-card">
          <div className="stat-title">Guides on duty</div>
          <div className="stat-value">4</div>
          <div className="stat-desc">2 available for reassignment</div>
        </div>
        <div className="stat-card">
          <div className="stat-title">Active alerts</div>
          <div className="stat-value">2</div>
          <div className="stat-desc">OpenWeatherMap + Traffic</div>
        </div>
        <div className="stat-card">
          <div className="stat-title">Avg. schedule drift</div>
          <div className="stat-value error">+11 min</div>
          <div className="stat-desc error-desc">vs yesterday</div>
        </div>
      </div>

      <div className="card map-card">
        <div className="map-header">
          <h2 className="map-title">Live map dashboard</h2>
          <p className="map-subtitle">Select a marker to inspect the tour's route log</p>
        </div>
        <div className="map-placeholder">
          <div className="map-badge">Live map - 1276 pings</div>
          <div className="map-shape green-shape"></div>
          <div className="map-shape orange-shape"></div>
          <div className="map-shape purple-shape"></div>
          
          <div className="map-marker" style={{ top: '30%', left: '60%' }}>
            <span className="marker-dot warning"></span>
            TOUR-5502
          </div>
          <div className="map-marker active" style={{ top: '50%', left: '70%' }}>
            TOUR-5510
          </div>
          <div className="map-marker" style={{ top: '70%', left: '40%' }}>
            TOUR-5505
          </div>
          <div className="map-marker" style={{ top: '80%', left: '80%' }}>
            TOUR-5508
          </div>
          
          <div className="map-coord">Map: 12.345, 80.678 (WGS84)</div>
        </div>
      </div>

      <div className="route-log-section">
        <div className="card route-log-card">
          <div className="route-log-header">
            <div>
              <h2 className="route-title">Route log</h2>
              <p className="route-subtitle">TOUR-5510 • Ravi Perera</p>
            </div>
            <span className="badge badge-warning">Disruption</span>
          </div>
          
          <div className="timeline">
            <div className="timeline-item completed">
              <div className="timeline-dot"></div>
              <div className="timeline-content">
                <div className="timeline-title">Hotel pickup — Ella town</div>
                <div className="timeline-time">08:00</div>
              </div>
            </div>
            <div className="timeline-item completed">
              <div className="timeline-dot"></div>
              <div className="timeline-content">
                <div className="timeline-title">Nine Arches Bridge</div>
                <div className="timeline-time">09:30</div>
              </div>
            </div>
            <div className="timeline-item completed">
              <div className="timeline-dot"></div>
              <div className="timeline-content">
                <div className="timeline-title">Tea factory tour</div>
                <div className="timeline-time">10:30</div>
              </div>
            </div>
            <div className="timeline-item active">
              <div className="timeline-dot pulse"></div>
              <div className="timeline-content">
                <div className="timeline-title">Little Adam's Peak</div>
                <div className="timeline-time">13:00 - 38 mins in rain</div>
              </div>
            </div>
            <div className="timeline-item pending">
              <div className="timeline-dot"></div>
              <div className="timeline-content">
                <div className="timeline-title">Ravana Falls</div>
                <div className="timeline-time">14:30</div>
              </div>
            </div>
            <div className="timeline-item pending">
              <div className="timeline-dot"></div>
              <div className="timeline-content">
                <div className="timeline-title">Drop-off — Ella station</div>
                <div className="timeline-time">16:00</div>
              </div>
            </div>
          </div>
        </div>
        
        <div className="agent-alert-box vertical">
          <div className="agent-icon-bg">
            <Zap className="agent-icon" size={20} />
          </div>
          <div className="agent-content">
            <div className="agent-title">Operations Agent</div>
            <p className="agent-text">
              TOUR-5510 will hit the Nine Arches viewpoint inside a heavy-rain window. Re-ordering stops 4 and 6 keeps all six stops, adds 18 minutes, and moves the outdoor segment to 14:40 when the cell has cleared.
            </p>
            <div className="agent-actions">
              <button className="btn-primary">Approve re-route</button>
              <button className="btn-outline">Reassign guide instead</button>
              <button className="btn-text">Dismiss</button>
            </div>
          </div>
        </div>
      </div>

      <div className="card table-card">
        <div className="card-header">
          <h2 className="card-title">Disruption alerts</h2>
          <p className="card-subtitle">Weather and traffic hazards with proposed route recalculations</p>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>Alert</th>
              <th>Severity</th>
              <th>Agent proposed TSP re-order</th>
              <th className="text-right">Decision</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <div className="alert-title">Heavy rain warning around Nine Arches Bridge until 15:00</div>
                <div className="alert-meta">ALT-841 • TOUR-5510<br/>OpenWeatherMap • 5 min ago</div>
              </td>
              <td><span className="badge badge-danger">High</span></td>
              <td>Reorder stops 4 & 6, move viewpoint to late afternoon (+18 min total)</td>
              <td className="text-right">
                <button className="btn-text">Dismiss</button>
                <button className="btn-primary ml-2">Approve re-route</button>
              </td>
            </tr>
            <tr>
              <td>
                <div className="alert-title">Road closure on A9 near Dambulla junction</div>
                <div className="alert-meta">ALT-912 • TOUR-5502<br/>Traffic • 22 min ago</div>
              </td>
              <td><span className="badge badge-warning">Medium</span></td>
              <td>Detour via B152, swap lunch stop earlier (+8 min total)</td>
              <td className="text-right">
                <button className="btn-text">Dismiss</button>
                <button className="btn-primary ml-2">Approve re-route</button>
              </td>
            </tr>
            <tr>
              <td>
                <div className="alert-title">High UV index, afternoon walking segment flagged</div>
                <div className="alert-meta">ALT-908 • TOUR-5508<br/>OpenWeatherMap • 1 hr ago</div>
              </td>
              <td><span className="badge badge-neutral">Low</span></td>
              <td>Add 20 min shade break after stop 4</td>
              <td className="text-right"><span className="text-success font-semibold">Approved</span></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div className="card table-card mt-6">
        <div className="card-header">
          <h2 className="card-title">Active tour progress</h2>
          <p className="card-subtitle">All tours currently reporting GPS pings</p>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>Tour</th>
              <th>Progress</th>
              <th>Last ping</th>
              <th>State</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <div className="tour-name">Ella Highlands & Tea Trails</div>
                <div className="tour-meta">TOUR-5510 • Ravi Perera</div>
              </td>
              <td>
                <div className="progress-text">3/6 stops</div>
                <div className="progress-bar-container">
                  <div className="progress-bar warning" style={{width: '50%'}}></div>
                </div>
              </td>
              <td>40 s ago</td>
              <td><span className="badge badge-warning">Disruption</span></td>
            </tr>
            <tr>
              <td>
                <div className="tour-name">Galle Fort Coastal Loop</div>
                <div className="tour-meta">TOUR-5508 • Nadia Silva</div>
              </td>
              <td>
                <div className="progress-text">5/7 stops</div>
                <div className="progress-bar-container">
                  <div className="progress-bar primary" style={{width: '71%'}}></div>
                </div>
              </td>
              <td>1 min ago</td>
              <td><span className="badge badge-success">On schedule</span></td>
            </tr>
            <tr>
              <td>
                <div className="tour-name">Yala Wildlife Expedition</div>
                <div className="tour-meta">TOUR-5505 • Ishara Bandara</div>
              </td>
              <td>
                <div className="progress-text">1/4 stops</div>
                <div className="progress-bar-container">
                  <div className="progress-bar primary" style={{width: '25%'}}></div>
                </div>
              </td>
              <td>6 min ago</td>
              <td><span className="badge badge-success">On schedule</span></td>
            </tr>
            <tr>
              <td>
                <div className="tour-name">Sigiriya & Ancient Cities</div>
                <div className="tour-meta">TOUR-5502 • Tharindu Jay</div>
              </td>
              <td>
                <div className="progress-text">4/8 stops</div>
                <div className="progress-bar-container">
                  <div className="progress-bar warning" style={{width: '50%'}}></div>
                </div>
              </td>
              <td>12 min ago</td>
              <td><span className="badge badge-warning">Disruption</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default LiveTourOperations;

import React from 'react';
import { ChevronDown, Zap } from 'lucide-react';
import './GuideAssignmentMatrix.css';

const GuideAssignmentMatrix = () => {
  return (
    <div className="page-container">
      <div className="page-header">
        <div className="page-breadcrumb">
          <span className="dot"></span>
          Operations & staffing
        </div>
        <h1 className="page-title">Guide assignment matrix</h1>
        <p className="page-subtitle">
          See the whole week at once, spot double-bookings and idle capacity, and reassign guides or drivers to specific tours and slots.
        </p>
        <div className="api-paths">
          <div className="api-path"><span className="api-method get">GET</span>/api/operations/guides/available</div>
          <div className="api-path"><span className="api-method put">PUT</span>/api/operations/assignments/&#123;id&#125;</div>
          <div className="api-path"><span className="api-method post">POST</span>/api/operations/activities</div>
        </div>
      </div>

      <div className="card matrix-card">
        <div className="matrix-header">
          <div className="matrix-week">Week of Sep 08 - Sep 14</div>
          <div className="matrix-subtitle">Click any cell to assign, reassign, or clear a guide</div>
          <div className="matrix-badge">6 guides - 7 days</div>
        </div>
        
        <div className="matrix-table-container">
          <table className="matrix-table">
            <thead>
              <tr>
                <th className="th-guide">Guide</th>
                <th>Mon 08</th>
                <th>Tue 09</th>
                <th>Wed 10</th>
                <th>Thu 11</th>
                <th>Fri 12</th>
                <th>Sat 13</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td className="td-guide">
                  <div className="guide-name">Ravi Perera</div>
                  <div className="guide-meta">Ella • EN/SI/DE</div>
                </td>
                <td><div className="slot-badge tour">TOUR-5518</div></td>
                <td><div className="slot-badge tour">TOUR-5510</div></td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge slot">SLOT-8841</div></td>
                <td><div className="slot-badge slot">SLOT-8841</div></td>
                <td className="empty-slot">-</td>
              </tr>
              <tr>
                <td className="td-guide">
                  <div className="guide-name">Nadia Silva</div>
                  <div className="guide-meta">Galle • EN/SI/FR</div>
                </td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge tour">TOUR-5508</div></td>
                <td><div className="slot-badge tour">TOUR-5508</div></td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge slot">SLOT-8842</div></td>
                <td><div className="slot-badge slot">SLOT-8842</div></td>
              </tr>
              <tr>
                <td className="td-guide">
                  <div className="guide-name">Ishara Bandara</div>
                  <div className="guide-meta">Tissamaharama • EN/SI</div>
                </td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge tour">TOUR-5505</div></td>
                <td><div className="slot-badge tour">TOUR-5505</div></td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
              </tr>
              <tr>
                <td className="td-guide">
                  <div className="guide-name">Tharindu Jay</div>
                  <div className="guide-meta">Dambulla • EN/TA</div>
                </td>
                <td><div className="slot-badge tour">TOUR-5502</div></td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge tour">TOUR-5502</div></td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge slot">SLOT-8851</div></td>
              </tr>
              <tr>
                <td className="td-guide">
                  <div className="guide-name">Kavindi Rathnayake</div>
                  <div className="guide-meta">Kandy • EN/SI/JA</div>
                </td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td><div className="slot-badge slot">SLOT-8841</div></td>
                <td className="empty-slot">-</td>
              </tr>
              <tr>
                <td className="td-guide">
                  <div className="guide-name">Sanjay Mendis</div>
                  <div className="guide-meta">Nuwara Eliya • EN/RU</div>
                </td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
                <td className="empty-slot">-</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div className="agent-alert-box">
        <div className="agent-icon-bg">
          <Zap className="agent-icon" size={20} />
        </div>
        <div className="agent-content">
          <div className="agent-title">Operations Agent</div>
          <p className="agent-text">
            SLOT-8845 (Yala, Sep 14) still has no guide and Ishara Bandara is already on TOUR-5505 that morning. Kavindi Rathnayake is verified, EN/SI/JA, and has one tour this week — the lightest load available.
          </p>
          <div className="agent-actions">
            <button className="btn-primary">Assign Kavindi</button>
            <button className="btn-text">Leave unassigned</button>
          </div>
        </div>
      </div>

      <div className="card roster-card">
        <div className="roster-header">
          <div className="roster-title-area">
            <h2 className="roster-title">Guide roster</h2>
            <div className="roster-subtitle">6 of 6 guides</div>
          </div>
          <div className="roster-filters">
            <input type="text" className="filter-input" placeholder="Search guide, ID, or location" />
            <div className="filter-select">
              All availability <ChevronDown size={16} />
            </div>
            <div className="filter-select">
              Any language <ChevronDown size={16} />
            </div>
          </div>
        </div>

        <table className="roster-table">
          <thead>
            <tr>
              <th>Guide</th>
              <th>Languages</th>
              <th>Verification</th>
              <th>Load this week</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <div className="roster-guide-name">Ravi Perera</div>
                <div className="roster-guide-meta">GD-201 • Ella</div>
              </td>
              <td>
                <div className="lang-badges">
                  <span className="lang-badge">EN</span>
                  <span className="lang-badge">SI</span>
                  <span className="lang-badge">DE</span>
                </div>
              </td>
              <td><span className="badge badge-success">Verified</span></td>
              <td>5 tours</td>
              <td><span className="badge badge-success">On tour</span></td>
            </tr>
            <tr>
              <td>
                <div className="roster-guide-name">Nadia Silva</div>
                <div className="roster-guide-meta">GD-204 • Galle</div>
              </td>
              <td>
                <div className="lang-badges">
                  <span className="lang-badge">EN</span>
                  <span className="lang-badge">SI</span>
                  <span className="lang-badge">FR</span>
                </div>
              </td>
              <td><span className="badge badge-success">Verified</span></td>
              <td>4 tours</td>
              <td><span className="badge badge-success">On tour</span></td>
            </tr>
            <tr>
              <td>
                <div className="roster-guide-name">Ishara Bandara</div>
                <div className="roster-guide-meta">GD-209 • Tissamaharama</div>
              </td>
              <td>
                <div className="lang-badges">
                  <span className="lang-badge">EN</span>
                  <span className="lang-badge">SI</span>
                </div>
              </td>
              <td><span className="badge badge-success">Verified</span></td>
              <td>2 tours</td>
              <td><span className="badge badge-info">Available</span></td>
            </tr>
            <tr>
              <td>
                <div className="roster-guide-name">Tharindu Jay</div>
                <div className="roster-guide-meta">GD-212 • Dambulla</div>
              </td>
              <td>
                <div className="lang-badges">
                  <span className="lang-badge">EN</span>
                  <span className="lang-badge">TA</span>
                </div>
              </td>
              <td><span className="badge badge-warning">Pending KYC</span></td>
              <td>3 tours</td>
              <td><span className="badge badge-success">On tour</span></td>
            </tr>
            <tr>
              <td>
                <div className="roster-guide-name">Kavindi Rathnayake</div>
                <div className="roster-guide-meta">GD-217 • Kandy</div>
              </td>
              <td>
                <div className="lang-badges">
                  <span className="lang-badge">EN</span>
                  <span className="lang-badge">SI</span>
                  <span className="lang-badge">JA</span>
                </div>
              </td>
              <td><span className="badge badge-success">Verified</span></td>
              <td>1 tours</td>
              <td><span className="badge badge-info">Available</span></td>
            </tr>
            <tr>
              <td>
                <div className="roster-guide-name">Sanjay Mendis</div>
                <div className="roster-guide-meta">GD-221 • Nuwara Eliya</div>
              </td>
              <td>
                <div className="lang-badges">
                  <span className="lang-badge">EN</span>
                  <span className="lang-badge">RU</span>
                </div>
              </td>
              <td><span className="badge badge-success">Verified</span></td>
              <td>0 tours</td>
              <td><span className="badge badge-neutral">Off duty</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default GuideAssignmentMatrix;

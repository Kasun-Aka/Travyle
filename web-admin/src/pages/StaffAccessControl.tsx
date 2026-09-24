import React from 'react';
import { UserPlus } from 'lucide-react';
import './StaffAccessControl.css';

const StaffAccessControl = () => {
  return (
    <div className="page-container">
      <div className="page-header">
        <div className="page-breadcrumb">
          <span className="dot"></span>
          System
        </div>
        <h1 className="page-title">Staff & access control</h1>
        <p className="page-subtitle">
          Provision Guide and Operator accounts against Firebase, manage role assignments, and confirm the shared foundation services are reachable.
        </p>
        <button className="btn-primary provision-btn">
          <UserPlus size={16} className="mr-2" /> Provision staff account
        </button>
        <div className="api-paths">
          <div className="api-path"><span className="api-method post">POST</span>/api/admin/staff</div>
          <div className="api-path"><span className="api-method post">POST</span>/api/auth/register-profile</div>
          <div className="api-path"><span className="api-method get">GET</span>/api/users/&#123;id&#125;/profile</div>
        </div>
      </div>

      <div className="card table-card mb-6">
        <div className="card-header">
          <h2 className="card-title">Staff accounts</h2>
          <p className="card-subtitle">5 provisioned Guide and Operator accounts</p>
        </div>
        <table className="data-table">
          <thead>
            <tr>
              <th>Staff member</th>
              <th>Role</th>
              <th>Provisioned</th>
              <th>Status</th>
              <th className="text-right">Access</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <td>
                <div className="staff-name">Ravi Perera</div>
                <div className="staff-meta">US-1001 • ravi.perera@travyle.io</div>
              </td>
              <td><span className="role-badge guide">Guide</span></td>
              <td>Jan 14, 2026</td>
              <td><span className="badge badge-success">Active</span></td>
              <td className="text-right">
                <button className="btn-text mr-4">Edit role</button>
                <button className="btn-outline">Suspend</button>
              </td>
            </tr>
            <tr>
              <td>
                <div className="staff-name">Nadia Silva</div>
                <div className="staff-meta">US-1004 • nadia.silva@travyle.io</div>
              </td>
              <td><span className="role-badge guide">Guide</span></td>
              <td>Feb 02, 2026</td>
              <td><span className="badge badge-success">Active</span></td>
              <td className="text-right">
                <button className="btn-text mr-4">Edit role</button>
                <button className="btn-outline">Suspend</button>
              </td>
            </tr>
            <tr>
              <td>
                <div className="staff-name">Chamath Weera</div>
                <div className="staff-meta">US-1011 • chamath.weera@travyle.io</div>
              </td>
              <td><span className="role-badge operator">Operator</span></td>
              <td>Mar 21, 2026</td>
              <td><span className="badge badge-success">Active</span></td>
              <td className="text-right">
                <button className="btn-text mr-4">Edit role</button>
                <button className="btn-outline">Suspend</button>
              </td>
            </tr>
            <tr>
              <td>
                <div className="staff-name">Kavindi Rathnayake</div>
                <div className="staff-meta">US-1019 • kavindi.r@travyle.io</div>
              </td>
              <td><span className="role-badge guide">Guide</span></td>
              <td>Aug 30, 2026</td>
              <td><span className="badge badge-warning">Invited</span></td>
              <td className="text-right">
                <button className="btn-text mr-4">Edit role</button>
                <button className="btn-outline">Suspend</button>
              </td>
            </tr>
            <tr>
              <td>
                <div className="staff-name">Sanjay Mendis</div>
                <div className="staff-meta">US-1024 • sanjay.mendis@travyle.io</div>
              </td>
              <td><span className="role-badge guide">Guide</span></td>
              <td>Jun 11, 2026</td>
              <td><span className="badge badge-neutral">Suspended</span></td>
              <td className="text-right">
                <button className="btn-text mr-4">Edit role</button>
                <button className="btn-outline">Reinstate</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div className="card health-card">
        <div className="card-header border-none">
          <h2 className="card-title">Shared foundation health</h2>
          <p className="card-subtitle">GET /api/health and third-party integration status</p>
        </div>
        
        <div className="health-grid">
          <div className="health-item">
            <div className="health-item-header">
              <span className="health-name">Web API (ASP.NET Core)</span>
              <span className="health-status ok">OK</span>
            </div>
            <div className="health-desc">GET /api/health • 200 OK</div>
          </div>
          <div className="health-item">
            <div className="health-item-header">
              <span className="health-name">PostgreSQL (Supabase)</span>
              <span className="health-status ok">OK</span>
            </div>
            <div className="health-desc">Latency 42 ms</div>
          </div>
          <div className="health-item">
            <div className="health-item-header">
              <span className="health-name">Agentic AI Orchestrator</span>
              <span className="health-status ok">OK</span>
            </div>
            <div className="health-desc">4 agents online</div>
          </div>
          <div className="health-item">
            <div className="health-item-header">
              <span className="health-name">Stripe Sandbox</span>
              <span className="health-status ok">OK</span>
            </div>
            <div className="health-desc">Escrow webhooks healthy</div>
          </div>
          <div className="health-item">
            <div className="health-item-header">
              <span className="health-name">OpenWeatherMap</span>
              <span className="health-status watch">Watch</span>
            </div>
            <div className="health-desc">Rate limit 78% used</div>
          </div>
          <div className="health-item">
            <div className="health-item-header">
              <span className="health-name">SendGrid / Twilio</span>
              <span className="health-status ok">OK</span>
            </div>
            <div className="health-desc">Queue 3 pending</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StaffAccessControl;

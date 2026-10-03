import { useState, useEffect } from 'react';
import { UserPlus, Users } from 'lucide-react';

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

  const handleEditRole = (staffMember: any) => {
    const newRole = prompt(`Enter new role for ${staffMember.fullName} (current: ${staffMember.role}):`, staffMember.role);
    if (!newRole || newRole === staffMember.role) return;

    fetch(`http://localhost:5085/api/admin/staff/${staffMember.id}/role`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ role: newRole })
    })
      .then(r => {
        if (r.ok) {
          alert('Role updated successfully');
          setLoading(true);
          fetch('http://localhost:5085/api/admin/staff')
            .then(res => res.json())
            .then(data => { setStaff(data); setLoading(false); });
        } else {
          alert('Failed to update role');
        }
      })
      .catch(err => {
        console.error(err);
        alert('Error updating role');
      });
  };

  const handleSuspend = (staffMember: any) => {
    if (!confirm(`Are you sure you want to suspend ${staffMember.fullName}?`)) return;

    fetch(`http://localhost:5085/api/admin/staff/${staffMember.id}/suspend`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' }
    })
      .then(r => {
        if (r.ok) {
          alert('Staff suspended successfully');
        } else {
          alert('Failed to suspend staff');
        }
      })
      .catch(err => {
        console.error(err);
        alert('Error suspending staff');
      });
  };

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
                      <button onClick={() => handleEditRole(s)} className="text-sm font-medium text-gray-500 hover:text-gray-900 transition-colors mr-4">Edit role</button>
                      <button onClick={() => handleSuspend(s)} className="text-sm font-medium border border-gray-200 text-gray-600 hover:bg-gray-50 px-3 py-1.5 rounded-lg transition-colors">Suspend</button>
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
    </div>
  );
};

export default StaffAccessControl;

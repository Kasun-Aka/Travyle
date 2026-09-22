import { useState, useEffect, useCallback } from 'react';
import Layout from '../components/Layout';
import DestinationFormModal from '../components/DestinationFormModal';
import { Plus, MapPin, Sparkles, Search, Edit2, Trash2, Loader2, AlertCircle } from 'lucide-react';
import { destinationsApi, type Destination } from '../api/destinations';

// ────────────────────────────────────────────────────────────
// Delete Confirmation Dialog
// ────────────────────────────────────────────────────────────
function DeleteConfirmDialog({
  destination,
  onCancel,
  onConfirm,
}: {
  destination: Destination;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const [loading, setLoading] = useState(false);

  const handleConfirm = async () => {
    setLoading(true);
    await onConfirm();
    setLoading(false);
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm p-6 space-y-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 bg-red-100 rounded-full flex items-center justify-center shrink-0">
            <AlertCircle size={20} className="text-red-600" />
          </div>
          <div>
            <h3 className="font-bold text-gray-900">Delete destination?</h3>
            <p className="text-sm text-gray-500 mt-0.5">This action cannot be undone.</p>
          </div>
        </div>
        <p className="text-sm text-gray-700 bg-gray-50 rounded-lg px-4 py-3">
          You are about to permanently delete{' '}
          <span className="font-semibold">"{destination.name}"</span> from the catalog.
        </p>
        <div className="flex gap-3 justify-end">
          <button
            onClick={onCancel}
            disabled={loading}
            className="px-4 py-2 text-sm font-medium text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={loading}
            className="px-4 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg transition-colors flex items-center gap-2 disabled:opacity-50"
          >
            {loading && <Loader2 size={14} className="animate-spin" />}
            Delete
          </button>
        </div>
      </div>
    </div>
  );
}

// ────────────────────────────────────────────────────────────
// Main Page
// ────────────────────────────────────────────────────────────
export default function MasterTourPackages() {
  const [destinations, setDestinations] = useState<Destination[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [fetchError, setFetchError] = useState<string | null>(null);

  // Search / filter state
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');

  // Modal state
  const [showForm, setShowForm] = useState(false);
  const [editTarget, setEditTarget] = useState<Destination | null>(null);

  // Delete confirmation state
  const [deleteTarget, setDeleteTarget] = useState<Destination | null>(null);

  // Detail panel state
  const [selected, setSelected] = useState<Destination | null>(null);

  // ── Fetch destinations ──────────────────────────────────
  const fetchDestinations = useCallback(async () => {
    setLoading(true);
    setFetchError(null);
    try {
      const response = await destinationsApi.list({ search: search || undefined, pageSize: 50 });
      setDestinations(response.data.items);
      setTotalCount(response.data.totalCount);
      // Auto-select first item if available
      if (response.data.items.length > 0 && !selected) {
        setSelected(response.data.items[0]);
      }
    } catch {
      setFetchError('Could not connect to the backend. Make sure the API server is running on port 5285.');
    } finally {
      setLoading(false);
    }
  }, [search]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    fetchDestinations();
  }, [fetchDestinations]);

  // ── Search handler (debounced on Enter / blur) ──────────
  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSearch(searchInput);
  };

  // ── Create / Edit handlers ──────────────────────────────
  const openCreate = () => {
    setEditTarget(null);
    setShowForm(true);
  };

  const openEdit = (dest: Destination) => {
    setEditTarget(dest);
    setShowForm(true);
  };

  const handleFormSaved = () => {
    fetchDestinations();
  };

  // ── Delete handler ──────────────────────────────────────
  const handleDeleteConfirm = async () => {
    if (!deleteTarget) return;
    try {
      await destinationsApi.delete(deleteTarget.id);
      if (selected?.id === deleteTarget.id) setSelected(null);
      setDeleteTarget(null);
      fetchDestinations();
    } catch {
      alert('Failed to delete the destination. Please try again.');
    }
  };

  return (
    <Layout>
      {/* ── Modals ──────────────────────────────────────────── */}
      {showForm && (
        <DestinationFormModal
          destination={editTarget}
          onClose={() => setShowForm(false)}
          onSaved={handleFormSaved}
        />
      )}
      {deleteTarget && (
        <DeleteConfirmDialog
          destination={deleteTarget}
          onCancel={() => setDeleteTarget(null)}
          onConfirm={handleDeleteConfirm}
        />
      )}

      <div className="max-w-6xl mx-auto space-y-8">

        {/* ── Page Header ───────────────────────────────────── */}
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-brand-600 uppercase tracking-wider mb-2">
            <span className="w-1.5 h-1.5 rounded-full bg-brand-600"></span>
            Catalog
          </div>
          <h1 className="text-4xl font-bold text-gray-900 mb-4 tracking-tight">Master tour packages</h1>
          <p className="text-gray-500 max-w-3xl text-lg leading-relaxed mb-6">
            Create and maintain the destination catalog travelers browse in the mobile app, including geocoded coordinates, preference tags, and capacity.
          </p>
          <button
            onClick={openCreate}
            className="bg-brand-600 hover:bg-brand-700 text-white px-6 py-2.5 rounded-lg font-medium flex items-center gap-2 transition-colors"
          >
            <Plus size={20} />
            Create package
          </button>
        </div>

        {/* ── API Endpoint Reference Chips ──────────────────── */}
        <div className="flex flex-wrap gap-4 text-xs font-mono">
          {[
            { method: 'GET', path: '/api/destinations' },
            { method: 'GET', path: '/api/destinations/{id}' },
            { method: 'POST', path: '/api/destinations' },
            { method: 'PUT', path: '/api/destinations/{id}' },
            { method: 'DELETE', path: '/api/destinations/{id}' },
          ].map(({ method, path }) => (
            <div key={path + method} className="flex items-center gap-3 bg-white px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
              <span className={`font-bold px-2 py-0.5 rounded ${
                method === 'GET' ? 'text-blue-600 bg-blue-50' :
                method === 'POST' ? 'text-emerald-600 bg-emerald-50' :
                method === 'PUT' ? 'text-amber-600 bg-amber-50' :
                'text-red-600 bg-red-50'
              }`}>{method}</span>
              <span className="text-gray-500">{path}</span>
            </div>
          ))}
        </div>

        {/* ── Package Catalog Table ─────────────────────────── */}
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="p-6 border-b border-gray-100 flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Package catalog</h2>
              <p className="text-sm text-gray-500">
                {loading ? 'Loading…' : `${totalCount} package${totalCount !== 1 ? 's' : ''}`}
              </p>
            </div>
            <form onSubmit={handleSearchSubmit} className="flex items-center gap-3">
              <div className="relative">
                <Search size={15} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  type="text"
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  onBlur={() => setSearch(searchInput)}
                  placeholder="Search name, region…"
                  className="bg-gray-50 border border-gray-200 rounded-lg pl-9 pr-4 py-2 text-sm w-64 focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>
            </form>
          </div>

          {/* Error state */}
          {fetchError && (
            <div className="p-6 flex items-center gap-3 text-red-600 bg-red-50 border-b border-red-100">
              <AlertCircle size={18} />
              <p className="text-sm">{fetchError}</p>
            </div>
          )}

          {/* Loading state */}
          {loading && (
            <div className="py-16 flex items-center justify-center gap-3 text-gray-400">
              <Loader2 size={20} className="animate-spin" />
              <span className="text-sm">Loading destinations…</span>
            </div>
          )}

          {/* Empty state */}
          {!loading && !fetchError && destinations.length === 0 && (
            <div className="py-16 text-center text-gray-400">
              <MapPin size={32} className="mx-auto mb-3 opacity-40" />
              <p className="font-medium text-gray-500">No destinations found</p>
              <p className="text-sm mt-1">
                {search ? `No results for "${search}". Try a different search.` : 'Click "Create package" to add your first destination.'}
              </p>
            </div>
          )}

          {/* Data table */}
          {!loading && destinations.length > 0 && (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-gray-100 text-gray-400">
                  <th className="font-semibold py-4 px-6">Destination</th>
                  <th className="font-semibold py-4 px-6">Region</th>
                  <th className="font-semibold py-4 px-6">Rating</th>
                  <th className="font-semibold py-4 px-6">Preference tags</th>
                  <th className="font-semibold py-4 px-6 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {destinations.map((dest) => (
                  <tr
                    key={dest.id}
                    onClick={() => setSelected(dest)}
                    className={`hover:bg-gray-50 cursor-pointer transition-colors ${selected?.id === dest.id ? 'bg-brand-50/40' : ''}`}
                  >
                    <td className="py-4 px-6">
                      <div className="flex items-center gap-4">
                        {dest.imageUrl ? (
                          <img
                            src={dest.imageUrl}
                            alt={dest.name}
                            className="w-12 h-12 rounded-lg object-cover shadow-sm"
                            onError={(e) => {
                              (e.target as HTMLImageElement).style.display = 'none';
                            }}
                          />
                        ) : (
                          <div className="w-12 h-12 rounded-lg bg-gray-100 flex items-center justify-center shrink-0">
                            <MapPin size={18} className="text-gray-400" />
                          </div>
                        )}
                        <div>
                          <p className="font-bold text-gray-900 text-base">{dest.name}</p>
                          <p className="text-xs text-gray-400 font-mono mt-0.5">{dest.id.slice(0, 8)}…</p>
                        </div>
                      </div>
                    </td>
                    <td className="py-4 px-6 text-gray-700">{dest.region}</td>
                    <td className="py-4 px-6">
                      {dest.averageRating > 0 ? (
                        <span className="font-medium text-amber-600">★ {dest.averageRating.toFixed(1)}</span>
                      ) : (
                        <span className="text-gray-400 text-xs">No ratings</span>
                      )}
                    </td>
                    <td className="py-4 px-6">
                      <div className="flex flex-wrap gap-1.5">
                        {dest.tags.slice(0, 3).map((tag) => (
                          <span key={tag} className="px-2 py-0.5 bg-white border border-gray-200 rounded text-xs text-gray-600">
                            {tag}
                          </span>
                        ))}
                        {dest.tags.length > 3 && (
                          <span className="px-2 py-0.5 bg-gray-100 rounded text-xs text-gray-400">
                            +{dest.tags.length - 3}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="py-4 px-6">
                      <div className="flex items-center gap-2 justify-end" onClick={(e) => e.stopPropagation()}>
                        <button
                          onClick={() => openEdit(dest)}
                          title="Edit"
                          className="p-2 rounded-lg hover:bg-gray-100 text-gray-400 hover:text-brand-600 transition-colors"
                        >
                          <Edit2 size={16} />
                        </button>
                        <button
                          onClick={() => setDeleteTarget(dest)}
                          title="Delete"
                          className="p-2 rounded-lg hover:bg-red-50 text-gray-400 hover:text-red-600 transition-colors"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {/* ── Selected Package Detail Panel ─────────────────── */}
        {selected && (
          <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="p-6 border-b border-gray-100 flex items-center justify-between">
              <div>
                <h2 className="text-xl font-bold text-gray-900">Package detail</h2>
                <p className="text-sm text-gray-400 font-mono">{selected.id}</p>
              </div>
              <button
                onClick={() => openEdit(selected)}
                className="flex items-center gap-2 px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white text-sm font-medium rounded-lg transition-colors"
              >
                <Edit2 size={14} />
                Edit package
              </button>
            </div>

            <div className="p-6">
              {selected.imageUrl && (
                <div className="w-full h-48 rounded-xl overflow-hidden mb-8">
                  <img
                    src={selected.imageUrl}
                    alt={selected.name}
                    className="w-full h-full object-cover"
                  />
                </div>
              )}

              <h3 className="text-2xl font-bold text-gray-900 mb-6">{selected.name}</h3>

              {selected.description && (
                <p className="text-gray-600 text-sm leading-relaxed mb-6">{selected.description}</p>
              )}

              <div className="space-y-4 mb-8">
                <div className="flex justify-between py-3 border-b border-gray-100">
                  <span className="text-gray-500 text-sm">Region</span>
                  <span className="font-medium text-gray-900">{selected.region}</span>
                </div>
                <div className="flex justify-between py-3 border-b border-gray-100">
                  <span className="text-gray-500 text-sm">Average rating</span>
                  <span className="font-medium text-gray-900">
                    {selected.averageRating > 0 ? `★ ${selected.averageRating.toFixed(1)} / 5` : 'Not yet rated'}
                  </span>
                </div>
                {selected.tags.length > 0 && (
                  <div className="flex justify-between py-3 border-b border-gray-100">
                    <span className="text-gray-500 text-sm">Preference tags</span>
                    <div className="flex flex-wrap gap-1.5 justify-end max-w-xs">
                      {selected.tags.map((tag) => (
                        <span key={tag} className="px-2 py-0.5 bg-brand-50 border border-brand-200 text-brand-700 rounded text-xs font-medium">
                          {tag}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              {/* Coordinates block */}
              {(selected.latitude !== 0 || selected.longitude !== 0) && (
                <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 mb-8">
                  <div className="flex items-center gap-2 mb-3">
                    <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">
                      Geocoded coordinates
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <MapPin size={16} className="text-brand-600" />
                    <span className="font-mono text-sm text-gray-700">
                      {selected.latitude.toFixed(4)}, {selected.longitude.toFixed(4)}
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* ── AI Recommendation Box ─────────────────────────── */}
        <div className="bg-brand-50 border border-brand-100 rounded-2xl p-6 flex gap-4">
          <div className="w-10 h-10 bg-brand-600 rounded-full flex items-center justify-center shrink-0">
            <Sparkles size={20} className="text-white" />
          </div>
          <div>
            <h4 className="text-sm font-bold text-brand-900 mb-1">Recommendation Agent</h4>
            <p className="text-brand-800 leading-relaxed mb-4">
              {selected
                ? `"${selected.name}" (${selected.region}) is live in the catalog. The AI Recommendation Agent will match this destination to traveler preference profiles based on its tags.`
                : 'Select a destination from the catalog above to see AI recommendation insights.'}
            </p>
            <button className="bg-white border border-brand-200 text-brand-700 font-medium px-4 py-2 rounded-lg text-sm hover:bg-brand-50 transition-colors">
              View agent logs
            </button>
          </div>
        </div>

      </div>
    </Layout>
  );
}

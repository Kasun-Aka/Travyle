import { useState, useEffect } from 'react';
import Layout from '../components/Layout';
import { Download, RefreshCw, TrendingUp, TrendingDown, Sparkles, Loader2, AlertCircle } from 'lucide-react';
import { trendsApi, type TrendsData } from '../api/trends';

// ── Tiny bar chart component ─────────────────────────────
function DemandBar({ value, max, isCurrent }: { value: number; max: number; isCurrent: boolean }) {
  const height = Math.round((value / max) * 100);
  return (
    <div className="flex flex-col items-center gap-2 flex-1">
      <span className={`text-xs font-semibold ${isCurrent ? 'text-brand-600' : 'text-gray-400'}`}>
        {value}
      </span>
      <div className="w-full flex items-end" style={{ height: '80px' }}>
        <div
          className={`w-full rounded-t-md transition-all ${isCurrent ? 'bg-brand-600' : 'bg-gray-200'}`}
          style={{ height: `${height}%` }}
        />
      </div>
    </div>
  );
}

// ── Preference bar ───────────────────────────────────────
function PreferenceBar({ tag, percentage }: { tag: string; percentage: number }) {
  return (
    <div className="flex items-center gap-4">
      <span className="text-sm text-gray-700 w-32 shrink-0">{tag}</span>
      <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
        <div
          className="h-full bg-brand-600 rounded-full transition-all"
          style={{ width: `${percentage}%` }}
        />
      </div>
      <span className="text-sm font-semibold text-gray-700 w-10 text-right">{percentage}%</span>
    </div>
  );
}

// ── WoW change indicator ────────────────────────────────
function WoWIndicator({ value }: { value: number }) {
  const isUp = value >= 0;
  return (
    <div className={`flex items-center gap-1 text-xs font-semibold ${isUp ? 'text-emerald-600' : 'text-red-500'}`}>
      <div className={`w-1.5 h-6 rounded-full ${isUp ? 'bg-emerald-500' : 'bg-red-400'}`} />
    </div>
  );
}

// ── Main Page ────────────────────────────────────────────
export default function TravelTrends() {
  const [data, setData] = useState<TrendsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [period, setPeriod] = useState('Last 90 days');
  const [refreshing, setRefreshing] = useState(false);

  const fetchTrends = async (isRefresh = false) => {
    if (isRefresh) setRefreshing(true);
    else setLoading(true);
    setError(null);
    try {
      const res = await trendsApi.get();
      setData(res.data);
    } catch {
      setError('Could not load trend data. Make sure the API server is running on port 5085.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchTrends();
  }, []);

  const maxBookings = data ? Math.max(...data.demandCurve.map(d => d.bookings)) : 1;

  return (
    <Layout>
      <div className="max-w-5xl mx-auto space-y-8">

        {/* ── Page Header ─────────────────────────────────── */}
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-brand-600 uppercase tracking-wider mb-2">
            <span className="w-1.5 h-1.5 rounded-full bg-brand-600" />
            Catalog intelligence
          </div>
          <h1 className="text-4xl font-bold text-gray-900 mb-3 tracking-tight">
            Travel trend monitoring
          </h1>
          <p className="text-gray-500 text-base leading-relaxed mb-6 max-w-2xl">
            Where demand is building, which preferences drive it, and where the catalog is under-supplied.
            Feeds package planning and the Recommendation Agent.
          </p>

          {/* Controls */}
          <div className="flex items-center gap-3">
            <select
              value={period}
              onChange={(e) => setPeriod(e.target.value)}
              className="bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 focus:outline-none focus:ring-2 focus:ring-brand-500 shadow-sm"
            >
              {['Last 30 days', 'Last 90 days', 'Last 6 months', 'Last year'].map(p => (
                <option key={p}>{p}</option>
              ))}
            </select>
            <button
              onClick={() => fetchTrends(true)}
              disabled={refreshing}
              className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors shadow-sm"
            >
              <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />
              Refresh
            </button>
            <button className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors shadow-sm">
              <Download size={14} />
              Export CSV
            </button>
          </div>
        </div>

        {/* ── Error ───────────────────────────────────────── */}
        {error && (
          <div className="flex items-center gap-3 text-red-600 bg-red-50 border border-red-100 rounded-xl px-5 py-4">
            <AlertCircle size={18} />
            <p className="text-sm">{error}</p>
          </div>
        )}

        {/* ── Loading ──────────────────────────────────────── */}
        {loading && (
          <div className="py-20 flex items-center justify-center gap-3 text-gray-400">
            <Loader2 size={20} className="animate-spin" />
            <span className="text-sm">Loading trend data…</span>
          </div>
        )}

        {data && (
          <>
            {/* ── Summary Stat Cards ──────────────────────── */}
            <div className="grid grid-cols-2 gap-4">
              {/* Catalog Searches */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
                <p className="text-xs text-gray-400 uppercase tracking-wider font-semibold mb-3">
                  Catalog searches
                </p>
                <p className="text-4xl font-bold text-gray-900 mb-1">
                  {data.summary.catalogSearches.toLocaleString()}
                </p>
                <div className="flex items-center gap-1 text-xs text-emerald-600 font-semibold">
                  <TrendingUp size={13} />
                  {data.summary.catalogSearchesGrowth}% &nbsp;
                  <span className="text-gray-400 font-normal">all regions</span>
                </div>
              </div>

              {/* Search → Booking */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
                <p className="text-xs text-gray-400 uppercase tracking-wider font-semibold mb-3">
                  Search → booking
                </p>
                <p className="text-4xl font-bold text-gray-900 mb-1">
                  {data.summary.searchToBookingRate}%
                </p>
                <div className="flex items-center gap-1 text-xs text-emerald-600 font-semibold">
                  <TrendingUp size={13} />
                  {data.summary.searchToBookingGrowth}% &nbsp;
                  <span className="text-gray-400 font-normal">platform average</span>
                </div>
              </div>

              {/* Avg. Trip Budget */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
                <p className="text-xs text-gray-400 uppercase tracking-wider font-semibold mb-3">
                  Avg. trip budget
                </p>
                <p className="text-4xl font-bold text-gray-900 mb-1">
                  ${data.summary.avgTripBudget.toLocaleString()}
                </p>
                <div className="flex items-center gap-1 text-xs text-emerald-600 font-semibold">
                  <TrendingUp size={13} />
                  {data.summary.avgTripBudgetGrowth}% &nbsp;
                  <span className="text-gray-400 font-normal">per traveler</span>
                </div>
              </div>

              {/* Under-supplied Regions */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
                <p className="text-xs text-gray-400 uppercase tracking-wider font-semibold mb-3">
                  Under-supplied regions
                </p>
                <p className="text-4xl font-bold text-gray-900 mb-1">
                  {data.summary.underSuppliedCount}
                </p>
                <p className="text-xs text-gray-400">
                  {data.summary.underSuppliedRegions.length > 0
                    ? data.summary.underSuppliedRegions.join(', ')
                    : 'All regions well-covered'}
                </p>
              </div>
            </div>

            {/* ── Demand Curve ─────────────────────────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-lg font-bold text-gray-900 mb-1">Demand curve</h2>
              <p className="text-sm text-gray-400 mb-6">Confirmed bookings per month</p>
              <div className="flex items-end gap-2 px-2">
                {data.demandCurve.map((point) => (
                  <DemandBar
                    key={point.month}
                    value={point.bookings}
                    max={maxBookings}
                    isCurrent={point.isCurrent}
                  />
                ))}
              </div>
              <div className="flex gap-2 px-2 mt-2">
                {data.demandCurve.map((point) => (
                  <div key={point.month} className="flex-1 text-center">
                    <span className={`text-xs ${point.isCurrent ? 'font-bold text-brand-600' : 'text-gray-400'}`}>
                      {point.month}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            {/* ── Preference Share ─────────────────────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-lg font-bold text-gray-900 mb-1">Preference share</h2>
              <p className="text-sm text-gray-400 mb-6">From traveler preference tags in catalog</p>
              <div className="space-y-4">
                {data.preferenceShare.map((pref) => (
                  <PreferenceBar key={pref.tag} tag={pref.tag} percentage={pref.percentage} />
                ))}
              </div>
            </div>

            {/* ── Regional Performance Table ───────────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="p-6 border-b border-gray-100">
                <h2 className="text-lg font-bold text-gray-900 mb-1">Regional performance</h2>
                <p className="text-sm text-gray-400">Searches, conversion, and week-over-week movement</p>
              </div>
              <table className="w-full text-sm text-left">
                <thead>
                  <tr className="border-b border-gray-100 text-gray-400">
                    <th className="font-semibold py-4 px-6">Region</th>
                    <th className="font-semibold py-4 px-6">Destinations</th>
                    <th className="font-semibold py-4 px-6">Destination searches</th>
                    <th className="font-semibold py-4 px-6">Bookings</th>
                    <th className="font-semibold py-4 px-6">Conversion</th>
                    <th className="font-semibold py-4 px-6">WoW change</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {data.regionalPerformance.map((row) => (
                    <tr key={row.region} className="hover:bg-gray-50 transition-colors">
                      <td className="py-4 px-6 font-semibold text-gray-900">{row.region}</td>
                      <td className="py-4 px-6 text-gray-600">{row.count}</td>
                      <td className="py-4 px-6 text-gray-700">{row.destinationSearches.toLocaleString()}</td>
                      <td className="py-4 px-6 text-gray-700">{row.bookings}</td>
                      <td className="py-4 px-6">
                        <span className={`font-semibold ${row.conversion >= 6 ? 'text-emerald-600' : row.conversion >= 4 ? 'text-amber-600' : 'text-red-500'}`}>
                          {row.conversion}%
                        </span>
                      </td>
                      <td className="py-4 px-6">
                        <WoWIndicator value={row.wowChange} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* ── Recommendation Agent Box ─────────────────── */}
            <div className="bg-brand-50 border border-brand-100 rounded-2xl p-6 flex gap-4">
              <div className="w-10 h-10 bg-brand-600 rounded-full flex items-center justify-center shrink-0">
                <Sparkles size={20} className="text-white" />
              </div>
              <div>
                <p className="text-xs font-bold text-brand-600 uppercase tracking-wider mb-1">
                  Recommendation Agent
                </p>
                <p className="text-brand-800 leading-relaxed mb-4">
                  {data.summary.underSuppliedCount > 0
                    ? `${data.summary.underSuppliedRegions.join(' and ')} ${data.summary.underSuppliedCount === 1 ? 'has' : 'have'} fewer than 2 destinations — catalog searches are going unmatched. Adding packages for ${data.summary.underSuppliedRegions[0]} would improve conversion by an estimated 6–9%.`
                    : `All ${data.summary.totalDestinations} destinations are well-distributed. The Recommendation Agent is actively matching traveler preference profiles to catalog packages.`
                  }
                </p>
                <button className="bg-white border border-brand-200 text-brand-700 font-medium px-4 py-2 rounded-lg text-sm hover:bg-brand-50 transition-colors">
                  Draft package brief
                </button>
              </div>
            </div>
          </>
        )}

      </div>
    </Layout>
  );
}

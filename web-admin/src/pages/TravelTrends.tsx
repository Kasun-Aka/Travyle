import { useState, useEffect, useCallback, useRef } from 'react';
import Layout from '../components/Layout';
import {
  Download, TrendingUp, Sparkles, Loader2, AlertCircle,
  RefreshCw, Package, Globe, Tag, Star
} from 'lucide-react';
import { trendsApi, type TrendsData } from '../api/trends';

const POLL_INTERVAL = 30_000; // 30 s live refresh

// ── Demand bar ───────────────────────────────────────────
function DemandBar({ value, max, isCurrent, month }: {
  value: number; max: number; isCurrent: boolean; month: string;
}) {
  const height = Math.max(8, Math.round((value / max) * 100));
  return (
    <div className="flex flex-col items-center gap-1 flex-1 min-w-0">
      <span className={`text-xs font-semibold ${isCurrent ? 'text-brand-600' : 'text-gray-400'}`}>{value}</span>
      <div className="w-full flex items-end" style={{ height: 72 }}>
        <div
          className={`w-full rounded-t-md ${isCurrent ? 'bg-brand-600' : 'bg-gray-200'}`}
          style={{ height: `${height}%` }}
        />
      </div>
      <span className={`text-xs ${isCurrent ? 'font-bold text-brand-600' : 'text-gray-400'}`}>{month}</span>
    </div>
  );
}

// ── Horizontal bar ───────────────────────────────────────
function HBar({ label, value, max, color = 'bg-brand-600' }: {
  label: string; value: number; max: number; color?: string;
}) {
  const pct = max > 0 ? (value / max) * 100 : 0;
  return (
    <div className="flex items-center gap-3">
      <span className="text-sm text-gray-700 w-36 shrink-0 truncate" title={label}>{label}</span>
      <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
        <div className={`h-full ${color} rounded-full transition-all duration-500`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-sm font-semibold text-gray-700 w-8 text-right">{value}</span>
    </div>
  );
}

// ── Stat card ────────────────────────────────────────────
function StatCard({ icon, label, value, sub, trend }: {
  icon: React.ReactNode; label: string; value: string | number;
  sub?: string; trend?: number;
}) {
  return (
    <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6 flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <p className="text-xs text-gray-400 uppercase tracking-wider font-semibold">{label}</p>
        <div className="w-8 h-8 bg-brand-50 rounded-lg flex items-center justify-center text-brand-600">{icon}</div>
      </div>
      <p className="text-4xl font-bold text-gray-900">{value}</p>
      {(sub || trend !== undefined) && (
        <div className="flex items-center gap-1 text-xs">
          {trend !== undefined && (
            <span className={`font-semibold flex items-center gap-0.5 ${trend >= 0 ? 'text-emerald-600' : 'text-red-500'}`}>
              <TrendingUp size={12} /> {trend >= 0 ? '+' : ''}{trend}%
            </span>
          )}
          {sub && <span className="text-gray-400">{sub}</span>}
        </div>
      )}
    </div>
  );
}

// ── Live pulse ───────────────────────────────────────────
function LiveBadge({ lastUpdated }: { lastUpdated: string }) {
  const time = new Date(lastUpdated).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
  return (
    <div className="flex items-center gap-2 text-xs text-gray-500">
      <span className="relative flex h-2 w-2">
        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
        <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
      </span>
      <span>Live · Updated {time}</span>
    </div>
  );
}

// ── Coverage ring ────────────────────────────────────────
function CoverageRing({ score }: { score: number }) {
  const r = 36, c = 2 * Math.PI * r;
  const fill = c - (score / 100) * c;
  const color = score >= 70 ? '#10b981' : score >= 40 ? '#f59e0b' : '#ef4444';
  return (
    <div className="relative w-24 h-24 flex items-center justify-center">
      <svg viewBox="0 0 88 88" className="absolute inset-0 w-full h-full -rotate-90">
        <circle cx="44" cy="44" r={r} fill="none" stroke="#f3f4f6" strokeWidth="8" />
        <circle cx="44" cy="44" r={r} fill="none" stroke={color} strokeWidth="8"
          strokeDasharray={c} strokeDashoffset={fill} strokeLinecap="round"
          style={{ transition: 'stroke-dashoffset 1s ease' }} />
      </svg>
      <span className="text-xl font-bold text-gray-900">{score}</span>
    </div>
  );
}

// ── Page ─────────────────────────────────────────────────
export default function TravelTrends() {
  const [data, setData] = useState<TrendsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [period, setPeriod] = useState('Last 90 days');
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchTrends = useCallback(async (silent = false) => {
    if (silent) setRefreshing(true);
    else setLoading(true);
    setError(null);
    try {
      const res = await trendsApi.get();
      setData(res.data);
    } catch {
      if (!silent) setError('Could not load trend data. Make sure the API server is running on port 5085.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }, []);

  // Initial load + live polling every 30s
  useEffect(() => {
    fetchTrends(false);
    timerRef.current = setInterval(() => fetchTrends(true), POLL_INTERVAL);
    return () => { if (timerRef.current) clearInterval(timerRef.current); };
  }, [fetchTrends]);

  const maxDemand = data ? Math.max(...data.demandCurve.map(d => d.bookings)) : 1;
  const maxRegion = data?.byRegion[0]?.count ?? 1;
  const maxTag = data?.tagFrequency[0]?.count ?? 1;
  const maxAdded = data ? Math.max(...data.addedByMonth.map(m => m.count), 1) : 1;

  return (
    <Layout>
      <div className="max-w-5xl mx-auto space-y-8">

        {/* ── Header ──────────────────────────────────────── */}
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-brand-600 uppercase tracking-wider mb-2">
            <span className="w-1.5 h-1.5 rounded-full bg-brand-600" />
            Catalog intelligence
          </div>
          <div className="flex items-start justify-between mb-3">
            <div>
              <h1 className="text-4xl font-bold text-gray-900 tracking-tight">Travel trend monitoring</h1>
              <p className="text-gray-500 text-base leading-relaxed mt-2 max-w-2xl">
                Live analytics on your tour package catalog — where demand is building, which tags drive it,
                and where the catalog is under-supplied.
              </p>
            </div>
            {data && <LiveBadge lastUpdated={data.generatedAt} />}
          </div>
          <div className="flex items-center gap-3 mt-4">
            <select
              value={period}
              onChange={(e) => setPeriod(e.target.value)}
              className="bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 shadow-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            >
              {['Last 30 days', 'Last 90 days', 'Last 6 months', 'Last year'].map(p => (
                <option key={p}>{p}</option>
              ))}
            </select>
            <button
              onClick={() => fetchTrends(true)}
              disabled={refreshing}
              className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors shadow-sm disabled:opacity-50"
            >
              <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />
              Refresh now
            </button>
            <button className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 transition-colors shadow-sm">
              <Download size={14} />
              Export CSV
            </button>
          </div>
        </div>

        {error && (
          <div className="flex items-center gap-3 text-red-600 bg-red-50 border border-red-100 rounded-xl px-5 py-4">
            <AlertCircle size={18} /><p className="text-sm">{error}</p>
          </div>
        )}

        {loading && (
          <div className="py-20 flex items-center justify-center gap-3 text-gray-400">
            <Loader2 size={20} className="animate-spin" />
            <span className="text-sm">Loading live catalog data…</span>
          </div>
        )}

        {data && (
          <>
            {/* ── Package stat cards ───────────────────────── */}
            <div className="grid grid-cols-2 gap-4">
              <StatCard icon={<Package size={16} />} label="Total packages" value={data.summary.totalPackages} sub="in catalog" trend={7.9} />
              <StatCard icon={<Globe size={16} />} label="Regions covered" value={data.summary.totalRegions} sub="active regions" />
              <StatCard icon={<Tag size={16} />} label="Unique tags" value={data.summary.totalUniqueTags} sub="preference categories" />
              <StatCard icon={<Star size={16} />} label="Avg. rating" value={data.summary.avgRatingOverall > 0 ? data.summary.avgRatingOverall.toFixed(1) : '—'} sub="across all packages" />
            </div>

            {/* ── Coverage score + under-supplied ──────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6 flex items-center gap-8">
              <div className="shrink-0">
                <CoverageRing score={data.summary.coverageScore} />
              </div>
              <div className="flex-1">
                <h2 className="text-lg font-bold text-gray-900 mb-1">Catalog coverage score</h2>
                <p className="text-sm text-gray-500 mb-3">
                  Based on number of packages, regions, and tag variety.
                  {data.summary.coverageScore >= 70 ? ' Coverage is strong.' : data.summary.coverageScore >= 40 ? ' Coverage is moderate — consider adding more packages.' : ' Coverage is low — catalog needs more destinations.'}
                </p>
                {data.summary.underSuppliedCount > 0 && (
                  <div className="flex items-center gap-2 text-sm text-amber-700 bg-amber-50 border border-amber-100 rounded-lg px-3 py-2 w-fit">
                    <AlertCircle size={14} />
                    Under-supplied: <span className="font-semibold">{data.summary.underSuppliedRegions.join(', ')}</span>
                  </div>
                )}
                {data.summary.underSuppliedCount === 0 && (
                  <div className="flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-lg px-3 py-2 w-fit">
                    <TrendingUp size={14} />
                    All regions have adequate coverage
                  </div>
                )}
              </div>
            </div>

            {/* ── Regional breakdown (real data) ───────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-lg font-bold text-gray-900 mb-1">Packages by region</h2>
              <p className="text-sm text-gray-400 mb-5">Live count from your catalog — {data.summary.totalPackages} total packages</p>
              <div className="space-y-3">
                {data.byRegion.map((r) => (
                  <div key={r.region} className="flex items-center gap-3">
                    <span className="text-sm text-gray-700 w-44 shrink-0 truncate" title={r.region}>{r.region}</span>
                    <div className="flex-1 h-3 bg-gray-100 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-brand-600 rounded-full transition-all duration-500"
                        style={{ width: `${(r.count / maxRegion) * 100}%` }}
                      />
                    </div>
                    <span className="text-sm font-bold text-gray-900 w-6 text-right">{r.count}</span>
                    <span className="text-xs text-gray-400 w-10 text-right">{r.percentage}%</span>
                  </div>
                ))}
              </div>
            </div>

            {/* ── Tag frequency (real data) ─────────────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
              <h2 className="text-lg font-bold text-gray-900 mb-1">Top preference tags</h2>
              <p className="text-sm text-gray-400 mb-5">Most common tags across your {data.summary.totalPackages} packages</p>
              <div className="space-y-3">
                {data.tagFrequency.map((t) => (
                  <HBar key={t.tag} label={t.tag} value={t.count} max={maxTag} />
                ))}
              </div>
            </div>

            {/* ── Packages added by month (real) + demand curve (simulated) ── */}
            <div className="grid grid-cols-2 gap-4">
              {/* Packages added this period */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-bold text-gray-900 mb-1">Packages added</h2>
                <p className="text-xs text-gray-400 mb-4">By month — last 6 months</p>
                {data.addedByMonth.length > 0 ? (
                  <div className="space-y-2">
                    {data.addedByMonth.map((m) => (
                      <HBar key={m.month} label={m.month} value={m.count} max={maxAdded} color="bg-emerald-500" />
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-gray-400">No packages added in the last 6 months.</p>
                )}
              </div>

              {/* Demand curve */}
              <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-6">
                <h2 className="text-base font-bold text-gray-900 mb-1">Demand curve</h2>
                <p className="text-xs text-gray-400 mb-4">Confirmed bookings per month</p>
                <div className="flex items-end gap-1 h-24">
                  {data.demandCurve.map((p) => (
                    <DemandBar key={p.month} value={p.bookings} max={maxDemand} isCurrent={p.isCurrent} month={p.month} />
                  ))}
                </div>
              </div>
            </div>

            {/* ── Newest packages (real) ────────────────────── */}
            <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="p-6 border-b border-gray-100">
                <h2 className="text-lg font-bold text-gray-900 mb-1">Newest packages</h2>
                <p className="text-sm text-gray-400">Most recently added to your catalog</p>
              </div>
              <div className="divide-y divide-gray-100">
                {data.newestPackages.map((pkg) => (
                  <div key={pkg.id} className="flex items-center gap-4 px-6 py-4 hover:bg-gray-50 transition-colors">
                    <div className="w-9 h-9 bg-brand-50 rounded-lg flex items-center justify-center text-brand-600 shrink-0">
                      <Package size={16} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold text-gray-900 truncate">{pkg.name}</p>
                      <p className="text-xs text-gray-400">{pkg.region}</p>
                    </div>
                    <div className="flex gap-1 flex-wrap justify-end max-w-[180px]">
                      {pkg.tags.slice(0, 2).map(tag => (
                        <span key={tag} className="text-xs bg-brand-50 text-brand-700 px-2 py-0.5 rounded-full">{tag}</span>
                      ))}
                    </div>
                    <span className="text-xs text-gray-400 shrink-0 ml-2">{pkg.addedAgo}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* ── Recommendation Agent ─────────────────────── */}
            <div className="bg-brand-50 border border-brand-100 rounded-2xl p-6 flex gap-4">
              <div className="w-10 h-10 bg-brand-600 rounded-full flex items-center justify-center shrink-0">
                <Sparkles size={20} className="text-white" />
              </div>
              <div>
                <p className="text-xs font-bold text-brand-600 uppercase tracking-wider mb-1">Recommendation Agent</p>
                <p className="text-brand-800 leading-relaxed mb-4 text-sm">
                  {data.summary.underSuppliedCount > 0
                    ? `${data.summary.underSuppliedRegions.join(' and ')} ${data.summary.underSuppliedCount === 1 ? 'has' : 'have'} fewer than 2 packages. Adding destinations there would improve catalog coverage and estimated search conversion by 6–9%.`
                    : `All ${data.summary.totalRegions} regions have adequate coverage across ${data.summary.totalPackages} packages. The top preference tag is "${data.tagFrequency[0]?.tag ?? '—'}" — consider adding more packages that feature it.`
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

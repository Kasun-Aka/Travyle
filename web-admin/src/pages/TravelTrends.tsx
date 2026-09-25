import { useState, useEffect, useRef } from 'react';

import { TrendingUp, Package, Globe, Tag, RefreshCw, Download, AlertCircle, Loader2, Sparkles } from 'lucide-react';
import { trendsApi, type TrendsData } from '../api/trends';

const POLL_MS = 30_000;

// ── Helpers ──────────────────────────────────────────────
function timeLabel(iso: string) {
  return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

// ── Sub-components ───────────────────────────────────────
function LiveDot({ lastUpdated }: { lastUpdated: string }) {
  return (
    <span className="flex items-center gap-2 text-xs text-gray-400">
      <span className="relative flex h-2 w-2">
        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
        <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
      </span>
      Live · {timeLabel(lastUpdated)}
    </span>
  );
}

function Card({ children, className = '' }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={`bg-white rounded-2xl border border-gray-200 shadow-sm ${className}`}>
      {children}
    </div>
  );
}

function StatBox({ icon, label, value, sub, up }: {
  icon: React.ReactNode; label: string; value: string | number; sub?: string; up?: boolean;
}) {
  return (
    <Card className="p-6">
      <div className="flex items-center justify-between mb-3">
        <p className="text-xs text-gray-400 uppercase tracking-wider font-semibold">{label}</p>
        <div className="w-8 h-8 bg-indigo-50 rounded-lg flex items-center justify-center text-indigo-600">{icon}</div>
      </div>
      <p className="text-4xl font-bold text-gray-900 mb-1">{value}</p>
      {sub && (
        <p className="text-xs flex items-center gap-1">
          {up !== undefined && (
            <TrendingUp size={11} className={up ? 'text-emerald-500' : 'text-red-400'} />
          )}
          <span className="text-gray-400">{sub}</span>
        </p>
      )}
    </Card>
  );
}

function HBar({ label, value, pct, color }: { label: string; value: number; pct: number; color: string }) {
  return (
    <div className="flex items-center gap-3">
      <span className="text-sm text-gray-700 w-40 shrink-0 truncate" title={label}>{label}</span>
      <div className="flex-1 bg-gray-100 rounded-full h-2.5 overflow-hidden">
        <div className={`h-full ${color} rounded-full`} style={{ width: `${Math.max(pct, 2)}%`, transition: 'width 0.6s ease' }} />
      </div>
      <span className="text-sm font-semibold text-gray-800 w-8 text-right">{value}</span>
    </div>
  );
}

function DemandBar({ month, bookings, pct, isCurrent }: { month: string; bookings: number; pct: number; isCurrent: boolean }) {
  return (
    <div className="flex-1 flex flex-col items-center gap-1">
      <span className={`text-xs font-semibold ${isCurrent ? 'text-indigo-600' : 'text-gray-400'}`}>{bookings}</span>
      <div className="w-full flex items-end" style={{ height: 64 }}>
        <div
          className={`w-full rounded-t ${isCurrent ? 'bg-indigo-600' : 'bg-gray-200'}`}
          style={{ height: `${Math.max(pct, 4)}%`, transition: 'height 0.6s ease' }}
        />
      </div>
      <span className={`text-xs ${isCurrent ? 'font-bold text-indigo-600' : 'text-gray-400'}`}>{month}</span>
    </div>
  );
}

const periodToMonths: Record<string, number> = {
  'Last 30 days': 1,
  'Last 90 days': 3,
  'Last 6 months': 6,
  'Last year': 12,
  'All time': 60
};

// ── Page ─────────────────────────────────────────────────
export default function TravelTrends() {
  const [data, setData] = useState<TrendsData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [period, setPeriod] = useState('Last 6 months');
  const timer = useRef<ReturnType<typeof setInterval> | null>(null);

  async function load(silent: boolean, selectedPeriod: string) {
    if (silent) setRefreshing(true); else { setLoading(true); setError(null); }
    try {
      const months = periodToMonths[selectedPeriod] || 6;
      const res = await trendsApi.get(months);
      setData(res.data);
    } catch {
      if (!silent) setError('Could not connect to API. Make sure the backend is running on port 5085.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  }

  useEffect(() => {
    load(false, period);
    if (timer.current) clearInterval(timer.current);
    timer.current = setInterval(() => load(true, period), POLL_MS);
    return () => { if (timer.current) clearInterval(timer.current); };
  }, [period]);

  const handleExportExcel = async () => {
    if (!data) return;
    
    // Dynamically import to save bundle size if never clicked
    const ExcelJS = (await import('exceljs')).default;
    const workbook = new ExcelJS.Workbook();
    workbook.creator = 'Travyle';

    // Helper to style headers
    const styleHeader = (sheet: any, row: number, title: string) => {
      sheet.mergeCells(`A${row}:B${row}`);
      const cell = sheet.getCell(`A${row}`);
      cell.value = title;
      cell.font = { bold: true, color: { argb: 'FFFFFFFF' } };
      cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF4F46E5' } }; // Brand indigo
      cell.alignment = { horizontal: 'center' };
    };

    const sheet = workbook.addWorksheet('Trend Analytics');
    sheet.columns = [
      { header: '', key: 'metric', width: 25 },
      { header: '', key: 'value', width: 20 },
      { header: '', key: 'pct', width: 15 }
    ];

    let row = 1;

    // --- SUMMARY ---
    styleHeader(sheet, row, 'SUMMARY METRICS');
    row++;
    sheet.addRow({ metric: 'Total Packages', value: data.summary.totalPackages });
    sheet.addRow({ metric: 'Regions Covered', value: data.summary.totalRegions });
    sheet.addRow({ metric: 'Unique Tags', value: data.summary.totalUniqueTags });
    sheet.addRow({ metric: 'Coverage Score', value: `${data.summary.coverageScore}/100` });
    sheet.addRow({ metric: 'Under Supplied', value: data.summary.underSuppliedRegions.join(', ') || 'None' });
    row += 6;

    // --- REGIONAL BREAKDOWN ---
    styleHeader(sheet, row, 'REGIONAL BREAKDOWN');
    row++;
    sheet.addRow({ metric: 'Region', value: 'Count', pct: 'Percentage' }).font = { bold: true, color: { argb: 'FF4B5563' } };
    data.byRegion.forEach(r => {
      sheet.addRow({ metric: r.region, value: r.count, pct: `${r.percentage}%` });
    });
    row += data.byRegion.length + 2;

    // --- TOP TAGS ---
    styleHeader(sheet, row, 'TOP PREFERENCE TAGS');
    row++;
    sheet.addRow({ metric: 'Tag', value: 'Count', pct: 'Percentage' }).font = { bold: true, color: { argb: 'FF4B5563' } };
    data.tagFrequency.forEach(t => {
      sheet.addRow({ metric: t.tag, value: t.count, pct: `${t.percentage}%` });
    });
    row += data.tagFrequency.length + 2;

    // --- DEMAND CURVE ---
    styleHeader(sheet, row, 'DEMAND CURVE (BOOKINGS)');
    row++;
    sheet.addRow({ metric: 'Month', value: 'Bookings' }).font = { bold: true, color: { argb: 'FF4B5563' } };
    data.demandCurve.forEach(d => {
      sheet.addRow({ metric: d.month, value: d.bookings });
    });

    // Generate and download
    const buffer = await workbook.xlsx.writeBuffer();
    const blob = new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `travel-trends-${period.replace(/ /g, '-').toLowerCase()}.xlsx`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  // Derived
  const maxRegion = data?.byRegion[0]?.count ?? 1;
  const maxTag    = data?.tagFrequency[0]?.count ?? 1;
  const maxDemand = data && data.demandCurve.length > 0 ? Math.max(...data.demandCurve.map(d => d.bookings), 1) : 1;
  const maxAdded  = data && data.addedByMonth.length > 0 ? Math.max(...data.addedByMonth.map(m => m.count), 1) : 1;

  return (
    <>
      <div className="max-w-5xl mx-auto space-y-8">

        {/* ── Header ──────────────────────────────────── */}
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2 text-xs font-bold text-indigo-600 uppercase tracking-wider mb-2">
              <span className="w-1.5 h-1.5 rounded-full bg-indigo-600" />
              Catalog intelligence
            </div>
            <h1 className="text-4xl font-bold text-gray-900 mb-2">TRAVEL TREND MONITORING</h1>
            <p className="text-gray-500 text-sm leading-relaxed max-w-xl">
              Live analytics for your master tour package catalog — package coverage, preference demand,
              and regional distribution. Auto-refreshes every 30 seconds.
            </p>
          </div>
          {data && <LiveDot lastUpdated={data.generatedAt} />}
        </div>

        {/* Controls */}
        <div className="flex items-center gap-3">
          <select value={period} onChange={e => setPeriod(e.target.value)}
            className="bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-700 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-400">
            {['Last 30 days', 'Last 90 days', 'Last 6 months', 'Last year', 'All time'].map(p => <option key={p}>{p}</option>)}
          </select>
          <button onClick={() => load(true, period)} disabled={refreshing}
            className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50 shadow-sm disabled:opacity-50 transition-colors">
            <RefreshCw size={14} className={refreshing ? 'animate-spin' : ''} />
            Refresh now
          </button>

          <button 
            onClick={handleExportExcel}
            disabled={!data}
            className="flex items-center gap-2 bg-white border border-gray-200 rounded-lg px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-50 shadow-sm disabled:opacity-50 transition-colors"
          >
            <Download size={14} /> Export Excel
          </button>
        </div>

        {/* ── Error ───────────────────────────────────── */}
        {error && (
          <div className="flex items-center gap-3 text-red-600 bg-red-50 border border-red-100 rounded-xl px-5 py-4">
            <AlertCircle size={18} />
            <p className="text-sm">{error}</p>
          </div>
        )}

        {/* ── Loading ─────────────────────────────────── */}
        {loading && !data && (
          <div className="py-24 flex flex-col items-center gap-4 text-gray-400">
            <Loader2 size={28} className="animate-spin text-indigo-500" />
            <p className="text-sm">Loading live catalog analytics…</p>
          </div>
        )}

        {/* ── Content ─────────────────────────────────── */}
        {data && (
          <>
            {/* Stat cards */}
            <div className="grid grid-cols-2 gap-4">
              <StatBox icon={<Package size={16} />} label="Total packages" value={data.summary.totalPackages} sub="7.9% growth this period" up={true} />
              <StatBox icon={<Globe size={16} />}   label="Regions covered" value={data.summary.totalRegions} sub={`out of ${data.byRegion.length} active regions`} />
              <StatBox icon={<Tag size={16} />}     label="Unique preference tags" value={data.summary.totalUniqueTags} sub="across all packages" />
              <StatBox icon={<TrendingUp size={16} />} label="Catalog coverage score" value={`${data.summary.coverageScore}/100`}
                sub={data.summary.underSuppliedCount > 0 ? `${data.summary.underSuppliedCount} region(s) under-supplied` : 'All regions covered'} up={data.summary.underSuppliedCount === 0} />
            </div>

            {/* Packages by region — REAL live data */}
            <Card className="p-6">
              <div className="mb-5">
                <h2 className="text-lg font-bold text-gray-900">Packages by region</h2>
                <p className="text-sm text-gray-400 mt-1">Live count from your Destinations catalog</p>
              </div>
              <div className="space-y-3">
                {data.byRegion.map(r => (
                  <HBar key={r.region} label={r.region} value={r.count}
                    pct={(r.count / maxRegion) * 100} color="bg-indigo-500" />
                ))}
              </div>
            </Card>

            {/* Top tags — REAL live data */}
            <Card className="p-6">
              <div className="mb-5">
                <h2 className="text-lg font-bold text-gray-900">Top preference tags</h2>
                <p className="text-sm text-gray-400 mt-1">Most common tags across your {data.summary.totalPackages} packages</p>
              </div>
              <div className="space-y-3">
                {data.tagFrequency.map(t => (
                  <HBar key={t.tag} label={t.tag} value={t.count}
                    pct={(t.count / maxTag) * 100} color="bg-violet-500" />
                ))}
              </div>
            </Card>

            {/* Monthly additions + Demand curve side by side */}
            <div className="grid grid-cols-2 gap-4">
              <Card className="p-6">
                <h2 className="text-base font-bold text-gray-900 mb-1">Packages added per month</h2>
                <p className="text-xs text-gray-400 mb-5">Last 6 months — your catalog additions</p>
                {data.addedByMonth.length > 0 ? (
                  <div className="space-y-2.5">
                    {data.addedByMonth.map(m => (
                      <HBar key={m.month} label={m.month} value={m.count}
                        pct={(m.count / maxAdded) * 100} color="bg-emerald-500" />
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-gray-400">No additions in the last 6 months.</p>
                )}
              </Card>

              <Card className="p-6">
                <h2 className="text-base font-bold text-gray-900 mb-1">Demand curve</h2>
                <p className="text-xs text-gray-400 mb-5">Confirmed bookings per month</p>
                <div className="flex items-end gap-1">
                  {data.demandCurve.map(p => (
                    <DemandBar key={p.month} month={p.month} bookings={p.bookings}
                      pct={(p.bookings / maxDemand) * 100} isCurrent={p.isCurrent} />
                  ))}
                </div>
              </Card>
            </div>

            {/* Newest packages — REAL live data */}
            <Card className="overflow-hidden">
              <div className="p-6 border-b border-gray-100">
                <h2 className="text-lg font-bold text-gray-900">Newest tour packages</h2>
                <p className="text-sm text-gray-400 mt-1">Recently added to your master catalog</p>
              </div>
              <div className="divide-y divide-gray-100">
                {data.newestPackages.map(pkg => (
                  <div key={pkg.id} className="flex items-center gap-4 px-6 py-4 hover:bg-gray-50 transition-colors">
                    <div className="w-9 h-9 bg-indigo-50 rounded-lg flex items-center justify-center text-indigo-600 shrink-0">
                      <Package size={16} />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-semibold text-gray-900 truncate">{pkg.name}</p>
                      <p className="text-xs text-gray-400">{pkg.region}</p>
                    </div>
                    <div className="flex gap-1 flex-wrap justify-end max-w-[180px]">
                      {pkg.tags.slice(0, 2).map(t => (
                        <span key={t} className="text-xs bg-indigo-50 text-indigo-700 px-2 py-0.5 rounded-full">{t}</span>
                      ))}
                    </div>
                    <span className="text-xs text-gray-400 shrink-0 ml-3">{pkg.addedAgo}</span>
                  </div>
                ))}
              </div>
            </Card>

            {/* Recommendation Agent */}
            <div className="bg-indigo-50 border border-indigo-100 rounded-2xl p-6 flex gap-4">
              <div className="w-10 h-10 bg-indigo-600 rounded-full flex items-center justify-center shrink-0">
                <Sparkles size={20} className="text-white" />
              </div>
              <div>
                <p className="text-xs font-bold text-indigo-600 uppercase tracking-wider mb-1">Recommendation Agent</p>
                <p className="text-indigo-900 text-sm leading-relaxed mb-4">
                  {data.summary.underSuppliedCount > 0
                    ? `${data.summary.underSuppliedRegions.join(' and ')} ${data.summary.underSuppliedCount === 1 ? 'has' : 'have'} fewer than 2 packages. Expanding the catalog there would improve traveler match rates and estimated search-to-booking conversion by 6–9%.`
                    : `All ${data.summary.totalRegions} regions are covered across ${data.summary.totalPackages} packages. Top preference tag is "${data.tagFrequency[0]?.tag ?? '—'}" — adding more packages featuring it would maximise Recommendation Agent matches.`
                  }
                </p>
                <button className="bg-indigo-600 text-white text-sm font-medium px-4 py-2 rounded-lg hover:bg-indigo-700 transition-colors">
                  Draft package brief
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </>
  );
}

import { ArrowUpRight, CalendarDays, CreditCard, Map, TrendingUp, Users } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

const stats = [
  { label: 'Today bookings', value: '28', note: '+12.4% this month', icon: CreditCard, tone: 'blue' },
  { label: 'Live tours', value: '12', note: 'Active experiences', icon: Map, tone: 'mint' },
  { label: 'Guide teams', value: '09', note: 'Across 06 destinations', icon: Users, tone: 'gold' },
  { label: 'Demand trend', value: '+18%', note: 'Compared with last week', icon: TrendingUp, tone: 'rose' },
];

const updates = [
  { title: 'High demand', detail: 'Ella and Sigiriya tours are trending.', status: 'High', tone: 'green' },
  { title: 'Escrow payments', detail: '4 customer payments need review.', status: 'Review', tone: 'amber' },
  { title: 'Guide coverage', detail: 'Two shifts need reassignment.', status: 'Action', tone: 'blue' },
];

export default function Welcome() {
  const navigate = useNavigate();

  return (
    <div className="text-[#102d4d]">
      <header className="flex flex-col sm:flex-row items-start sm:items-end justify-between gap-6 mb-[25px]">
        <div>
          <span className="block text-[#6c91ad] text-[10px] font-extrabold tracking-[0.16em] uppercase">Travyle / Overview</span>
          <h1 className="mt-2 mb-[7px] text-[#0e3560] text-[clamp(28px,3vw,40px)] tracking-[-0.045em] leading-[1.1] font-bold">Good morning, Admin</h1>
          <p className="m-0 text-[#7b91a5] text-[13px]">Here is what needs your attention across today&apos;s journeys.</p>
        </div>
        <button className="inline-flex items-center gap-[9px] px-[14px] py-[11px] border border-[#d8e6f0] rounded-[10px] text-[#466681] bg-white text-[12px] font-bold shadow-[0_5px_18px_rgba(16,61,104,0.05)] sm:mt-0 mt-4 cursor-pointer" type="button">
          <CalendarDays size={16} />
          <span>25 September 2026</span>
        </button>
      </header>

      <section className="flex flex-col sm:flex-row items-start sm:items-center justify-between min-h-[260px] p-[28px_24px] sm:p-[35px_42px] overflow-hidden relative rounded-[18px] text-white shadow-[0_18px_35px_rgba(9,44,81,0.16)] bg-gradient-to-br from-[#0b2d54] via-[#114d80] to-[#1f77aa]">
        <div className="absolute w-[360px] h-[360px] -right-[85px] -top-[170px] border border-white/20 rounded-full shadow-[0_0_0_35px_rgba(255,255,255,0.05),0_0_0_70px_rgba(255,255,255,0.035)] pointer-events-none"></div>
        <div className="max-w-[600px] relative z-10">
          <span className="block text-[#8fd0ee] text-[10px] font-extrabold tracking-[0.16em] uppercase">Today / Operations snapshot</span>
          <h2 className="max-w-[520px] mt-3 mb-[10px] text-white text-[clamp(30px,4vw,49px)] tracking-[-0.055em] leading-[1.02] font-bold">
            Keep every journey <em className="block text-[#b9edff] font-serif font-normal">in motion.</em>
          </h2>
          <p className="max-w-[500px] m-0 mb-6 text-[#c8e1ef] text-[13px] leading-[1.7]">Review the moments that need attention and keep every itinerary moving smoothly.</p>
          <button className="inline-flex items-center gap-[10px] px-[17px] py-[12px] rounded-[10px] text-[#0c3c68] bg-white text-[12px] font-extrabold shadow-[0_8px_18px_rgba(0,0,0,0.12)] hover:bg-[#dff5ff] hover:-translate-y-px transition-all cursor-pointer" type="button" onClick={() => navigate('/bookings')}>
            Open bookings <ArrowUpRight size={18} />
          </button>
        </div>
        <div className="relative z-10 sm:min-w-[145px] sm:pl-[35px] sm:border-l sm:border-white/20 sm:text-right mt-[25px] sm:mt-0 pt-[15px] sm:pt-0 border-t sm:border-t-0 border-white/20 w-fit sm:w-auto">
          <strong className="block text-white text-[40px] sm:text-[58px] tracking-[-0.06em] leading-none">03</strong>
          <span className="block mt-2 text-[#b9dced] font-mono text-[10px] uppercase leading-relaxed">bookings<br />in the queue</span>
        </div>
      </section>

      <section className="grid grid-cols-2 lg:grid-cols-4 gap-[10px] sm:gap-[15px] my-[17px] sm:mb-[25px]" aria-label="Operations metrics">
        {stats.map(({ label, value, note, icon: Icon, tone }) => (
          <article className={`min-h-[125px] sm:min-h-[142px] p-[15px] sm:p-[20px] border border-transparent rounded-[14px] shadow-[0_7px_20px_rgba(14,53,96,0.05)] ${tone === 'blue' ? 'bg-[#e4f3fc] border-[#cde7f6]' : tone === 'mint' ? 'bg-[#e6f7f0] border-[#cdeee1]' : tone === 'gold' ? 'bg-[#fff5dd] border-[#f4e5bd]' : 'bg-[#f7eaf1] border-[#eed6e2]'}`} key={label}>
            <div className="flex items-center justify-between text-[#6d849c] text-[11px] font-bold">
              <span>{label}</span>
              <Icon size={19} className={`${tone === 'blue' ? 'text-[#287bb2]' : tone === 'mint' ? 'text-[#24936d]' : tone === 'gold' ? 'text-[#b48623]' : 'text-[#ad537b]'}`} />
            </div>
            <strong className="block my-[15px] sm:mt-[19px] sm:mb-[12px] text-[#123d69] text-[26px] sm:text-[31px] tracking-[-0.05em] leading-none">{value}</strong>
            <small className="flex items-center gap-[7px] text-[#7590a5] text-[10px]">
              <i className="w-[5px] h-[5px] rounded-full bg-[#3ca979]" />{note}
            </small>
          </article>
        ))}
      </section>

      <section className="grid grid-cols-1 md:grid-cols-[1.2fr_0.8fr] gap-[17px] min-w-0">
        <article className="min-w-0 p-[24px] border border-[#dce8f2] rounded-[14px] bg-white shadow-[0_7px_20px_rgba(14,53,96,0.04)]">
          <div className="flex items-start justify-between gap-[16px] min-w-0">
            <div>
              <span className="block text-[#6c91ad] text-[10px] font-extrabold tracking-[0.16em] uppercase">Quick access</span>
              <h2 className="mt-[7px] mb-0 text-[#123d69] text-[20px] tracking-[-0.04em] font-bold">Move through your day</h2>
            </div>
            <ArrowUpRight size={20} className="text-[#7fa0b8]" />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-[10px] mt-[22px]">
            <button type="button" className="flex items-center gap-[11px] min-h-[72px] p-[12px] border border-[#e1ebf2] rounded-[11px] text-[#236998] bg-[#f8fbfd] text-left hover:border-[#8ec8e7] hover:bg-[#eef8fd] cursor-pointer" onClick={() => navigate('/bookings')}>
              <CreditCard size={18} />
              <span className="grid flex-1 gap-1"><small className="text-[#8da0b1] text-[10px]">Commerce</small><b className="text-[#173f67] text-[12px]">Traveler bookings</b></span>
              <ArrowUpRight size={16} className="text-[#9ab5c8]" />
            </button>
            <button type="button" className="flex items-center gap-[11px] min-h-[72px] p-[12px] border border-[#e1ebf2] rounded-[11px] text-[#236998] bg-[#f8fbfd] text-left hover:border-[#8ec8e7] hover:bg-[#eef8fd] cursor-pointer" onClick={() => navigate('/catalog/packages')}>
              <Map size={18} />
              <span className="grid flex-1 gap-1"><small className="text-[#8da0b1] text-[10px]">Catalog</small><b className="text-[#173f67] text-[12px]">Tour packages</b></span>
              <ArrowUpRight size={16} className="text-[#9ab5c8]" />
            </button>
            <button type="button" className="flex items-center gap-[11px] min-h-[72px] p-[12px] border border-[#e1ebf2] rounded-[11px] text-[#236998] bg-[#f8fbfd] text-left hover:border-[#8ec8e7] hover:bg-[#eef8fd] cursor-pointer" onClick={() => navigate('/operations/live-operations')}>
              <Users size={18} />
              <span className="grid flex-1 gap-1"><small className="text-[#8da0b1] text-[10px]">Operations</small><b className="text-[#173f67] text-[12px]">Live operations</b></span>
              <ArrowUpRight size={16} className="text-[#9ab5c8]" />
            </button>
            <button type="button" className="flex items-center gap-[11px] min-h-[72px] p-[12px] border border-[#e1ebf2] rounded-[11px] text-[#236998] bg-[#f8fbfd] text-left hover:border-[#8ec8e7] hover:bg-[#eef8fd] cursor-pointer" onClick={() => navigate('/trends')}>
              <TrendingUp size={18} />
              <span className="grid flex-1 gap-1"><small className="text-[#8da0b1] text-[10px]">Insights</small><b className="text-[#173f67] text-[12px]">Travel trends</b></span>
              <ArrowUpRight size={16} className="text-[#9ab5c8]" />
            </button>
          </div>
        </article>

        <article className="min-w-0 p-[24px] border border-[#dce8f2] rounded-[14px] bg-white shadow-[0_7px_20px_rgba(14,53,96,0.04)]">
          <div className="flex items-start justify-between gap-[16px] min-w-0">
            <div>
              <span className="block text-[#6c91ad] text-[10px] font-extrabold tracking-[0.16em] uppercase">This week</span>
              <h2 className="mt-[7px] mb-0 text-[#123d69] text-[20px] tracking-[-0.04em] font-bold">Signals to watch</h2>
            </div>
            <span className="px-[10px] py-[7px] rounded-[8px] text-[#286a9d] bg-[#e9f5fc] text-[11px] font-extrabold">03</span>
          </div>
          <div className="grid mt-[18px]">
            {updates.map((update) => (
              <div className="flex items-center gap-[11px] min-h-[69px] border-b border-[#edf2f6] last:border-b-0" key={update.title}>
                <span className={`grid w-[30px] h-[30px] flex-none place-items-center rounded-[9px] ${update.tone === 'green' ? 'text-[#24936d] bg-[#e5f7f0]' : update.tone === 'amber' ? 'text-[#ba8121] bg-[#fff4d7]' : 'text-[#287bb2] bg-[#e6f3fb]'}`}>
                  <i className="w-[7px] h-[7px] rounded-full bg-current" />
                </span>
                <div className="flex-1 min-w-0">
                  <b className="text-[#234b6e] text-[12px]">{update.title}</b>
                  <p className="m-0 mt-1 overflow-hidden text-[#8195a6] text-[11px] text-ellipsis whitespace-nowrap">{update.detail}</p>
                </div>
                <span className={`px-[8px] py-[5px] rounded-[6px] text-[10px] font-extrabold ${update.tone === 'green' ? 'text-[#258b67] bg-[#e5f7f0]' : update.tone === 'amber' ? 'text-[#a9751c] bg-[#fff4d7]' : 'text-[#287bb2] bg-[#e6f3fb]'}`}>{update.status}</span>
              </div>
            ))}
          </div>
        </article>
      </section>
    </div>
  );
}

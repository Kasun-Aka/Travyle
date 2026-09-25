import { ArrowUpRight, CalendarDays, CreditCard, Map, TrendingUp, Users } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import './Welcome.css';

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
    <div className="welcome-page">
      <header className="welcome-header">
        <div>
          <span className="welcome-kicker">Travyle / Overview</span>
          <h1>Good morning, Admin</h1>
          <p>Here is what needs your attention across today&apos;s journeys.</p>
        </div>
        <button className="welcome-date" type="button">
          <CalendarDays size={16} />
          <span>25 September 2026</span>
        </button>
      </header>

      <section className="welcome-hero">
        <div className="hero-copy">
          <span className="welcome-kicker">Today / Operations snapshot</span>
          <h2>Keep every journey <em>in motion.</em></h2>
          <p>Review the moments that need attention and keep every itinerary moving smoothly.</p>
          <button className="hero-button" type="button" onClick={() => navigate('/bookings')}>
            Open bookings <ArrowUpRight size={18} />
          </button>
        </div>
        <div className="hero-total">
          <strong>03</strong>
          <span>bookings<br />in the queue</span>
        </div>
      </section>

      <section className="stats-grid" aria-label="Operations metrics">
        {stats.map(({ label, value, note, icon: Icon, tone }) => (
          <article className={`stat-card ${tone}`} key={label}>
            <div className="stat-topline"><span>{label}</span><Icon size={19} /></div>
            <strong>{value}</strong>
            <small><i />{note}</small>
          </article>
        ))}
      </section>

      <section className="welcome-lower-grid">
        <article className="welcome-panel">
          <div className="panel-heading-row">
            <div><span className="welcome-kicker">Quick access</span><h2>Move through your day</h2></div>
            <ArrowUpRight size={20} className="panel-arrow" />
          </div>
          <div className="quick-links">
            <button type="button" onClick={() => navigate('/bookings')}><CreditCard size={18} /><span><small>Commerce</small><b>Traveler bookings</b></span><ArrowUpRight size={16} /></button>
            <button type="button" onClick={() => navigate('/catalog/packages')}><Map size={18} /><span><small>Catalog</small><b>Tour packages</b></span><ArrowUpRight size={16} /></button>
            <button type="button" onClick={() => navigate('/operations/live-operations')}><Users size={18} /><span><small>Operations</small><b>Live operations</b></span><ArrowUpRight size={16} /></button>
            <button type="button" onClick={() => navigate('/trends')}><TrendingUp size={18} /><span><small>Insights</small><b>Travel trends</b></span><ArrowUpRight size={16} /></button>
          </div>
        </article>

        <article className="welcome-panel updates-panel">
          <div className="panel-heading-row"><div><span className="welcome-kicker">This week</span><h2>Signals to watch</h2></div><span className="updates-count">03</span></div>
          <div className="updates-list">
            {updates.map((update) => <div className="update-row" key={update.title}><span className={`update-icon ${update.tone}`}><i /></span><div><b>{update.title}</b><p>{update.detail}</p></div><span className={`update-status ${update.tone}`}>{update.status}</span></div>)}
          </div>
        </article>
      </section>
    </div>
  );
}

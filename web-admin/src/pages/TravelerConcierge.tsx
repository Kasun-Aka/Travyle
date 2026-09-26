import { useState, useEffect } from 'react';
import Layout from '../components/Layout';
import { Sparkles, Send, User, Map, CheckCircle2, Clock } from 'lucide-react';
import axios from 'axios';
import { destinationsApi, type Destination } from '../api/destinations';

interface NotificationHistory {
  id: string;
  pitch: string;
  isRead: boolean;
  sentAt: string;
  userEmail: string;
  userName: string;
  destinationName: string;
  destinationRegion: string;
}

export default function TravelerConcierge() {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<{ destination: Destination; reasoning: string } | null>(null);
  const [sent, setSent] = useState(false);
  const [history, setHistory] = useState<NotificationHistory[]>([]);

  const fetchHistory = async () => {
    try {
      const res = await axios.get('http://localhost:5085/api/notifications/admin');
      setHistory(res.data);
    } catch (err) {
      console.error("Failed to load history", err);
    }
  };

  useEffect(() => {
    fetchHistory();
  }, []);

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    setLoading(true);
    setError('');
    setResult(null);
    setSent(false);

    try {
      const agentResponse = await axios.post('http://localhost:5085/api/agent/recommend', {
        email: email.trim(),
      });

      const { destination, reasoning } = agentResponse.data;
      
      setResult({
        destination,
        reasoning,
      });
    } catch (err: any) {
      if (err.response?.status === 404) {
        setError('Traveler not found or has no preferences saved yet.');
      } else if (err.response?.status === 503) {
        setError(err.response?.data || 'Google AI servers are overloaded. Please try again.');
      } else {
        setError('Failed to generate recommendation. Please ensure the backend is running.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleNotify = async () => {
    if (!result || !email) return;
    try {
      await axios.post('http://localhost:5085/api/notifications', {
        email: email.trim(),
        destinationId: result.destination.id,
        pitch: result.reasoning
      });
      setSent(true);
      fetchHistory(); // Refresh the list
    } catch (err) {
      alert("Failed to send notification. Make sure the backend is running.");
    }
  };

  return (
    <Layout>
      <div className="max-w-4xl mx-auto">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 mb-4 tracking-tight uppercase flex items-center gap-3">
            <Sparkles className="text-brand-600" size={32} />
            AI Traveler Concierge
          </h1>
          <p className="text-gray-500 text-lg leading-relaxed max-w-2xl">
            Run the Recommendation Agent to analyze a traveler's saved preferences and generate a personalized tour package pitch for them.
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden mb-8">
          <div className="p-6 border-b border-gray-100 bg-gray-50">
            <h2 className="text-lg font-bold text-gray-900 mb-2">Look up traveler</h2>
            <form onSubmit={handleGenerate} className="flex gap-4">
              <div className="relative flex-1">
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <User size={20} className="text-gray-400" />
                </div>
                <input
                  type="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="e.g. vinod23@gmail.com"
                  className="block w-full pl-10 pr-3 py-3 border border-gray-300 rounded-xl focus:ring-2 focus:ring-brand-500 focus:border-brand-500 sm:text-sm"
                  required
                />
              </div>
              <button
                type="submit"
                disabled={loading || !email.trim()}
                className="bg-brand-600 text-white font-medium px-6 py-3 rounded-xl hover:bg-brand-700 transition-colors disabled:opacity-50 flex items-center gap-2 shadow-md shadow-brand-600/20"
              >
                {loading ? <span className="animate-pulse">Analyzing...</span> : 'Generate Pitch'}
                {!loading && <Sparkles size={18} />}
              </button>
            </form>
            {error && <p className="text-red-500 mt-3 text-sm font-medium">{error}</p>}
          </div>

          {result && (
            <div className="p-8">
              <div className="flex items-start gap-6">
                <div className="w-16 h-16 bg-brand-100 rounded-2xl flex items-center justify-center shrink-0">
                  <Map size={32} className="text-brand-600" />
                </div>
                <div className="flex-1">
                  <div className="flex justify-between items-start">
                    <div>
                      <span className="text-xs font-bold text-brand-600 uppercase tracking-wider bg-brand-50 px-2 py-1 rounded-md mb-2 inline-block">
                        AI Matched Destination
                      </span>
                      <h3 className="text-2xl font-bold text-gray-900 mb-1">{result.destination.name}</h3>
                      <p className="text-gray-500 mb-4">{result.destination.region}</p>
                    </div>
                    {sent ? (
                      <span className="flex items-center gap-2 text-emerald-600 font-bold bg-emerald-50 px-4 py-2 rounded-lg">
                        <CheckCircle2 size={20} /> Notification Sent
                      </span>
                    ) : (
                      <button 
                        onClick={handleNotify}
                        className="bg-gray-900 text-white font-medium px-4 py-2 rounded-lg hover:bg-black transition-colors flex items-center gap-2 shadow-sm"
                      >
                        <Send size={16} /> Notify to Traveler
                      </button>
                    )}
                  </div>

                  <div className="bg-gray-50 rounded-xl p-5 border border-gray-100 mb-6 mt-4">
                    <h4 className="text-sm font-bold text-gray-900 mb-2 flex items-center gap-2">
                      <Sparkles size={16} className="text-brand-600" />
                      Agent's Personalized Pitch
                    </h4>
                    <p className="text-gray-700 leading-relaxed italic">
                      "{result.reasoning}"
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* History Section */}
        <div>
          <h2 className="text-xl font-bold text-gray-900 mb-4 flex items-center gap-2">
            <Clock className="text-gray-400" size={24} />
            Sent Recommendations History
          </h2>
          {history.length === 0 ? (
            <div className="bg-white rounded-2xl border border-gray-200 p-8 text-center text-gray-500">
              No recommendations sent yet.
            </div>
          ) : (
            <div className="grid gap-4">
              {history.map((h) => (
                <div key={h.id} className="bg-white rounded-xl border border-gray-200 p-5 flex gap-4 shadow-sm hover:shadow-md transition-shadow">
                  <div className="w-12 h-12 bg-gray-100 rounded-full flex items-center justify-center shrink-0">
                    <User size={20} className="text-gray-500" />
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex justify-between items-start mb-1">
                      <h4 className="font-bold text-gray-900 truncate">{h.userName} <span className="text-gray-400 font-normal ml-1">({h.userEmail})</span></h4>
                      <span className="text-xs text-gray-400">{new Date(h.sentAt).toLocaleDateString()}</span>
                    </div>
                    <p className="text-sm text-brand-600 font-medium mb-2">Recommended: {h.destinationName} ({h.destinationRegion})</p>
                    <p className="text-sm text-gray-600 italic line-clamp-2">"{h.pitch}"</p>
                  </div>
                  <div className="shrink-0 flex items-center">
                    {h.isRead ? (
                      <span className="text-xs font-bold text-emerald-600 bg-emerald-50 px-2.5 py-1 rounded-full border border-emerald-200 flex items-center gap-1">
                        <CheckCircle2 size={12} /> Read
                      </span>
                    ) : (
                      <span className="text-xs font-bold text-amber-600 bg-amber-50 px-2.5 py-1 rounded-full border border-amber-200">
                        Pending
                      </span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </Layout>
  );
}

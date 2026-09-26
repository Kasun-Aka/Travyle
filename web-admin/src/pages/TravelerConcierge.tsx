import { useState, useEffect } from 'react';
import Layout from '../components/Layout';
import { Sparkles, Send, User, Map, CheckCircle2, Clock, ChevronDown, ChevronUp } from 'lucide-react';
import axios from 'axios';
import { type Destination } from '../api/destinations';

interface Traveler {
  email: string;
  fullName: string;
  preferences: string[];
  budget: string;
}

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
  const [travelers, setTravelers] = useState<Traveler[]>([]);
  const [isExpanded, setIsExpanded] = useState(false);



  const fetchTravelers = async () => {
    try {
      const res = await axios.get('http://localhost:5085/api/auth/travelers');
      setTravelers(res.data);
    } catch (err) {
      console.error("Failed to load travelers", err);
    }
  };
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
    fetchTravelers();
  }, []);

  const handleGenerate = async (targetEmail: string) => {
    if (!targetEmail.trim()) return;
    setEmail(targetEmail); // Keep it in state for the notification part

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
          <div className="p-6 border-b border-gray-100 bg-gray-50 flex items-center justify-between">
            <div>
              <h2 className="text-lg font-bold text-gray-900">Registered Travelers</h2>
              <p className="text-sm text-gray-500 mt-1">Select a traveler to generate a personalized AI pitch</p>
            </div>
            {loading && <div className="flex items-center gap-2 text-brand-600 font-bold animate-pulse"><Sparkles size={20} /> Analyzing Match...</div>}
            {error && <p className="text-red-500 text-sm font-medium">{error}</p>}
          </div>
          
          <div className={`p-6 ${isExpanded ? '' : 'overflow-x-auto'}`}>
            <div className={isExpanded ? "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4" : "flex gap-4 min-w-max pb-2"}>
              {travelers.length === 0 ? (
                <p className="text-gray-500 italic text-sm">No registered travelers found.</p>
              ) : (
                travelers.map(t => (
                  <div key={t.email} className={`${isExpanded ? 'w-full' : 'w-80 shrink-0'} rounded-xl border p-5 flex flex-col gap-4 transition-all ${email === t.email ? 'border-brand-500 bg-brand-50/30 ring-4 ring-brand-50' : 'border-gray-200 bg-white hover:border-gray-300 shadow-sm'}`}>
                    
                    {/* Header */}
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 rounded-full bg-gradient-to-br from-brand-400 to-brand-600 flex items-center justify-center text-white font-bold text-lg shadow-inner">
                        {t.fullName.charAt(0).toUpperCase()}
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="font-bold text-gray-900 truncate">{t.fullName}</h3>
                        <p className="text-xs text-gray-500 truncate">{t.email}</p>
                      </div>
                    </div>

                    {/* Preferences Tags */}
                    <div>
                      <p className="text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-2">Style Preferences</p>
                      <div className="flex flex-wrap gap-1.5">
                        {t.preferences.length > 0 ? (
                          t.preferences.slice(0, 3).map(p => (
                            <span key={p} className="px-2 py-1 bg-gray-100 text-gray-600 text-[10px] rounded-md font-medium">{p}</span>
                          ))
                        ) : (
                          <span className="text-xs text-gray-400 italic">No preferences saved</span>
                        )}
                        {t.preferences.length > 3 && (
                          <span className="px-2 py-1 bg-gray-100 text-gray-600 text-[10px] rounded-md font-medium">+{t.preferences.length - 3}</span>
                        )}
                      </div>
                    </div>

                    <button
                      onClick={() => handleGenerate(t.email)}
                      disabled={loading}
                      className="mt-auto w-full flex items-center justify-center gap-2 bg-white border border-gray-200 hover:border-brand-600 hover:text-brand-600 text-gray-700 font-medium py-2 rounded-lg text-sm transition-colors disabled:opacity-50"
                    >
                      <Sparkles size={16} />
                      Generate Pitch
                    </button>
                  </div>
                ))
              )}
            </div>
          </div>
          
          {/* Expand / Collapse Footer */}
          {travelers.length > 0 && (
            <div className="bg-gray-50 border-t border-gray-100 p-2 flex justify-end">
              <button
                type="button"
                onClick={() => setIsExpanded(!isExpanded)}
                className="flex items-center gap-1.5 text-xs font-bold text-gray-500 uppercase tracking-wider hover:text-brand-600 transition-colors px-4 py-2 rounded-lg hover:bg-gray-200/50"
              >
                {isExpanded ? 'View Less' : 'View All Travelers'}
                {isExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
              </button>
            </div>
          )}

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

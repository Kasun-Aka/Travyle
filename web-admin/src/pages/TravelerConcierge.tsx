import { useState } from 'react';
import Layout from '../components/Layout';
import { Sparkles, Send, User, Map, CheckCircle2 } from 'lucide-react';
import axios from 'axios';
import { destinationsApi, type Destination } from '../api/destinations';

export default function TravelerConcierge() {
  const [email, setEmail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<{ destination: Destination; reasoning: string } | null>(null);
  const [sent, setSent] = useState(false);

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;

    setLoading(true);
    setError('');
    setResult(null);
    setSent(false);

    try {
      // 1. Call the Recommendation Agent
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
                        <CheckCircle2 size={20} /> Pitch Sent
                      </span>
                    ) : (
                      <button 
                        onClick={() => setSent(true)}
                        className="bg-gray-900 text-white font-medium px-4 py-2 rounded-lg hover:bg-black transition-colors flex items-center gap-2"
                      >
                        <Send size={16} /> Email to Traveler
                      </button>
                    )}
                  </div>

                  <div className="bg-gray-50 rounded-xl p-5 border border-gray-100 mb-6">
                    <h4 className="text-sm font-bold text-gray-900 mb-2 flex items-center gap-2">
                      <Sparkles size={16} className="text-brand-600" />
                      Agent's Personalized Pitch
                    </h4>
                    <p className="text-gray-700 leading-relaxed italic">
                      "{result.reasoning}"
                    </p>
                  </div>

                  <div>
                    <h4 className="text-sm font-bold text-gray-900 mb-2">Package Included Tags</h4>
                    <div className="flex flex-wrap gap-2">
                      {result.destination.tags.map((tag: string) => (
                        <span key={tag} className="bg-white border border-gray-200 text-gray-600 text-xs font-medium px-2.5 py-1 rounded-md">
                          {tag}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </Layout>
  );
}

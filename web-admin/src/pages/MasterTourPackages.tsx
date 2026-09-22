
import Layout from '../components/Layout';
import { Plus, MapPin, Sparkles } from 'lucide-react';

export default function MasterTourPackages() {
  return (
    <Layout>
      <div className="max-w-6xl mx-auto space-y-8">
        
        {/* Page Header */}
        <div>
          <div className="flex items-center gap-2 text-xs font-bold text-brand-600 uppercase tracking-wider mb-2">
            <span className="w-1.5 h-1.5 rounded-full bg-brand-600"></span>
            Catalog
          </div>
          <h1 className="text-4xl font-bold text-gray-900 mb-4 tracking-tight">Master tour packages</h1>
          <p className="text-gray-500 max-w-3xl text-lg leading-relaxed mb-6">
            Create and maintain the destination catalog travelers browse in the mobile app, including geocoded coordinates, preference tags, and capacity.
          </p>
          <button className="bg-brand-600 hover:bg-brand-700 text-white px-6 py-2.5 rounded-lg font-medium flex items-center gap-2 transition-colors">
            <Plus size={20} />
            Create package
          </button>
        </div>

        {/* API Endpoint Chips */}
        <div className="flex flex-wrap gap-4 text-xs font-mono">
          <div className="flex items-center gap-3 bg-white px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
            <span className="text-blue-600 font-bold bg-blue-50 px-2 py-0.5 rounded">GET</span>
            <span className="text-gray-500">/api/destinations</span>
          </div>
          <div className="flex items-center gap-3 bg-white px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
            <span className="text-blue-600 font-bold bg-blue-50 px-2 py-0.5 rounded">GET</span>
            <span className="text-gray-500">/api/destinations/{'{id}'}</span>
          </div>
          <div className="flex items-center gap-3 bg-white px-4 py-2 rounded-lg border border-gray-200 shadow-sm">
            <span className="text-emerald-600 font-bold bg-emerald-50 px-2 py-0.5 rounded">POST</span>
            <span className="text-gray-500">/api/destinations/generate-itinerary-pdf</span>
          </div>
        </div>

        {/* Package Catalog Card */}
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="p-6 border-b border-gray-100 flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Package catalog</h2>
              <p className="text-sm text-gray-500">5 of 5 packages</p>
            </div>
            
            <div className="flex items-center gap-4">
              <input 
                type="text" 
                placeholder="Search name, region, or ID" 
                className="bg-gray-50 border border-gray-200 rounded-lg px-4 py-2 text-sm w-64 focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
              <div className="flex items-center bg-gray-50 rounded-lg p-1 border border-gray-200">
                <button className="px-4 py-1.5 text-sm font-medium bg-brand-600 text-white rounded-md shadow-sm">All</button>
                <button className="px-4 py-1.5 text-sm font-medium text-gray-500 hover:text-gray-700">Published</button>
                <button className="px-4 py-1.5 text-sm font-medium text-gray-500 hover:text-gray-700">Draft</button>
                <button className="px-4 py-1.5 text-sm font-medium text-gray-500 hover:text-gray-700">Paused</button>
              </div>
            </div>
          </div>

          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-gray-100 text-gray-400">
                <th className="font-semibold py-4 px-6">Master package</th>
                <th className="font-semibold py-4 px-6">Duration</th>
                <th className="font-semibold py-4 px-6">Base price</th>
                <th className="font-semibold py-4 px-6">Capacity</th>
                <th className="font-semibold py-4 px-6">Preference tags</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              
              {/* Package Row 1 */}
              <tr className="hover:bg-gray-50 bg-brand-50/30">
                <td className="py-4 px-6">
                  <div className="flex items-center gap-4">
                    <img src="https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=100&h=100&fit=crop" alt="Ella" className="w-12 h-12 rounded-lg object-cover shadow-sm" />
                    <div>
                      <p className="font-bold text-gray-900 text-base">Ella Highlands & Tea Trails</p>
                      <p className="text-xs text-gray-500 mt-0.5">PKG-1041 • Central Highlands</p>
                    </div>
                  </div>
                </td>
                <td className="py-4 px-6 text-gray-700">4 days</td>
                <td className="py-4 px-6 font-bold text-gray-900">$420</td>
                <td className="py-4 px-6">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-bold text-gray-900">19<span className="text-gray-400 font-normal">/24</span></span>
                    <span className="text-xs text-gray-500">seats</span>
                  </div>
                  <div className="w-24 h-1.5 bg-gray-200 rounded-full overflow-hidden">
                    <div className="bg-brand-600 h-full w-[79%]"></div>
                  </div>
                </td>
                <td className="py-4 px-6">
                  <div className="flex gap-2">
                    <span className="px-2 py-1 bg-white border border-gray-200 rounded text-xs text-gray-600">Cool weather</span>
                    <span className="px-2 py-1 bg-white border border-gray-200 rounded text-xs text-gray-600">Hiking</span>
                  </div>
                </td>
              </tr>

              {/* Package Row 2 */}
              <tr className="hover:bg-gray-50">
                <td className="py-4 px-6">
                  <div className="flex items-center gap-4">
                    <img src="https://images.unsplash.com/photo-1577717903315-1691ae25ab3f?w=100&h=100&fit=crop" alt="Galle" className="w-12 h-12 rounded-lg object-cover shadow-sm opacity-90" />
                    <div>
                      <p className="font-bold text-gray-900 text-base">Galle Fort Coastal Loop</p>
                      <p className="text-xs text-gray-500 mt-0.5">PKG-1038 • Southern Coast</p>
                    </div>
                  </div>
                </td>
                <td className="py-4 px-6 text-gray-700">2 days</td>
                <td className="py-4 px-6 font-bold text-gray-900">$260</td>
                <td className="py-4 px-6">
                  <div className="flex items-center gap-2 mb-1 justify-between w-24">
                    <div className="flex items-center gap-1">
                      <span className="font-bold text-gray-900">30<span className="text-gray-400 font-normal">/30</span></span>
                      <span className="text-xs text-gray-500">seats</span>
                    </div>
                    <span className="text-[10px] font-bold text-amber-600 uppercase tracking-wider">Full</span>
                  </div>
                  <div className="w-24 h-1.5 bg-gray-200 rounded-full overflow-hidden">
                    <div className="bg-amber-500 h-full w-full"></div>
                  </div>
                </td>
                <td className="py-4 px-6">
                  <div className="flex gap-2">
                    <span className="px-2 py-1 bg-white border border-gray-200 rounded text-xs text-gray-600">Beach</span>
                    <span className="px-2 py-1 bg-white border border-gray-200 rounded text-xs text-gray-600">Food</span>
                  </div>
                </td>
              </tr>

              {/* Package Row 3 */}
              <tr className="hover:bg-gray-50">
                <td className="py-4 px-6">
                  <div className="flex items-center gap-4">
                    <img src="https://images.unsplash.com/photo-1516426122078-c23e76319801?w=100&h=100&fit=crop" alt="Yala" className="w-12 h-12 rounded-lg object-cover shadow-sm" />
                    <div>
                      <p className="font-bold text-gray-900 text-base">Yala Wildlife Expedition</p>
                      <p className="text-xs text-gray-500 mt-0.5">PKG-1035 • Deep South</p>
                    </div>
                  </div>
                </td>
                <td className="py-4 px-6 text-gray-700">3 days</td>
                <td className="py-4 px-6 font-bold text-gray-900">$510</td>
                <td className="py-4 px-6">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="font-bold text-gray-900">11<span className="text-gray-400 font-normal">/16</span></span>
                    <span className="text-xs text-gray-500">seats</span>
                  </div>
                  <div className="w-24 h-1.5 bg-gray-200 rounded-full overflow-hidden">
                    <div className="bg-brand-600 h-full w-[68%]"></div>
                  </div>
                </td>
                <td className="py-4 px-6">
                  <div className="flex gap-2">
                    <span className="px-2 py-1 bg-white border border-gray-200 rounded text-xs text-gray-600">Safari</span>
                    <span className="px-2 py-1 bg-white border border-gray-200 rounded text-xs text-gray-600">Dry zone</span>
                  </div>
                </td>
              </tr>

            </tbody>
          </table>
        </div>

        {/* Selected Package Detail Section */}
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="p-6 border-b border-gray-100 flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Package detail</h2>
              <p className="text-sm text-gray-500">PKG-1041</p>
            </div>
            <span className="px-3 py-1 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded-full text-xs font-bold uppercase tracking-wider">
              Published
            </span>
          </div>

          <div className="p-6">
            <div className="w-full h-48 rounded-xl overflow-hidden mb-8">
              <img src="https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=1200&h=400&fit=crop" alt="Ella Cover" className="w-full h-full object-cover" />
            </div>

            <h3 className="text-2xl font-bold text-gray-900 mb-6">Ella Highlands & Tea Trails</h3>

            <div className="space-y-4 mb-8">
              <div className="flex justify-between py-3 border-b border-gray-100">
                <span className="text-gray-500 text-sm">Region</span>
                <span className="font-medium text-gray-900">Central Highlands</span>
              </div>
              <div className="flex justify-between py-3 border-b border-gray-100">
                <span className="text-gray-500 text-sm">Duration</span>
                <span className="font-medium text-gray-900">4 days</span>
              </div>
              <div className="flex justify-between py-3 border-b border-gray-100">
                <span className="text-gray-500 text-sm">Base price</span>
                <span className="font-medium text-gray-900">$420 per traveler</span>
              </div>
              <div className="flex justify-between py-3 border-b border-gray-100">
                <span className="text-gray-500 text-sm">Capacity</span>
                <span className="font-medium text-gray-900">19 booked of 24</span>
              </div>
              <div className="flex justify-between py-3">
                <span className="text-gray-500 text-sm">Last updated</span>
                <span className="font-medium text-gray-900">2 hrs ago</span>
              </div>
            </div>

            {/* Map Preview Area */}
            <div className="bg-gray-50 border border-gray-200 rounded-xl p-4 mb-8">
              <div className="flex items-center gap-2 mb-3">
                <span className="text-xs font-bold text-gray-500 uppercase tracking-wider">Google Maps geocoding</span>
              </div>
              <div className="flex items-center gap-2 mb-4">
                <MapPin size={16} className="text-brand-600" />
                <span className="font-mono text-sm text-gray-700">6.8667, 81.0466</span>
              </div>
              <div className="w-full h-32 bg-gray-200 rounded-lg flex items-center justify-center border border-gray-300">
                <span className="text-gray-400 text-sm font-medium">Map preview placeholder</span>
              </div>
            </div>

            <div className="flex gap-4">
              <button className="px-6 py-2 bg-white border border-gray-300 text-gray-700 font-medium rounded-lg hover:bg-gray-50 transition-colors">
                Edit package
              </button>
              <button className="px-6 py-2 bg-white border border-transparent text-gray-600 font-medium rounded-lg hover:bg-gray-50 transition-colors">
                Preview PDF pass
              </button>
            </div>
          </div>
        </div>

        {/* AI Recommendation Box */}
        <div className="bg-brand-50 border border-brand-100 rounded-2xl p-6 flex gap-4">
          <div className="w-10 h-10 bg-brand-600 rounded-full flex items-center justify-center shrink-0">
            <Sparkles size={20} className="text-white" />
          </div>
          <div>
            <h4 className="text-sm font-bold text-brand-900 mb-1">Recommendation Agent</h4>
            <p className="text-brand-800 leading-relaxed mb-4">
              Ella Highlands & Tea Trails matches 91% of "cool weather + hiking" profiles under $450. Adding a scenic rail tag would surface it to an additional 340 travelers this month.
            </p>
            <button className="bg-white border border-brand-200 text-brand-700 font-medium px-4 py-2 rounded-lg text-sm hover:bg-brand-50 transition-colors">
              Apply suggested tags
            </button>
          </div>
        </div>

      </div>
    </Layout>
  );
}

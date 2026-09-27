import { useState, useEffect } from 'react';
import { X, Loader2 } from 'lucide-react';
import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import { destinationsApi, type Destination, type CreateDestinationPayload } from '../api/destinations';

// Fix Leaflet's default icon paths
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

interface Props {
  /** If provided, we are editing; otherwise, creating */
  destination?: Destination | null;
  onClose: () => void;
  onSaved: () => void;
}

const EMPTY_FORM: CreateDestinationPayload = {
  name: '',
  region: '',
  description: '',
  tags: [],
  imageUrl: '',
  latitude: undefined,
  longitude: undefined,
};


function LocationMarker({ position, setPosition }: { position: [number, number], setPosition: (lat: number, lng: number) => void }) {
  const map = useMapEvents({
    click(e) {
      setPosition(e.latlng.lat, e.latlng.lng);
      map.flyTo(e.latlng, map.getZoom());
    },
  });

  return position[0] !== 0 ? (
    <Marker position={position}></Marker>
  ) : null;
}

export default function DestinationFormModal({ destination, onClose, onSaved }: Props) {
  const isEditing = !!destination;
  const [form, setForm] = useState<CreateDestinationPayload>(EMPTY_FORM);
  const [tagInput, setTagInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (destination) {
      setForm({
        name: destination.name,
        region: destination.region,
        description: destination.description,
        tags: destination.tags,
        imageUrl: destination.imageUrl,
        latitude: destination.latitude,
        longitude: destination.longitude,
      });
      setTagInput(destination.tags.join(', '));
    } else {
      setForm(EMPTY_FORM);
      setTagInput('');
    }
  }, [destination]);

  const handleTagInputChange = (value: string) => {
    setTagInput(value);
    const tags = value
      .split(',')
      .map((t) => t.trim())
      .filter(Boolean);
    setForm((f) => ({ ...f, tags }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!form.name.trim() || !form.region.trim()) {
      setError('Name and Region are required.');
      return;
    }

    setLoading(true);
    try {
      if (isEditing && destination) {
        await destinationsApi.update(destination.id, form);
      } else {
        await destinationsApi.create(form);
      }
      onSaved();
      onClose();
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred.';
      setError(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg max-h-[90vh] flex flex-col">
        {/* Modal Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-100 shrink-0">
          <h2 className="text-xl font-bold text-gray-900">
            {isEditing ? 'Edit destination' : 'Create destination'}
          </h2>
          <button
            onClick={onClose}
            className="p-2 rounded-lg hover:bg-gray-100 text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X size={20} />
          </button>
        </div>

        {/* Modal Body */}
        <form onSubmit={handleSubmit} className="overflow-y-auto flex-1">
          <div className="p-6 space-y-5">
            {error && (
              <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
                {error}
              </div>
            )}

            {/* Name */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                Destination name <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="e.g. Ella Highlands & Tea Trails"
                className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                required
              />
            </div>

            {/* Region */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                Region <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                value={form.region}
                onChange={(e) => setForm((f) => ({ ...f, region: e.target.value }))}
                placeholder="e.g. Central Highlands"
                className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                required
              />
            </div>

            {/* Description */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                Description
              </label>
              <textarea
                value={form.description}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                placeholder="Write a brief description for travelers..."
                rows={3}
                className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none"
              />
            </div>

            {/* Tags */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                Preference tags
              </label>
              <input
                type="text"
                value={tagInput}
                onChange={(e) => handleTagInputChange(e.target.value)}
                placeholder="e.g. Hiking, Cool weather, Tea (comma-separated)"
                className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
              {form.tags.length > 0 && (
                <div className="flex flex-wrap gap-1.5 mt-2">
                  {form.tags.map((tag) => (
                    <span
                      key={tag}
                      className="px-2 py-0.5 bg-brand-50 border border-brand-200 text-brand-700 rounded text-xs font-medium"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              )}
            </div>

            {/* Image URL */}
            <div>
              <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                Image URL
              </label>
              <input
                type="url"
                value={form.imageUrl}
                onChange={(e) => setForm((f) => ({ ...f, imageUrl: e.target.value }))}
                placeholder="https://..."
                className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
            </div>

            {/* Coordinates */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                  Latitude
                </label>
                <input
                  type="number"
                  step="any"
                  value={form.latitude ?? ''}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      latitude: e.target.value === '' ? undefined : parseFloat(e.target.value),
                    }))
                  }
                  placeholder="6.8667"
                  className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>
              <div>
                <label className="block text-sm font-semibold text-gray-700 mb-1.5">
                  Longitude
                </label>
                <input
                  type="number"
                  step="any"
                  value={form.longitude ?? ''}
                  onChange={(e) =>
                    setForm((f) => ({
                      ...f,
                      longitude: e.target.value === '' ? undefined : parseFloat(e.target.value),
                    }))
                  }
                  placeholder="81.0466"
                  className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                />
              </div>
            </div>
              {/* Map Preview */}
              <div className="mt-4 border border-gray-200 rounded-xl overflow-hidden h-48 relative z-0">
                <MapContainer 
                  center={[form.latitude || 7.8731, form.longitude || 80.7718]} 
                  zoom={form.latitude && form.longitude ? 10 : 7} 
                  style={{ height: '100%', width: '100%' }}
                >
                  <TileLayer
                    url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
                    attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                  />
                  <LocationMarker 
                    position={[form.latitude || 0, form.longitude || 0]} 
                    setPosition={(lat, lng) => setForm(f => ({ ...f, latitude: lat, longitude: lng }))}
                  />
                </MapContainer>
                <div className="absolute top-2 left-2 z-[40] bg-white/90 backdrop-blur-sm px-3 py-1.5 rounded-lg text-xs font-semibold text-gray-700 shadow-sm border border-gray-200 pointer-events-none">
                  ?? Click map to set coordinates (or leave 0 to auto-geocode)
                </div>
              </div>
          </div>

          {/* Modal Footer */}
          <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-100 shrink-0">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="px-5 py-2.5 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-5 py-2.5 text-sm font-medium text-white bg-brand-600 hover:bg-brand-700 rounded-lg transition-colors disabled:opacity-50 flex items-center gap-2"
            >
              {loading && <Loader2 size={16} className="animate-spin" />}
              {isEditing ? 'Save changes' : 'Create destination'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

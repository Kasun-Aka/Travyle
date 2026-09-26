import re

with open("web-admin/src/components/DestinationFormModal.tsx", "r", encoding="utf-8") as f:
    text = f.read()

# 1. Imports
imports = """import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';

// Fix Leaflet's default icon paths
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

"""

if "MapContainer" not in text:
    text = text.replace("import { destinationsApi", imports + "import { destinationsApi")


# 2. Map click component
map_click_component = """
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
"""

if "function LocationMarker" not in text:
    text = text.replace("export default function DestinationFormModal", map_click_component + "\nexport default function DestinationFormModal")


# 3. Add Map to the form
# It goes right after the grid of coordinates
map_jsx = """
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
                <div className="absolute top-2 left-2 z-[400] bg-white/90 backdrop-blur-sm px-3 py-1.5 rounded-lg text-xs font-semibold text-gray-700 shadow-sm border border-gray-200 pointer-events-none">
                  ?? Click map to set coordinates (or leave 0 to auto-geocode)
                </div>
              </div>
"""

old_coords = """                <div>
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
                    placeholder="80.7718"
                    className="w-full border border-gray-200 rounded-lg px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
                  />
                </div>
              </div>"""

new_coords = old_coords + "\n" + map_jsx

if "MapContainer" not in text or "Map Preview" not in text:
    # Need to match strictly or use find/replace carefully
    text = text.replace(old_coords, new_coords)

with open("web-admin/src/components/DestinationFormModal.tsx", "w", encoding="utf-8") as f:
    f.write(text)

print("Injected map into DestinationFormModal")

import re

with open("web-admin/src/components/DestinationFormModal.tsx", "r", encoding="utf-8") as f:
    text = f.read()

pattern = r'(placeholder="81\.0466"[^>]*>\s*</div>\s*</div>)'

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
                <div className="absolute top-2 left-2 z-[40] bg-white/90 backdrop-blur-sm px-3 py-1.5 rounded-lg text-xs font-semibold text-gray-700 shadow-sm border border-gray-200 pointer-events-none">
                  ?? Click map to set coordinates (or leave 0 to auto-geocode)
                </div>
              </div>"""

match = re.search(pattern, text)
if match and "Map Preview" not in text:
    text = text[:match.end()] + map_jsx + text[match.end():]
    with open("web-admin/src/components/DestinationFormModal.tsx", "w", encoding="utf-8") as f:
        f.write(text)
    print("Injected map JSX")
else:
    print("Could not find insertion point!")

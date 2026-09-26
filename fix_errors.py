import re

# 1. Fix MasterTourPackages.tsx missing useRef
with open("web-admin/src/pages/MasterTourPackages.tsx", "r", encoding="utf-8") as f:
    text1 = f.read()

text1 = text1.replace("import { useState, useEffect, useCallback } from 'react';", "import { useState, useEffect, useCallback, useRef } from 'react';")

with open("web-admin/src/pages/MasterTourPackages.tsx", "w", encoding="utf-8") as f:
    f.write(text1)

# 2. Fix DestinationFormModal.tsx import ordering
with open("web-admin/src/components/DestinationFormModal.tsx", "r", encoding="utf-8") as f:
    text2 = f.read()

# I injected:
# import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
# import 'leaflet/dist/leaflet.css';
# import L from 'leaflet';
#
# // Fix Leaflet's default icon paths
# delete (L.Icon.Default.prototype as any)._getIconUrl;
# L.Icon.Default.mergeOptions({ ... });
# 
# import { destinationsApi, type Destination, type CreateDestinationPayload } from '../api/destinations';

# We need to move the executable code AFTER the imports.
old_import_block = """import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';

// Fix Leaflet's default icon paths
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});

import { destinationsApi, type Destination, type CreateDestinationPayload } from '../api/destinations';"""

new_import_block = """import { MapContainer, TileLayer, Marker, useMapEvents } from 'react-leaflet';
import 'leaflet/dist/leaflet.css';
import L from 'leaflet';
import { destinationsApi, type Destination, type CreateDestinationPayload } from '../api/destinations';

// Fix Leaflet's default icon paths
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.7.1/images/marker-shadow.png',
});"""

text2 = text2.replace(old_import_block, new_import_block)

with open("web-admin/src/components/DestinationFormModal.tsx", "w", encoding="utf-8") as f:
    f.write(text2)

print("Fixed syntax and compilation errors")

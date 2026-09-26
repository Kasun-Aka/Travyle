with open("web-admin/src/App.tsx", "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace("import React from 'react';\n", "")
with open("web-admin/src/App.tsx", "w", encoding="utf-8") as f:
    f.write(text)

with open("web-admin/src/pages/TravelerConcierge.tsx", "r", encoding="utf-8") as f:
    text = f.read()
text = text.replace("import { destinationsApi, type Destination } from '../api/destinations';", "import { type Destination } from '../api/destinations';")
with open("web-admin/src/pages/TravelerConcierge.tsx", "w", encoding="utf-8") as f:
    f.write(text)

print("Fixed warnings")

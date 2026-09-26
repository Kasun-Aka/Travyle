import re

with open("web-admin/src/pages/MasterTourPackages.tsx", "r", encoding="utf-8") as f:
    text = f.read()

# Find the start of the Package detail section
pattern = r'(<div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">)(\s*<div className="p-6 border-b border-gray-100 flex items-center justify-between">\s*<div>\s*<h2 className="text-xl font-bold text-gray-900">Package detail</h2>)'
replacement = r'<div ref={detailRef} className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">\2'

text = re.sub(pattern, replacement, text)

with open("web-admin/src/pages/MasterTourPackages.tsx", "w", encoding="utf-8") as f:
    f.write(text)

print("Injected ref={detailRef}")

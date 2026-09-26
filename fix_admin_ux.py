import re

with open("web-admin/src/pages/MasterTourPackages.tsx", "r", encoding="utf-8") as f:
    text = f.read()

# 1. Add useRef to the imports
if "useRef" not in text:
    text = text.replace("import { useState, useEffect } from 'react';", "import { useState, useEffect, useRef } from 'react';")

# 2. Add the ref to the component state
ref_line = "  const detailRef = useRef<HTMLDivElement>(null);\n"
if "const detailRef" not in text:
    text = text.replace("  const [destinations, setDestinations]", ref_line + "  const [destinations, setDestinations]")

# 3. Create the select handler
handler = """
  const handleSelectDestination = (dest: Destination) => {
    setSelected(dest);
    // Add a tiny timeout to ensure it renders before scrolling if on mobile, though usually instant is fine
    setTimeout(() => {
      detailRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 100);
  };
"""
if "handleSelectDestination" not in text:
    text = text.replace("  const fetchDestinations", handler + "\n  const fetchDestinations")

# 4. Replace onClick={() => setSelected(dest)} with onClick={() => handleSelectDestination(dest)}
text = text.replace("onClick={() => setSelected(dest)}", "onClick={() => handleSelectDestination(dest)}")

# 5. Attach the ref to the detail container
# Search for the Package detail block: 
# <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
#           <div className="p-6 border-b border-gray-100 flex items-center justify-between">
#               <h2 className="text-xl font-bold text-gray-900">Package detail</h2>
old_detail = """        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="p-6 border-b border-gray-100 flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Package detail</h2>"""

new_detail = """        <div ref={detailRef} className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="p-6 border-b border-gray-100 flex items-center justify-between">
            <div>
              <h2 className="text-xl font-bold text-gray-900">Package detail</h2>"""

if "ref={detailRef}" not in text:
    text = text.replace(old_detail, new_detail)

with open("web-admin/src/pages/MasterTourPackages.tsx", "w", encoding="utf-8") as f:
    f.write(text)

print("Updated MasterTourPackages.tsx with auto-scroll UX")

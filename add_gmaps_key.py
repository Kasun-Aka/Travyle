import json

with open("backend/appsettings.Development.json", "r", encoding="utf-8") as f:
    config = json.load(f)

config["GoogleMaps"] = {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY_HERE"
}

with open("backend/appsettings.Development.json", "w", encoding="utf-8") as f:
    json.dump(config, f, indent=4)

print("Added GoogleMaps API Key to config")

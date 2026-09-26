import urllib.request
import urllib.parse
import json

address = "Sigiriya Rock Fortress, Central Province"
encoded = urllib.parse.quote(address)
url = f"https://nominatim.openstreetmap.org/search?q={encoded}&format=json&limit=1"

req = urllib.request.Request(url, headers={'User-Agent': 'Travyle-UniversityProject/1.0'})
try:
    with urllib.request.urlopen(req) as response:
        res = response.read()
        print("Result for full string:", json.loads(res))
except Exception as e:
    print(f"Error: {e}")

address2 = "Sigiriya, Central Province"
encoded2 = urllib.parse.quote(address2)
url2 = f"https://nominatim.openstreetmap.org/search?q={encoded2}&format=json&limit=1"
req2 = urllib.request.Request(url2, headers={'User-Agent': 'Travyle-UniversityProject/1.0'})
try:
    with urllib.request.urlopen(req2) as response:
        res = response.read()
        print("Result for shorter string:", json.loads(res))
except Exception as e:
    print(f"Error: {e}")

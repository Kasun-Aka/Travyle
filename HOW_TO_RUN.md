# Travyle — How to Run the Project

Use this guide to run the API, AI booking agent, web admin, and mobile app locally.
Commands below are for Windows PowerShell and assume you have cloned the repository.

---

## Prerequisites (One-Time Setup)

### 1. Install the .NET 10 SDK
- Download the Windows x64 SDK from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- Verify in a new terminal:
  ```
  dotnet --version
  ```
  It should show `10.x.x`.

### 2. Install Node.js
```powershell
node --version
npm --version
```
Node.js 22 and npm 10 or newer are recommended.

### 3. Install Python
Install Python 3.10 or newer and verify it is available:
```powershell
python --version
```

### 4. Install Flutter
```powershell
flutter --version
flutter doctor
```
Flutter Doctor lists anything else needed for Android development. If `flutter` is not recognized, add the Flutter SDK `bin` folder to your Windows PATH.

### 5. Get project configuration
The API needs a Supabase PostgreSQL connection and Firebase Admin credentials. The web admin and mobile app also need Firebase client configuration. Ask the project maintainer for access and configuration values; do not commit credentials.

Copy the backend example file and fill in its placeholders:
```powershell
Copy-Item backend\appsettings.Development.json.example backend\appsettings.Development.json
```

Copy the web admin example and fill in the Firebase web values:
```powershell
Copy-Item web-admin\.env.example web-admin\.env.local
```

For mobile, configure `mobile\lib\firebase_options.dart` with the project's Firebase configuration. Keep these local configuration files out of commits.


## Run the Project

Open four separate PowerShell / Windows Terminal tabs from the repository root. Start the backend and agent before using features that depend on them.

---

### Tab 1 — Backend API (ASP.NET Core)

```powershell
cd backend

# First run (or after dependency changes)
dotnet restore

# Start with hot reload
dotnet watch run
```

- API: **http://localhost:5085**
- Swagger UI: **http://localhost:5085/swagger**
- Press `Ctrl+C` to stop

The API applies database migrations and seeds initial data when it starts. A reachable, correctly configured Supabase database is required.

---

### Tab 2 — AI Booking Agent (FastAPI + LangGraph)

```powershell
cd backend\agent

# Create and activate a virtual environment (first time only)
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt

# Start the agent
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

- Agent: **http://localhost:8000**
- Interactive API docs: **http://localhost:8000/docs**
- Press `Ctrl+C` to stop

---

### Tab 3 — Web Admin (React + Vite)

```powershell
cd web-admin

# First run (or after dependency changes)
npm install

# Start the dev server
npm run dev
```

- Web admin: **http://localhost:5173**
- Press `Ctrl+C` to stop

The web app uses `http://localhost:5085/api` for the API by default. Set `VITE_API_URL` in `.env.local` only if using a different API address.

---

### Tab 4 — Mobile App (Flutter)

```powershell
cd mobile

# First run (or after dependency changes)
flutter pub get

# List connected devices
flutter devices

# Run on connected Android device or emulator
flutter run

# OR run on a specific device
flutter run -d <device-id>
```

The Android emulator is configured to access the API at `http://10.0.2.2:5085`. For an iOS simulator or physical device, use an API address reachable from that device if needed.

---

## Geocoding

Destination geocoding uses OpenStreetMap Nominatim and does not require an API key.

---

## Git — Team Workflow

```powershell
# Check the current branch and working tree
git status

# Create or switch to your own feature branch
git switch -c <your-branch-name>

# Stage and commit your changes
git add .
git commit -m "feat: describe what you did"
git push -u origin <your-branch-name>
```

Open a pull request to merge your branch according to the team's repository policy. Do not commit local configuration or secrets.

---

## Quick Status Check — Is Everything Working?

```powershell
# Run these from the repository root
git branch --show-current

# Check .NET SDK
dotnet --version

# Check Node
node --version

# Check Flutter
flutter --version

# Check Python
python --version
```

---

## Config Files Reference (DO NOT COMMIT THESE)

| File | Purpose | Gitignored? |
|---|---|---|
| `backend\appsettings.Development.json` | Supabase DB + Firebase Admin SDK credentials | ✅ Yes |
| `web-admin\.env.local` | Firebase web config and optional API URL | ✅ Yes |
| `mobile\lib\firebase_options.dart` | Firebase config for Flutter | ✅ Yes |

---

## Useful URLs

| Service | URL |
|---|---|
| Backend API | http://localhost:5085 |
| Swagger UI (API docs) | http://localhost:5085/swagger |
| AI Agent docs | http://localhost:8000/docs |
| Web Admin (React) | http://localhost:5173 |
| Supabase Dashboard | https://supabase.com/dashboard |
| Firebase Console | https://console.firebase.google.com/project/travyle |
| Nominatim (geocoding) | https://nominatim.openstreetmap.org/ |


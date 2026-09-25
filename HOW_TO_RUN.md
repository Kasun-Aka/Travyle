# Travyle — How to Run the Project on This Machine
### Component: User & Travel Plan Management | IT24104383

> Keep this file handy. Every time you open your laptop to work on the project,
> follow the sections below in order.

---

## Prerequisites (One-Time Setup)

### 1. Install .NET 10 SDK  ← **YOU NEED TO DO THIS FIRST**
- Download from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- Choose: **Windows x64 — SDK installer**
- After install, verify in a new terminal:
  ```
  dotnet --version
  ```
  Should show `10.x.x`

### 2. Verify Node.js (already installed ✅)
```powershell
node --version    # should show v22.x.x
npm --version     # should show 10.x.x
```

### 3. Verify Flutter (check if it works)
```powershell
flutter --version
flutter doctor    # shows what is missing for Android dev
```
If `flutter` is not recognized, add the Flutter SDK `bin` folder to your Windows PATH.


### 4. Geocoding — No Setup Needed ✅
This project uses **OpenStreetMap Nominatim** for geocoding (converting destination names/addresses to lat/lng coordinates).
- **Free forever. No API key. No billing. No account.**
- Works out of the box — nothing to configure.

---


## Daily Workflow — Starting the Project

Open **3 separate PowerShell / Windows Terminal tabs**.

---

### Tab 1 — Backend (ASP.NET Core API)

```powershell
cd "d:\UNI Projects\Travyle\backend"

# First time only — restore packages
dotnet restore

# Run the backend (hot-reload enabled)
dotnet watch run
```

- API will be available at: **https://localhost:5001**
- Swagger UI at: **https://localhost:5001/swagger**
- Press `Ctrl+C` to stop

#### First time only — run database migration (after .NET 10 is installed):
```powershell
cd "d:\UNI Projects\Travyle\backend"
dotnet tool install --global dotnet-ef       # install EF CLI (one-time)
dotnet ef database update                    # applies migrations to Supabase
```

---

### Tab 2 — Web Admin (React + Vite)

```powershell
cd "d:\UNI Projects\Travyle\web-admin"

# First time only — install dependencies
npm install

# Start the dev server
npm run dev
```

- Web Admin will be available at: **http://localhost:5173**
- Press `Ctrl+C` to stop

---

### Tab 3 — Mobile App (Flutter)

```powershell
cd "d:\UNI Projects\Travyle\mobile"

# First time only — get packages
flutter pub get

# List connected devices
flutter devices

# Run on connected Android device or emulator
flutter run

# OR run on a specific device
flutter run -d <device-id>
```

---

## Git — Working on Your Branch

```powershell
cd "d:\UNI Projects\Travyle"

# Make sure you're on your branch (always work here)
git checkout IT24104383

# Pull latest changes from your branch
git pull origin IT24104383

# Get latest updates from the development branch (team changes)
git fetch origin development
git merge origin/development   # merge team changes into your branch

# Stage and commit your work
git add .
git commit -m "feat: describe what you did"

# Push your changes
git push origin IT24104383
```

> Never push directly to `development` or `production`. Always push to `IT24104383`.

---

## Quick Status Check — Is Everything Working?

```powershell
# Check git branch
git -C "d:\UNI Projects\Travyle" branch

# Check .NET SDK
dotnet --version

# Check Node
node --version

# Check Flutter
flutter --version
```

---

## Config Files Reference (DO NOT COMMIT THESE)

| File | Purpose | Gitignored? |
|---|---|---|
| `backend\appsettings.Development.json` | Supabase DB + Firebase Admin SDK credentials | ✅ Yes |
| `web-admin\.env.local` | Firebase web config for React app | ✅ Yes |
| `mobile\lib\firebase_options.dart` | Firebase config for Flutter | ✅ Yes |

---

## Useful URLs

| Service | URL |
|---|---|
| Swagger UI (API docs) | https://localhost:5001/swagger |
| Web Admin (React) | http://localhost:5173 |
| Supabase Dashboard | https://supabase.com/dashboard |
| Firebase Console | https://console.firebase.google.com/project/travyle |
| Nominatim (geocoding) | https://nominatim.openstreetmap.org/ |


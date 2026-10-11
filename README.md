# Travyle - Travel with Style 🌍✈️

**Smart Travel, Tour Operations & Support Platform**

Travyle is an integrated full-stack and Agentic AI platform designed to simplify tour management and customer support across four key user roles: Travelers, Local Guides, Tour Operators, and System Administrators. When travel plans get disrupted, our AI agents analyze options and prepare actionable recommendations for authorized staff to review and approve with a single click.

## 🚀 Live Demo & Access

| Component | Link / URL |
| :--- | :--- |
| **React Web App (Admin)** | [https://travyle-admin.netlify.app/](https://travyle-admin.netlify.app/) |
| **ASP.NET Core API (Health)** | [https://travyle-production.up.railway.app/api/health](https://travyle-production.up.railway.app/api/health) |
| **Swagger / OpenAPI** | [https://travyle-production.up.railway.app/swagger](https://travyle-production.up.railway.app/swagger) |
| **Agentic AI Service** | [https://travyle.onrender.com](https://travyle.onrender.com) |

### Test Accounts

| Role | Email / Username | Password | Platform |
| :--- | :--- | :--- | :--- |
| **Traveler** | Nithu@gmail.com | `nithu12345` | Flutter |
| **Local Guide** | upek@mail.com | `K12345` | Flutter |
| **Tour Operator** | operator@travyle.com | `Operator@123` | React |
| **System Admin** | admin@travyle.com | `Admin@123` | React |

---

## 🏗️ System Architecture & Tech Stack

Travyle utilizes a unified backend servicing both web and mobile clients, with a dedicated Python microservice handling AI orchestration.

*   **Backend API:** C#, ASP.NET Core Web API (.NET 10 LTS)
*   **Database:** PostgreSQL (hosted on Supabase) via EF Core 10 + Npgsql
*   **Web Portal (Admin/Staff):** React 19.2 + Vite + TypeScript, TailwindCSS
*   **Mobile App (Travelers/Guides):** Flutter 3.44 (Dart 3.12), Riverpod
*   **Agentic AI Service:** Python 3.13, FastAPI, LangGraph, Google Gemini (gemini-2.5-flash)
*   **Authentication:** Firebase Auth (JWT verified server-side)
*   **3rd Party Integrations:** Stripe Sandbox, Google Maps Geocoding, OpenWeatherMap, SendGrid/Twilio

---

## 🧩 Core Business Components & AI Agents

1.  **Component A: User & Travel Plan Management**
    *   *Features:* Destination browsing, itinerary generation (PDF passes), traveler preferences.
    *   *AI:* **Recommendation Agent** - Analyzes preferences and budget to suggest personalized tour packages.
2.  **Component B: Booking & Financial Management**
    *   *Features:* Tour scheduling, capacity checks, booking lifecycle, Stripe escrow simulation, discount approvals.
    *   *AI:* **Smart Booking Agent** - Resolves scheduling conflicts and proposes booking reservations.
3.  **Component C: Tour Operations & Logistics**
    *   *Features:* Daily checklists, guide assignments, QR check-in, live GPS map tracking.
    *   *AI:* **Operations Agent** - Monitors weather hazards and recommends TSP route re-sequencing.
4.  **Component D: Support & Customer Quality**
    *   *Features:* Ticket submission, photo uploads, goodwill voucher wallet, post-tour reviews.
    *   *AI:* **Support & Quality Agent** - Triage tickets, performs sentiment analysis, and drafts goodwill vouchers for human sign-off.

---

## 📁 Repository Structure

```text
├── backend/          # ASP.NET Core Web API Solution (.NET 10)
├── web-admin/        # React Web Application (Vite/TS)
├── mobile/           # Flutter Cross-Platform Mobile App
├── agent/            # Python FastAPI LangGraph AI Service
└── .github/          # CI/CD Workflows
```

---

## ⚙️ Local Development Setup

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/)
*   [Node.js 22 LTS](https://nodejs.org/)
*   [Flutter SDK 3.44](https://flutter.dev/)
*   [Python 3.13](https://www.python.org/)
*   PostgreSQL / Supabase instance

### 1. Database Initialization
Apply EF Core migrations to your PostgreSQL database to create tables and seed initial data:
```bash
cd backend
dotnet ef database update --project Travyle.Api.csproj --startup-project Travyle.Api.csproj
```

### 2. Python Agentic AI Service (Port 8000)
Ensure you have your `GEMINI_API_KEY` set in your `.env`.
```bash
cd agent
python -m venv .venv
# Activate venv (Windows: .\.venv\Scripts\Activate.ps1 | Mac/Linux: source .venv/bin/activate)
python -m pip install -r requirements.txt
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

### 3. ASP.NET Core Backend (Port 5085)
Set up `appsettings.Development.json` with your database connection strings and Firebase configs.
```bash
cd backend
dotnet restore
dotnet watch run
```

### 4. React Web Admin Portal (Port 5173)
```bash
cd web-admin
npm install
npm run dev
```

### 5. Flutter Mobile App
```bash
cd mobile
flutter pub get
flutter run
```

---

## 👥 Team SE3090_G43

*   **Kalhara K.V** (IT24104383) - Component A
*   **Nitharshana.G** (IT24104175) - Component B
*   **Akalanka W.D.U.K.** (IT24104376) - Component C *(Group Leader)*
*   **Alahakoon B.M.B.S** (IT24101474) - Component D

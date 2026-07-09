# 🏢 Property Management System

A full-stack property maintenance management system built with **Angular 18**, **Tailwind CSS 3**, **.NET 8 Web API**, and **Supabase (PostgreSQL cloud database)**.

---

## 📋 Table of Contents

- [Project Overview](#project-overview)
- [Tech Stack](#tech-stack)
- [🚀 NEW TEAMMATE SETUP — Start Here](#-new-teammate-setup--start-here)
- [▶️ Running the Project Daily](#️-running-the-project-daily)
- [Project Structure](#project-structure)
- [Test Accounts](#test-accounts)
- [Team Collaboration Rules](#team-collaboration-rules)
- [Common Issues](#common-issues)

---

## Project Overview

A web-based system for managing property maintenance operations, covering:

| Role | Capabilities |
|------|-------------|
| **Tenant / Resident** | Submit maintenance requests, track status, chat with technicians |
| **Property Owner** | All Tenant features + manage tenants, view contracts |
| **Technician** | View & execute work orders, update job status, file reports |
| **Property Manager** | Full system access — staff management, request approvals, asset tracking |

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Frontend Framework | Angular | **18.2** |
| UI Styling | Tailwind CSS | **3.4** |
| Language | TypeScript | **5.4** |
| Backend | .NET Web API | **8.0** |
| Database | Supabase (PostgreSQL) | Cloud ☁️ |
| Runtime | Node.js | **20.x** |

> ⚠️ **No SQL Server installation needed.** The database is hosted on Supabase (cloud). Everyone connects to the same shared database automatically.

---

## 🚀 NEW TEAMMATE SETUP — Start Here

> Follow these steps **in order**. Do this only once on a new computer.

---

### STEP 1 — Install Node.js (v20 LTS)

Node.js is required to run the Angular frontend.

1. Go to **https://nodejs.org/**
2. Download the **"20.x LTS"** version (left button — says "Recommended for most users")
3. Run the installer → click **Next** for everything → **Install**
4. After installing, open **Command Prompt** and verify:
   ```
   node --version
   ```
   You should see something like `v20.19.0`

---

### STEP 2 — Install Angular CLI

Open **Command Prompt** or **PowerShell** and run:

```
npm install -g @angular/cli@18
```

Wait for it to finish (1–2 minutes). Then verify:
```
ng version
```
You should see `Angular CLI: 18.x.x`

> 💡 If you see a **permissions error**, close the window, right-click **Command Prompt** → **Run as Administrator**, then try again.

---

### STEP 3 — Install .NET 8 SDK

> Skip this step if you already have .NET 8 installed.

1. Go to **https://dotnet.microsoft.com/en-us/download/dotnet/8.0**
2. Under **.NET 8.0** → click **Download .NET SDK x64** (Windows)
3. Run the installer → click **Install**
4. Verify:
   ```
   dotnet --version
   ```
   You should see `8.x.x`

---

### STEP 4 — Install Git

> Skip this step if you already have Git installed (try `git --version` to check).

1. Go to **https://git-scm.com/download/win**
2. Download and install with all default options

---

### STEP 5 — Clone the Repository

Open **Command Prompt** and navigate to where you want the project (e.g., your Documents folder):

```
cd Documents
git clone https://github.com/huiyi1031/Project-II.git
cd Project-II
```

---

### STEP 6 — Install Frontend Dependencies

```
cd property-management-system\property-management-ui
npm install
```

This installs Angular, Tailwind CSS, and all other packages. May take **2–3 minutes**.

> ✅ Tailwind CSS is already configured in `package.json` — you do NOT install it separately.

---

### STEP 7 — Install Backend Dependencies & Sync Database

```
cd ..\PropertyManagement.API
dotnet restore
dotnet ef database update
```

- `dotnet restore` downloads all .NET packages
- `dotnet ef database update` creates any missing tables in the shared Supabase database

> 💡 If `dotnet ef` is not found, install it with:
> ```
> dotnet tool install --global dotnet-ef
> ```
> Then run `dotnet ef database update` again.

---

### STEP 8 — Verify Everything Works

Open **two separate** Command Prompt / PowerShell windows:

**Window 1 — Backend API:**
```
cd Documents\Project-II\property-management-system\PropertyManagement.API
dotnet run
```
Wait until you see: `Now listening on: http://localhost:5004`

**Window 2 — Frontend:**
```
cd Documents\Project-II\property-management-system\property-management-ui
ng serve --port 4201
```
Wait until you see: `Application bundle generation complete`

Then open your browser and go to: **http://localhost:4201**

✅ You should see the login page. Use the test accounts below to log in.

---

## Test Accounts

> These accounts are seeded on the shared database. If they don't work, someone might have deleted them or changed passwords. Run `POST http://localhost:5004/api/Seed/run` to reset the database.

| Role | Email | Password |
|------|-------|----------|
| **Property Manager** | `manager1@test.com` | `Manager@123` |
| **Technician** | `tech1@test.com` | `Tech@123` |
| **Owner 1** | `owner1@test.com` | `Owner@123` |
| **Owner 2** | `owner2@test.com` | `Owner@123` |
| **Tenant 1** | `tenant1@test.com` | `Tenant@123` |
| **Tenant 2** | `tenant2@test.com` | `Tenant@123` |

> 🔑 **Tip:** You can test the "Forgot Password" or "Add Family Member/Tenant" features! The system will generate a temporary password and send a real activation email (or check the backend console output).

---

## ▶️ Running the Project Daily

Every time you want to work on the project, open **two terminals**:

**Terminal 1 — Backend:**
```
cd Documents\Project-II\property-management-system\PropertyManagement.API
dotnet run
```

**Terminal 2 — Frontend:**
```
cd Documents\Project-II\property-management-system\property-management-ui
ng serve --port 4201
```

Then visit **http://localhost:4201**

---

## Project Structure

```
Project-II/
└── property-management-system/
    ├── property-management-ui/          ← Angular frontend
    │   ├── src/app/
    │   │   ├── auth/                    ← Login page
    │   │   ├── core/                    ← Services, Guards, Models
    │   │   └── features/
    │   │       ├── tenant/              ← Tenant/Resident/Owner pages
    │   │       ├── technician/          ← Technician pages
    │   │       ├── manager/             ← Property Manager pages
    │   │       └── shared/              ← Shared layout & components
    │   ├── package.json                 ← npm dependencies (includes Tailwind)
    │   └── tailwind.config.js           ← Tailwind configuration
    │
    └── PropertyManagement.API/          ← .NET 8 backend
        ├── Controllers/                 ← API endpoints
        ├── Data/AppDbContext.cs         ← Database table definitions
        ├── Models/Entities/             ← C# entity classes (one per table)
        ├── Services/                    ← Business logic
        ├── Migrations/                  ← Auto-generated database migrations
        ├── appsettings.json             ← Safe config (no secrets)
        └── appsettings.Development.json ← YOUR LOCAL SECRETS (gitignored)
```

---

## Team Collaboration Rules

### Every morning before coding (or when pulling new code):
```bash
git pull origin main
cd property-management-system/PropertyManagement.API
dotnet restore # (Optional) Run if new backend packages were added
dotnet ef database update # (Optional) Run if someone added new tables

cd ../property-management-ui
npm install # (Optional) Run if new frontend packages were added
```

### After finishing your work:
```
git add -A
git commit -m "feat: describe what you did"
git push origin main
```

### When adding a new database table:
1. Create the C# model in `Models/Entities/`
2. Add `DbSet<YourModel>` to `AppDbContext.cs`
3. Run: `dotnet ef migrations add YourMigrationName`
4. Run: `dotnet ef database update`
5. Commit and push — teammates then just run `dotnet ef database update`

> ⚠️ **Tell your team in the group chat before modifying shared tables** to avoid merge conflicts.

---

## Common Issues

| Problem | Solution |
|---------|---------|
| `ng: command not found` | Run `npm install -g @angular/cli@18` |
| `npm install` fails | Delete the `node_modules` folder and run `npm install` again |
| `dotnet ef: command not found` | Run `dotnet tool install --global dotnet-ef` |
| `Unable to connect to server` | Check that `dotnet run` is running in a separate terminal |
| Login says "Incorrect email or password" | Run the seed: `POST http://localhost:5004/api/Seed/run` |
| Port 4201 already in use | Run `ng serve --port 4202` instead |
| `git push` rejected | Run `git pull origin main` first, then push again |

---

## Recommended VS Code Extensions

Install these from the VS Code Extensions panel (Ctrl+Shift+X):

| Extension | Purpose |
|-----------|---------|
| **Angular Language Service** | IntelliSense for Angular HTML templates |
| **Tailwind CSS IntelliSense** | Autocomplete for Tailwind classes |
| **C# Dev Kit** | .NET/C# IntelliSense and debugging |
| **GitLens** | See who changed what line in Git |

---

*Built with ❤️ using Angular 18 + Tailwind CSS 3 + .NET 8 + Supabase*

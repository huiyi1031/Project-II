# 🏢 Property Management System

A full-stack property maintenance management system built with **Angular 18**, **Tailwind CSS 3**, and **.NET 8 Web API**.

---

## 📋 Table of Contents

- [Project Overview](#project-overview)
- [Tech Stack](#tech-stack)
- [Prerequisites — Install These First](#prerequisites--install-these-first)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Demo Accounts](#demo-accounts)
- [Team Members](#team-members)

---

## Project Overview

A web-based system for managing property maintenance operations, covering:

| Role | Capabilities |
|------|-------------|
| **Tenant / Resident** | Submit maintenance requests, track status, manage family members, chat with technicians |
| **Property Owner** | All Tenant features + manage tenants, view contracts, file documents |
| **Technician** | View & execute work orders, update job status, file reports |
| **Property Manager** | Full system access — staff management, request approvals, asset tracking, proactive maintenance |

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| Frontend Framework | Angular | **18.2** |
| UI Styling | Tailwind CSS | **3.4** |
| Language | TypeScript | **5.4** |
| Backend | .NET Web API | **8.0** |
| Database | SQL Server | 2019+ |
| Runtime | Node.js | **20.x** |
| Package Manager | npm | **10.x** |

---

## Prerequisites — Install These First

> ⚠️ **Install all tools below before cloning.** Your teammates only need to do this once.

### 1. 🟢 Node.js (v20 LTS)

Node.js is the JavaScript runtime that Angular needs.

1. Go to **https://nodejs.org/**
2. Download the **"20.x LTS"** version (Left button)
3. Run the installer — keep all defaults
4. Verify installation:
   ```bash
   node --version    # should show v20.x.x
   npm --version     # should show 10.x.x
   ```

---

### 2. 📐 Angular CLI (v18)

Angular CLI is the command-line tool for running and building Angular apps.

Open **Command Prompt** or **PowerShell** and run:
```bash
npm install -g @angular/cli@18
```

Verify:
```bash
ng version    # should show Angular CLI: 18.x.x
```

> 💡 If you see a permissions error on Windows, run PowerShell **as Administrator**.

---

### 3. 🎨 Tailwind CSS

Tailwind is already included in the project's `package.json` — **you do NOT need to install it separately**. It installs automatically when you run `npm install` inside the project.

---

### 4. 🔷 .NET SDK 8.0

Required to run the backend API.

1. Go to **https://dotnet.microsoft.com/download**
2. Download **.NET 8.0 SDK** (not Runtime)
3. Run the installer
4. Verify:
   ```bash
   dotnet --version    # should show 8.x.x
   ```

---

### 5. 🗄️ SQL Server

Required for the database.

- **Option A (Recommended for development):** Download [SQL Server 2022 Developer Edition](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) — free for development use
- **Option B:** Use [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) — free, lighter weight

Also install **SQL Server Management Studio (SSMS)**:
- https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms

---

### 6. 💻 Visual Studio Code (Recommended Editor)

Download from **https://code.visualstudio.com/**

Recommended extensions (install from VS Code Extensions panel):
- **Angular Language Service** — IntelliSense for Angular templates
- **Tailwind CSS IntelliSense** — Autocomplete for Tailwind classes
- **C# Dev Kit** — .NET backend support
- **GitLens** — Better git integration

---

## Getting Started

### Step 1 — Clone the Repository

```bash
git clone https://github.com/huiyi1031/Project-II.git
cd Project-II/property-management-system
```

---

### Step 2 — Set Up the Frontend (Angular)

```bash
# Navigate to the UI folder
cd property-management-ui

# Install all dependencies (this downloads node_modules — may take 1-2 minutes)
npm install

# Start the development server
ng serve --port 4201 --open
```

✅ The app will open at **http://localhost:4201**

> 💡 `node_modules` is NOT included in the repository (it's in `.gitignore`).  
> Always run `npm install` after cloning or pulling new changes that include `package.json` updates.

---

### Step 3 — Set Up the Backend (.NET API)

```bash
# From the project root
cd PropertyManagement.API

# Restore NuGet packages
dotnet restore

# Update appsettings.json with your SQL Server connection string
# (see appsettings.json — update the "DefaultConnection" value)

# Apply database migrations
dotnet ef database update

# Run the API
dotnet run
```

✅ The API will run at **http://localhost:5004**

---

### Step 4 — Configure Database Connection

Open `PropertyManagement.API/appsettings.json` and update:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=PropertyManagementDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

Replace `YOUR_SERVER_NAME` with your SQL Server instance (e.g., `localhost` or `DESKTOP-ABC123\SQLEXPRESS`).

---

## Project Structure

```
Project-II/
└── property-management-system/
    ├── property-management-ui/          ← Angular frontend
    │   ├── src/
    │   │   ├── app/
    │   │   │   ├── auth/                ← Login page
    │   │   │   ├── core/                ← Services, Models, Guards
    │   │   │   └── features/
    │   │   │       ├── tenant/          ← Tenant/Resident/Owner pages
    │   │   │       ├── technician/      ← Technician pages
    │   │   │       ├── manager/         ← Property Manager pages
    │   │   │       └── shared/          ← Shared layout & components
    │   │   └── styles.css               ← Global Tailwind + custom styles
    │   ├── package.json                 ← npm dependencies
    │   ├── angular.json                 ← Angular project config
    │   └── tailwind.config.js           ← Tailwind configuration
    │
    └── PropertyManagement.API/          ← .NET 8 backend
        ├── Controllers/                 ← API endpoints
        ├── Models/                      ← Database entity models
        ├── Services/                    ← Business logic
        └── appsettings.json             ← Configuration (DB connection)
```

---

## Demo Accounts

> Use these to test the app without a backend connection. Password for all: **`Test123!`**

| Email | Role | Notes |
|-------|------|-------|
| `tenant@demo.com` | Tenant | Normal returning user |
| `owner@demo.com` | Property Owner | Has "My Tenants" menu |
| `tech@demo.com` | Technician | Work order management |
| `admin@demo.com` | Property Manager | Full system access |
| `new.tenant@demo.com` | New Tenant | First login — set password flow |
| `new.staff@demo.com` | New Staff | Temp password: `TEMP9999` |
| IC bypass | Owner | IC: `900101-10-1234` |

---

## 🔄 Pulling Latest Changes (For Team Members)

When a teammate pushes new code, update your local copy:

```bash
git pull origin main

# If package.json was updated, reinstall dependencies:
cd property-management-ui
npm install
```

---

## ❓ Common Issues

| Problem | Solution |
|---------|---------|
| `ng: command not found` | Run `npm install -g @angular/cli@18` again |
| `EACCES` permission error | Run terminal as Administrator |
| `npm install` fails | Delete `node_modules` folder and run `npm install` again |
| App shows blank page | Check browser console (F12) for errors |
| API not connecting | Ensure `dotnet run` is running and port 5004 is not blocked |

---

## Team Members

<!-- Add your team member names here -->
- 
- 
- 
- 

---

*Built with ❤️ using Angular 18 + Tailwind CSS 3 + .NET 8*

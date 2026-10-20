# ATLAS

ATLAS is a local full-stack effort tracking application based on the supplied requirements and design deck. It uses React 19, ASP.NET Core MVC on .NET 10, Entity Framework Core, and SQLite.

## Clone and run in Visual Studio

### Prerequisites

- Windows 10 or 11
- Visual Studio 2026 with the ASP.NET and web development workload
- .NET 10 SDK
- Node.js 22.12+ (or 20.19+) with npm
- Git (Visual Studio can clone the repository)

### First run

1. In Visual Studio, choose **Git > Clone Repository** and enter:
   https://github.com/Dinesh16203/atlas-mvc.git
2. Open the cloned repository as a folder in Visual Studio if it does not open automatically.
3. Open **View > Terminal** in Visual Studio, change to the frontend folder, and run:

       cd frontend
       npm install
       npm run build

   This installs React dependencies and builds the browser app into the ASP.NET static files folder. Repeat npm run build after changing React files if you run the single-app Visual Studio workflow.
4. In Solution Explorer's **Folder View**, right-click backend/Atlas.Web/Atlas.Web.csproj and choose **Set as Startup Item**.
5. Select the Atlas.Web startup item and press **F5** (or **Ctrl+F5**).
6. Open http://localhost:5166 in a browser.

The website and backend run under Visual Studio. The integrated terminal is only needed for the one-time npm install/build steps. No separate SQLite server or database installation is needed.

### SQLite database

On first start, the app creates backend/Atlas.Web/App_Data/atlas.db and seeds local demo data. The database stays on that machine and is intentionally excluded from Git. Each teammate gets their own database on their first run.

DB Browser for SQLite is optional if you want to inspect tables and records. Open the .db file from **DB Browser > Open Database**; do not type the database file path as a PowerShell command.

## Demo accounts

All seeded local accounts use password AtlasDemo1!.

| Role | Email |
| --- | --- |
| Team Member | member@atlas.local |
| Team Lead | lead@atlas.local |
| Domain Owner | owner@atlas.local |
| Administrator | admin@atlas.local |
| Management | management@atlas.local |

These are localhost demo accounts only. Configure approved corporate SSO before deployment.

## Optional React live-reload workflow

For front-end work, run the ASP.NET project from Visual Studio as above. In Visual Studio's integrated terminal, run:

    cd frontend
    npm run dev

Open the Vite address shown in the terminal (normally http://127.0.0.1:5173). Vite forwards /api requests to the Visual Studio app on port 5166. Stop the Vite process with Ctrl+C when finished. For the simple run-and-review workflow, use the built React app at http://localhost:5166.

## Source changes and contributions

1. Use **Git > New Branch** before larger changes; make edits in Visual Studio.
2. Build the React app after front-end changes (npm run build) and build/run the backend with Visual Studio.
3. Review changed files in **Git Changes**, then commit your work.
4. Teammates can fork the repo and open a pull request. For direct pushes, the repository owner must add each teammate under **Settings > Collaborators**.

Suggested change request:

> In this ATLAS project, change [page/workflow]. When [role] does [action], the expected result is [behavior]. Keep React + ASP.NET Core MVC + SQLite and the existing design. Update API permissions, data/schema, validation, docs, and tests as needed. Tell me what changed and how to verify it in Visual Studio.

## Build and tests

From Visual Studio's integrated terminal, with the .NET 10 SDK installed:

    dotnet build .\backend\Atlas.Web\Atlas.Web.csproj
    dotnet test .\tests\Atlas.Tests\Atlas.Tests.csproj

Build the React app with npm run build from frontend. The manual workflow checklist and requirement mapping are in docs/requirements-and-test-plan.md.

## Database and configuration notes

- Default database: backend/Atlas.Web/App_Data/atlas.db.
- The local app creates the schema on first run with EF Core EnsureCreated; a formal EF migration history is not included yet.
- Back up the database with the app stopped before making schema/seed changes. Deleting it resets local data and recreates demo records on next start.
- No environment file or API key is required for local development.
- Optional database override: set ConnectionStrings__Atlas in the environment, for example Data Source=C:/atlas-data/atlas.db.
- Do not commit real credentials, local databases, bin/obj, node_modules, wwwroot, or Visual Studio user state.

## Project structure

    atlas-mvc/
    ├── README.md
    ├── docs/
    │   └── requirements-and-test-plan.md
    ├── backend/Atlas.Web/
    │   ├── Controllers/       API endpoints and authorization
    │   ├── Data/              EF Core entities, schema setup, and seed data
    │   ├── Properties/        Local API launch settings
    │   ├── Services/          Audit and submission services
    │   ├── Program.cs
    │   └── Atlas.Web.csproj
    ├── frontend/
    │   ├── src/               React pages, shared components, API client, styles
    │   ├── package.json
    │   ├── package-lock.json
    │   └── vite.config.js
    └── tests/Atlas.Tests/      Backend rule tests

For requirements, role scopes, business rules, known gaps, and acceptance steps, see docs/requirements-and-test-plan.md.

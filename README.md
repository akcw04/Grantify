# Grantify

Cloud-based scholarship management system (CT071-3-3-DDAC group project). ASP.NET Core Razor Pages + EF Core, deployed on AWS (Elastic Beanstalk + RDS).

## Run it locally
1. Install Visual Studio 2026 with the "ASP.NET and web development" workload (includes LocalDB).
2. Clone this repo, open `Grantify.slnx`.
3. Press F5. The app creates and fills your local database by itself on first run.
4. Log in with a test account (all passwords `Grantify@123`):
   - `student@grantify.test` / `officer@grantify.test` / `admin@grantify.test`

Database questions? Read **DATABASE-GUIDE.txt** (same folder as this file) — it explains setup, daily use, and how to change tables safely.

## Where code goes (IMPORTANT — keep this clean)

```
Grantify/                        <- repo root
├── Grantify.slnx                <- open this in Visual Studio
├── DATABASE-GUIDE.txt           <- read before touching the database
├── GIT-GUIDE.txt                <- read before your first push
└── Grantify/                    <- the web application
    │
    │  FRONTEND (what the user sees)
    ├── Pages/                   <- .cshtml files = HTML shown to the user
    │   ├── Student/             <- Member A builds student pages here
    │   ├── Officer/             <- Member B builds officer pages here
    │   ├── Admin/               <- Member C builds admin pages here
    │   └── Shared/              <- layout + navbar (shared, announce changes)
    ├── wwwroot/                 <- css, javascript, images
    │   ├── css/site.css         <- SHARED theme, loads on every page (announce changes)
    │   ├── css/student|officer|admin/  <- styles only YOUR role's pages use
    │   ├── js/site.js           <- SHARED javascript (announce changes)
    │   ├── js/student|officer|admin/   <- scripts only YOUR role's pages use
    │   ├── images/              <- logos, icons (see images/README.txt for rules)
    │   └── lib/                 <- Bootstrap, jQuery — third-party, do not edit
    │
    │  BACKEND (logic and data)
    ├── Services/                <- business logic classes (the "brain")
    ├── Models/                  <- database table classes
    ├── Data/                    <- AppDbContext (door to DB) + DbSeeder (test data)
    ├── Migrations/              <- auto-generated DB change files (never edit)
    └── Program.cs               <- app wiring: DB, login, page access rules
```

### Frontend vs backend rule
- A `.cshtml` file only **shows** data. No logic beyond loops and ifs for display.
- Its `.cshtml.cs` (page model) stays **thin**: it calls a service and stores the result in a property.
- Real logic (rules, calculations, database queries) lives in a **`Services/`** class.
- `Pages/Index.cshtml.cs` + `Services/ScholarshipService.cs` show the full pattern — copy them.
- New service class? Register it with one line in `Program.cs` (section 4).

### CSS and JavaScript rule
- `css/site.css` and `js/site.js` are the **shared base** — the app must look like ONE system (the marking scheme rewards a consistent theme). Colors, fonts, spacing live here.
- Each role folder (`css/student/` etc.) holds styles/scripts **only that role's pages** need, loaded per page with an `@section Styles` / `@section Scripts` block — the three dashboard pages show how; copy from yours.
- Never copy shared rules into your role css "to be safe" — change the shared file (after announcing) or add only what is new.

### Page access by role
Access rules are set **once** in `Program.cs` (section 3): everything under `Pages/Student/` needs the Student role, `Pages/Officer/` the Officer role, `Pages/Admin/` the Admin role. You do not need `[Authorize]` on each page.

## Branch & folder ownership
| Member | Role pages | Branch prefix |
|---|---|---|
| A | `Pages/Student/` | `feature/student-*` |
| B | `Pages/Officer/` | `feature/officer-*` |
| C | `Pages/Admin/` | `feature/admin-*` |

Nobody codes directly on `main`. One branch per feature, merged back through a Pull Request.

Shared files (`Models/`, `Data/`, `Services/`, `Pages/Shared/`, `Program.cs`) — announce in group chat before changing.

Commit message format: `[Student] Add application submission form`

**Read GIT-GUIDE.txt** (same folder as this file) before your first push — it has the daily 6 steps, the branch rules, and what to do when a merge or migration goes wrong.

## Code style
- Comments in simple English: say what the code does and why, no fancy words.
- PascalCase for classes/methods, camelCase for variables.
- One feature per commit.
- Never commit secrets (real passwords, RDS connection strings).

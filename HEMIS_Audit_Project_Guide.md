# HEMIS AUDIT MANAGEMENT SYSTEM
## System Overview & Purpose Summary

**SNG Grant Thornton &nbsp;|&nbsp; Data Analytics — HEMIS Assurance &nbsp;|&nbsp; June 2026**

| | |
|---|---|
| **Prepared by:** | Mamishi Madire |
| **Role:** | Trainee Accountant (ACCA) — Data Analytics |
| **Stream:** | General Assurance / Digitech Assurance |
| **Date:** | June 2026 |

---

## 1. What Is HEMIS and Why Does It Need Auditing?

HEMIS — the Higher Education Management Information System — is the national data platform used by South African universities, universities of technology, and other higher education institutions to submit student and staff data to the Department of Higher Education and Training (DHET). Government funding subsidies are calculated directly from the data each institution submits through HEMIS.

Because public funding is allocated based on this data, its accuracy and integrity are critical. Errors, inconsistencies, or irregularities in submitted HEMIS data can result in:

- Incorrect government subsidy allocations — the institution may receive more or less funding than it is entitled to
- Funding being paid for students who do not qualify under HEMIS directives
- Non-compliance with the HEMIS Data Element Dictionary (DEETYAPAC), audit directives, or NQF requirements
- Reputational and regulatory risk for the institution

SNG Grant Thornton is engaged by higher education institutions to independently audit their HEMIS data before submission to DHET — verifying that the data complies with applicable directives and the HEMIS Data Element Dictionary. This engagement requires a structured, repeatable, and well-documented audit process.

---

## 2. Why I Built This System

### 2.1 The Observation That Led to This System

Previously, HEMIS audit engagements at SNG Grant Thornton were performed with the assistance of an external service provider. During the course of the engagement, I observed that the service provider was running HEMIS validations using technical scripts — SQL queries, R scripts, and similar tools — to execute the required checks against the institution's data.

This observation raised two questions: first, whether SNG Grant Thornton could perform these validations independently rather than relying on an external party; and second, whether there was a better way to deliver those validations than through scripts. Scripts, while technically capable, create significant limitations in an audit team environment:

- Only someone who can read and write code can operate or understand the validation — this immediately excludes most trainees and many audit professionals
- Scripts are not self-documenting — a reviewer cannot easily follow the audit logic without being taken through the code line by line
- There is no standardisation — different people may implement the same validation differently, producing inconsistent results across engagements
- There is no built-in workflow for review, approval, or sign-off — these steps must be managed separately
- When the person who wrote the scripts leaves, their knowledge leaves with them

Rather than replicating the service provider's approach by writing scripts of our own, I decided to build a purpose-built system — one that SNG Grant Thornton can own, operate, and scale independently, without reliance on an external party or on any individual's technical scripting skills.

Consider how this compares to how SNG Grant Thornton's audit teams currently work:

> Every member of an engagement team — trainee, senior, manager, or director — can be trained to use **LEAP**, the firm's audit management platform, after a short onboarding period. Nobody needs to understand how LEAP's database is structured, how its queries are written, or how its workflows are implemented. The platform guides users through the process, presents a consistent interface, and enforces the quality control workflow automatically.

**The HEMIS Audit System was built with exactly the same philosophy in mind.**

### 2.2 A Purpose-Built Audit Tool

Rather than writing scripts to replicate what the service provider was doing, I built a browser-based audit management system that any member of the SNG Grant Thornton engagement team can operate — without writing or reading a single line of code. The system brings the same accessibility and structure to HEMIS data auditing that LEAP brings to the broader audit practice:

- Any team member — from trainee to director — can be trained to operate the system within a short period, without any knowledge of SQL, R, or statistics
- All analytics required by VALPAC and the engagement letter are built into the system — users configure parameters through a guided interface and run the validation with a single action
- Every analytic follows the same consistent interface — a user who knows how to run one procedure can run all of them
- The review workflow — analyst preparation, manager review, director approval — is enforced by the system and cannot be bypassed
- Results are stored in a database and are always accessible to the full team, exactly as archived LEAP engagements remain accessible after the engagement closes
- For those who need it, the system can also generate and export the underlying SQL script for any validation — providing full transparency into the logic and serving as additional technical documentation in the engagement file

This shifts HEMIS auditing from a service-provider-dependent, script-based activity into a managed, repeatable, in-house process — one that SNG Grant Thornton owns and controls, consistent with how the firm manages quality and risk across all its service lines.

---

## 3. What the System Does

The HEMIS Audit System is a full web-based audit management platform. The following summarises its core capabilities.

### 3.1 Engagement Management

Every HEMIS audit is managed as an engagement within the system. Each engagement records the institution's details, the audit year, the team assigned and their roles, all validation results, and the sign-off and archive status. Engagements move through a structured lifecycle: **Pending → Active → Archived**. Only fully signed-off engagements can be archived, ensuring that incomplete work cannot be closed prematurely.

### 3.2 Audit Analytics Modules

The core of the system is a comprehensive set of audit analytics modules covering the full scope of validations required as per the HEMIS Data Element Dictionary (DEETYAPAC / VALPAC) and the terms of the engagement letter. The analytics implemented in the system are not a subset or a selection — they represent the complete set of procedures that the engagement requires to be performed and documented. For each analytic, the system:

- Allows the auditor to connect to the institution's HEMIS SQL Server database
- Provides a dynamic configuration interface where table and column names are mapped to the analytic parameters
- Generates and executes the validation query against the live data
- Presents results clearly: total records validated, pass count, fail count, exception rate, and sample exception rows
- Stores the full results in the system database for retrieval and review at any time
- Supports multi-level sign-off: analyst, manager, and director
- Allows export of results to Excel, CSV, and SQL script

Because the analytics are derived directly from the VALPAC directives and the engagement letter scope, the system ensures that no required procedure is omitted and that the audit can demonstrate full coverage against the mandated validation requirements.

### 3.3 Portfolio Dashboard and Result Storage

Every validation run is saved to the system's database. The dashboard provides a portfolio-level view of all active engagements, showing which rules/analytics have been run, the current pass/fail outcome per rule, the sign-off status across the team, and any approvals pending attention. The full audit history of every engagement is preserved and accessible at any time — the team does not need to maintain results in separate spreadsheets or files.

### 3.4 Internal Messaging

The system includes a built-in messaging platform for team communication within the context of each engagement. Messages support file attachments (up to 15 MB), thread-based conversations, read receipts, and edit and delete functionality.

### 3.5 Audit Trail

Every significant action in the system is recorded in an audit log — logins, logouts, validation runs, sign-offs, downloads, user management actions, and more — with the user identity, timestamp, and IP address. This provides full traceability supporting both quality review and regulatory compliance.

---

## 4. How the System Will Assist SNG Grant Thornton

### 4.1 Anyone on the Engagement Team Can Use It

The system is designed to be operated by the full team, including trainees and junior staff, without any technical background:

| Role | Capabilities in the System |
|------|---------------------------|
| **Trainee** | View all engagement results, follow progress, download exports — read-only access ideal for learning and observation |
| **Data Analyst** | Configure and run all required audit analytics, save workspaces, sign off results as the preparer |
| **Manager** | Review analyst-signed results, apply manager-level sign-off, manage team assignments per engagement |
| **Director** | Review all signed results, provide director-level approval, archive completed engagements |
| **Admin** | Full system control: user management, engagement setup, audit log access, system configuration |

### 4.2 A Persistent Database of Engagement Records — Like LEAP

One of the most significant advantages of the system is its persistent database. Every engagement, every validation run, and every result is stored and retrievable at any time — regardless of who ran the validation, when they ran it, or whether they are still on the team.

This mirrors the way LEAP works for the broader audit: archived engagements remain accessible, historical results can be reviewed, and the firm builds an institutional knowledge base that does not depend on any individual team member's files or personal scripts. In practical terms:

- A manager can open the system and immediately see the current state of every active HEMIS engagement
- A director can review signed-off results without waiting for a file to be emailed
- Historical validation runs from prior years remain available for comparison and continuity
- Nothing is lost when a team member leaves, rolls off the engagement, or changes roles

### 4.3 Speed and Consistency

Traditional script-based HEMIS validation requires time to write, test, and execute code for each procedure. The system has the validation logic for every analytic required by VALPAC and the engagement letter pre-built and tested — an auditor can connect to a client's database, configure the parameters, and run the validation in minutes, without writing a single line of code. Because every team member uses the same system, the output of the same analytic is identical regardless of who runs it, eliminating the risk of different auditors interpreting or implementing the same procedure differently.

### 4.4 Enforced Workflow and Quality Control

The system enforces a structured review workflow aligned with the firm's quality control requirements:

> **Step 1:** Data Analyst runs the validation and applies Analyst Sign-Off  
> **Step 2:** Manager reviews the analyst-signed results and applies Manager Sign-Off  
> **Step 3:** Director reviews all signed results, provides Director Approval, and archives the engagement

This workflow is enforced at every step — the system will not allow sign-offs to be applied out of sequence or by the wrong role. Once an engagement is archived, it becomes read-only and all results and sign-offs are permanently preserved.

### 4.5 Live Reference Tools Built Into the System

The system provides two live reference integrations that give auditors access to critical external resources without leaving the platform.

#### DEETYAPAC / VALPAC Help (heda.co.za)

The DEETYAPAC Help module embeds live access to the full HEMIS Data Element Dictionary and audit directives hosted at `www.heda.co.za`. When an auditor needs to check what a specific data element means, or what the directive says about a particular field, the reference is immediately available within the system — always showing the current live version. If DHET or HEDA updates the DEETYAPAC content, the system automatically reflects those changes because it fetches the content live from the source each time it is accessed.

#### SAQA Qualification Register (allqs.saqa.org.za)

For engagements involving clinical training programmes and other regulated qualifications, the system provides live access to the SAQA qualification register. Auditors can verify, directly within the system, whether a qualification is currently registered on the NQF, its NQF level and sub-framework, minimum credits, the originating institution, and all critical registration dates — Registration Start Date, Registration End Date, Last Date for Enrolment, and Last Date for Achievement.

The SAQA module includes a built-in four-step interactive guide that teaches auditors how to interpret the SAQA search interface, read results tables, understand qualification detail fields, and apply the four critical dates in an audit context — with colour-coded visual timelines. The guide is accessible alongside the live SAQA search, so auditors can read the explanation and search simultaneously without switching screens.

### 4.6 Export-Ready Audit Evidence

For every validation run, the system produces exports that serve directly as audit evidence filed in the engagement file:

- **Excel (.xlsx)** — formatted results sheets with exception details and run metadata
- **CSV** — raw data for further analysis or import into other tools
- **SQL Script** — the exact query that was executed, for documentation and peer review

### 4.7 Faster Engagement Turnaround

Because the system eliminates the time spent writing, debugging, and executing scripts, and because results are immediately visible to all team members, the overall turnaround time for a HEMIS engagement is significantly reduced. The team can focus their time on analysing exceptions and forming audit conclusions, rather than on the mechanics of data extraction and validation.

---

## 5. Technical Architecture

This section documents the internal structure of the system — the components that were built, what each one does, and how they work together. This information is intended for developers, technical reviewers, or team members who need to understand, maintain, or extend the system.

### 5.1 Overview of the Application Structure

The system is built on **ASP.NET Core MVC**, following the standard Model-View-Controller pattern. The application is organised into the following layers:

```
HemisAudit/
├── Controllers/        Request handling — one controller per feature or rule
├── Models/             Core data entities stored in the database
├── ViewModels/         Data transfer objects shaped for each view
├── Views/              Razor HTML templates — one folder per controller
├── Services/           Business logic and database operations
├── Helpers/            Shared utility classes and static lookups
├── Data/               Database context, migrations, and bootstrapper
└── wwwroot/            Static assets — CSS, JavaScript, uploads
```

---

### 5.2 Controllers

Controllers receive HTTP requests, coordinate with services, and return views or JSON responses. There is one controller per major feature area and one controller per audit rule.

#### Core Feature Controllers

| Controller | Purpose |
|-----------|---------|
| **AccountController** | Authentication — login, logout, forgot password, reset password, password expiry enforcement, forced renewal flow |
| **AdminController** | System administration — create and manage users and engagements, assign users to engagements, approve/archive/delete engagements, view audit log, unlock accounts, force password resets |
| **DashboardController** | Portfolio home — displays all engagements assigned to the current user, rule outcome metrics, pending approvals, favourite engagements, and industry breakdown |
| **MessagesController** | Internal messaging — send messages, reply to threads, edit/delete messages, file attachments, AJAX polling for real-time unread count updates |
| **ProfileController** | User profile — edit personal details (name, phone, department, address, employee code), upload profile picture, change password |
| **DirectivesController** | DEETYAPAC Help module — serves the embedded help shell, proxies HTML content from `heda.co.za/Valpac_Help/`, and provides the sitemap navigation JSON |
| **SaqaController** | SAQA Search module — serves the full-screen SAQA qualification search page with the built-in interactive guide |
| **ValidationOperationsController** | Background job coordination — manages long-running validation operations with progress polling so the browser does not time out during large data runs |

#### Audit Rule Controllers (Rule10Controller through Rule67Controller)

There are **58 rule controllers**, one for each audit rule or rule group. Every rule controller follows an identical action pattern:

| Action | Purpose |
|--------|---------|
| `Index` | Load the rule workspace — either a fresh configuration form or the saved workspace state for the selected engagement |
| `GetWorkspaceState` | Return the current saved workspace as JSON (called on page load) |
| `GetDatabases` | Return the list of available databases on the specified SQL Server |
| `GetTables` | Return the list of tables in the selected database |
| `GetColumns` | Return the list of columns in the selected table |
| `VerifyTables` | Test that the configured tables exist and contain data before running |
| `RunValidation` | Execute the audit analytic against the client's HEMIS database and return results |
| `SaveWorkspace` | Persist the current configuration and results to the system database |
| `SignOffWorkspace` / `RemoveWorkspaceSignoff` | Apply or retract the current user's sign-off at the workspace level |
| `AddSignoff` / `RemoveSignoff` | Apply or retract sign-off from the completed run review screen |
| `BeginWorkspaceEdit` | Unlock a signed-off workspace for re-validation |
| `Run` | Display the read-only review of a completed, saved run |
| `GenerateSql` | Return the SQL query script for the configured rule as downloadable text |
| `GenerateRScript` | Return an R statistical script for the rule |
| `DownloadExcel` / `DownloadCsv` / `DownloadSql` | Export current workspace results |
| `DownloadSavedExcel` / `DownloadSavedCsv` / `DownloadSavedSql` | Export from a saved completed run |

This consistent pattern means any developer familiar with one rule controller immediately understands all of them.

---

### 5.3 Models

Models represent the core data entities stored in the **SQLite application database**. They are defined in `Models/ApplicationModels.cs`.

| Model | Purpose | Key Fields |
|-------|---------|-----------|
| **ApplicationUser** | Extended Identity user account | `FirstName`, `LastName`, `EmployeeCode`, `Department`, `Gender`, `OfficeAddress`, `IsActive`, `ProfilePicturePath`, `PasswordSetDate`, `PasswordChangedAt`, `PasswordHistory` |
| **Client** | An engagement / institution being audited | `Name`, `FiscalYear`, `InstitutionType`, `Description`, `Status` (Pending / Active / Closed), `IsActive`, `CreatedAt`, `CreatedByUserId` |
| **ClientUser** | Many-to-many link between users and engagements | `ClientId`, `UserId`, `EngagementRole` (DataAnalyst / Manager / Director / Trainee), `AssignedAt`, `AssignedByUserId`, `IsActive` |
| **ValidationRun** | A saved result of an audit analytic execution | `ClientId`, `RuleNumber`, `RuleName`, `HemisServer`, `HemisDatabase`, `TotalValidated`, `PassCount`, `FailCount`, `ExceptionRate`, `Status`, `ExceptionsJson`, `ResultsJson`, `RunAt`, `RunByUserId`, `IsCurrent` |
| **AuditLog** | A record of every significant system action | `Timestamp`, `UserId`, `UserName`, `Action`, `Details`, `IpAddress` |

**Key database relationships:**
- A `Client` has many `ClientUsers` (cascade delete) and many `ValidationRuns` (cascade delete)
- A `ApplicationUser` has many `ClientUsers` (restrict delete — a user with engagement assignments cannot be deleted)
- `ValidationRun.IsCurrent` flags the most recent run per rule per engagement — historical runs are preserved but not shown by default

---

### 5.4 ViewModels

ViewModels are data transfer objects that shape the data passed from a controller to a view. They are defined in `ViewModels/ApplicationViewModels.cs` and individual `ViewModels/Rule*ViewModels.cs` files.

#### Core ViewModels

| ViewModel | Used By | Purpose |
|-----------|---------|---------|
| **LoginViewModel** | Account/Login | Email, Password, RememberMe |
| **ChangePasswordViewModel** | Profile, Account | CurrentPassword, NewPassword, ConfirmPassword with policy validation |
| **RenewPasswordViewModel** | Account/PasswordExpired | Handles both the in-flow and standalone password renewal paths |
| **UserListViewModel** | Admin/Users | Flattened user data for the user table (name, role, status, last login, locked-out state) |
| **CreateUserViewModel** | Admin/CreateUser | FirstName, LastName, Email, EmployeeCode, Role |
| **EditUserViewModel** | Admin/EditUser | Full user profile fields plus current profile picture path |
| **ClientDetailViewModel** | Admin/ClientDetail | Client summary, assigned users list, validation runs, archive eligibility flags |
| **DashboardViewModel** | Dashboard/Index | Portfolio engagement cards, rule outcome metrics, industry breakdown, pending approval queue |
| **MessagePageViewModel** | Messages/Index | Inbox thread list, active thread, compose modal state |
| **MessageSendViewModel** | Messages/Send | Subject, Body, RecipientIds, ClientId, file attachments |
| **MessageThreadViewModel** | Messages/Index | ThreadId, Subject, ordered Messages, Participants, read status |

#### Rule-Specific ViewModels

Each rule has two or three dedicated ViewModels:

| ViewModel Pattern | Purpose |
|------------------|---------|
| `Rule*ValidationRequest` | The POST payload sent when running a validation — server, database, table/column mappings, and any rule-specific parameters |
| `Rule*ValidationSummary` | The result returned after a run — pass/fail counts, exception rows, metadata, signoff state |
| `Rule*WorkspaceStateViewModel` | The full saved state of a workspace — configuration plus latest run, used to restore the UI on page load |
| `Rule*RunReviewViewModel` | Read-only view of a completed run for sign-off and download — excludes editable configuration fields |

---

### 5.5 Services

Services contain the business logic and data access operations. Controllers are kept thin — they handle the HTTP layer only and delegate all processing to services. Services are registered with the ASP.NET Core dependency injection container and injected into controllers via their constructors.

| Service | Interface | Purpose |
|---------|-----------|---------|
| **Rule*Service** | `IRule*Service` | One service per rule — contains the SQL generation logic, validation execution, result parsing, and Excel/CSV/SQL export logic for that rule |
| **SystemDatabaseService** | `ISystemDatabaseService` | All read/write operations on the SQLite application database — engagements, users, validation runs, messages, audit log, dashboard metrics |
| **AuditLogService** | `IAuditLogService` | Writes entries to the audit log table; called from controllers after every significant action |
| **ExportService** | `IExportService` | Generates Excel (.xlsx) and CSV files from validation result data; used by all rule controllers |
| **PasswordPolicyService** | `IPasswordPolicyService` | Enforces password complexity rules, expiry checks, history tracking, and renewal enforcement on login |
| **ValidationOperationService** | `IValidationOperationService` | Manages background validation jobs — queues long-running operations, tracks progress, and makes results available for polling |
| **Rule*RScriptGenerator** | _(static helpers)_ | Generates R statistical script exports for each rule, mirroring the SQL logic in the R language |

---

### 5.6 Helpers

Helpers are static utility classes that provide shared logic used across multiple controllers or services.

| Helper | Purpose |
|--------|---------|
| **IntegrityRuleCatalog** | A static lookup table of all 67 audit rules — rule number, name, description, and default table/column expectations. Used to populate rule metadata throughout the system without duplication. |
| **RuleRouteHelper** | Maps rule numbers to their controller routes and action names. Used to generate correct navigation links (Previous Rule / Next Rule) on every rule workspace page. |
| **ModuleSequenceNavigationHelper** | Computes the previous and next rule in the sequence for a given rule number, enabling breadcrumb navigation across the full 67-rule set. |
| **ValidationRunAccessPolicy** | Determines whether a given user has permission to view, run, sign off, or download a validation run — based on their role and assignment to the engagement. Centralises access control logic so it does not need to be repeated in every controller. |
| **AvatarHelper** | Resolves the correct avatar image source for a user — returns the profile picture path if one has been uploaded, otherwise returns a generated initials-based avatar URL. |

---

### 5.7 Data Layer

| Component | Purpose |
|-----------|---------|
| **ApplicationDbContext** | Entity Framework Core `DbContext` — defines the SQLite database schema through `DbSet<T>` properties for `Client`, `ClientUser`, `ValidationRun`, and `AuditLog`, plus the ASP.NET Identity tables. Configures composite keys, unique indexes, and cascade delete rules. |
| **SystemDatabaseBootstrapper** | Runs on application startup — applies any pending EF Core migrations, creates the SQLite database if it does not exist, and seeds the initial Admin user account if no users are present. |
| **DbInitializer** | Seeds reference data and default configuration values required for the application to function on first run. |

The system uses **two databases**:

| Database | Technology | Stores |
|----------|-----------|--------|
| Application DB | SQLite (local file) | Users, roles, engagements, user-engagement assignments, validation run metadata, audit log, messages |
| HEMIS Data DB | SQL Server (client's existing server) | The client's HEMIS student and staff data — the system connects to this read-only to execute validation queries |

The system **never writes to the client's SQL Server database**. All validation queries are read-only `SELECT` statements. The client's data is never modified.

---

### 5.8 Views

Views are Razor `.cshtml` templates. There is one folder per controller, plus a `Shared` folder containing the layout and partial views used across the application.

| View Folder | Key Views |
|-------------|----------|
| `Views/Shared/` | `_Layout.cshtml` — the master layout; defines the sidebar navigation, topnav bar, theme switching, session guard, and page-content wrapper used by every page |
| `Views/Account/` | Login, ForgotPassword, ResetPassword, ChangePassword, PasswordExpired |
| `Views/Admin/` | Clients (engagement list), ClientDetail, CreateClient, Users, CreateUser, EditUser, AuditLog |
| `Views/Dashboard/` | Index — portfolio view with engagement cards and metrics |
| `Views/Messages/` | Index — unified inbox, compose, and thread view |
| `Views/Profile/` | Edit — profile details and profile picture upload |
| `Views/Directives/` | Index — DEETYAPAC browser shell with sidebar navigation and proxied content frame |
| `Views/Saqa/` | Index — full-screen SAQA iframe with toolbar, live bar, fallback message, and four-step interactive guide panel |
| `Views/Rule10/` through `Views/Rule67/` | Each rule has two views: `Index.cshtml` (the interactive workspace) and `Run.cshtml` (the read-only completed run review for sign-off and download) |

---

### 5.9 Static Assets (wwwroot)

| Path | Content |
|------|---------|
| `wwwroot/css/site.css` | The complete application stylesheet — layout, sidebar, topnav, cards, tables, rule workspace UI, messaging, modals, dark/light theme variables |
| `wwwroot/js/rule-workspace-ui.js` | Shared JavaScript for the rule workspace — database/table/column discovery dropdowns, run progress handling, signoff interactions, and export triggers used by all 67 rule pages |
| `wwwroot/uploads/profiles/` | User profile picture files — JPG/PNG/GIF/WEBP, max 5 MB, validated by MIME type and file signature |
| `wwwroot/uploads/messages/` | Message attachment files — any format, max 15 MB per file, classified by MIME type for display |

---

### 5.10 Technology Stack Summary

| Layer | Technology |
|-------|-----------|
| Application framework | ASP.NET Core MVC (C#) |
| ORM / data access | Entity Framework Core |
| Application database | SQLite |
| Client HEMIS database | Microsoft SQL Server (read-only connection) |
| Authentication | ASP.NET Core Identity |
| Password policy | Custom `IPasswordPolicyService` with history tracking |
| Frontend | HTML5, CSS3, Vanilla JavaScript |
| HTTP compression | Brotli + Gzip response compression |
| File exports | EPPlus / ClosedXML for Excel; `System.Text` for CSV and SQL |
| Email | SMTP (password reset links) |
| External HTTP | `IHttpClientFactory` (DEETYAPAC proxy) |
| Deployment target | Windows Server / IIS |

---

## Summary

The HEMIS Audit System was built to solve a practical problem: HEMIS data auditing is a specialised, repeatable process that should not depend on the scripting skills of individual team members. By building a purpose-built audit application, the system delivers the same benefits to HEMIS engagements that LEAP delivers to the broader audit practice at SNG Grant Thornton:

| Benefit | How the System Delivers It |
|---------|---------------------------|
| **Accessible to all team members** | Anyone can be trained to operate the system — trainees to directors — without technical knowledge |
| **Persistent engagement records** | All results stored in a database, retrievable at any time, like archived LEAP engagements |
| **Enforced review workflow** | Analyst → Manager → Director sign-off is built in and cannot be bypassed |
| **Live reference integration** | DEETYAPAC and SAQA are embedded live in the system; content is always current |
| **Built-in user guidance** | Interactive guides for SAQA search, date interpretation, and NQF field meaning |
| **Production-ready exports** | Excel, CSV, and SQL exports serve directly as engagement file evidence |
| **Full audit trail** | Every system action is logged with user, timestamp, and IP address |
| **Faster turnaround** | Pre-built analytics covering the full VALPAC and engagement letter scope — no script writing or debugging required |

The result is a professional, scalable, and team-friendly audit tool that makes HEMIS data auditing faster, more consistent, and accessible to the full engagement team — entirely aligned with the way SNG Grant Thornton manages quality and risk across its service lines.

---

*Mamishi Madire &nbsp;|&nbsp; SNG Grant Thornton &nbsp;|&nbsp; Data Analytics — HEMIS Assurance &nbsp;|&nbsp; June 2026*

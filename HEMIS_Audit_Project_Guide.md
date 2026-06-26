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

## 5. How the System Is Built — A Plain-English Guide

This section explains the internal building blocks of the system. It is written so that anyone — including team members with no coding background — can understand what each part is, what it does, and why it matters.

Think of the system the same way you would think about a large office building. The building has different floors and departments, each with a specific job. The sections below explain each "department" of the system.

---

### 5.1 The Big Picture

When you click a button or open a page in the system, the following happens — in order, every time:

```
You (the user)
    → click something in the browser
        → the Controller receives your request
            → the Controller asks a Service to do the work
                → the Service reads or saves data to the Database
                    → the Controller takes the result
                        → the View displays it to you as a page
```

There are five main building blocks: **Views**, **Controllers**, **Services**, **Models**, and **Helpers**. Each one has one job and does only that job.

---

### 5.2 Views — What You See

**Simple explanation:** A View is the page you see on screen. It is the HTML — the buttons, tables, forms, and text that appear in your browser.

Every page in the system has a View. The View's only job is to display information. It does not calculate anything, does not touch the database, and does not make decisions. It simply shows what it has been given.

**Examples of Views in this system:**

| Page you visit | What the View shows |
|---------------|---------------------|
| Dashboard | Your engagement cards, rule outcome summary, and metrics |
| Rule workspace | The configuration form, run button, and results table |
| Messages | Your inbox, the active conversation thread, and the reply box |
| SAQA Search | The full-screen SAQA website and the Search Guide panel |
| DEETYAPAC Help | The sidebar navigation and the proxied help content |
| Admin — Users | The list of all system users and their status |

Every rule (Rule 10 through Rule 67) has two Views:
- One for **running** the validation (the workspace with the configuration form)
- One for **reviewing** a completed run (read-only, for sign-off and download)

There is also a shared master layout — think of it as the frame around every page — that provides the sidebar, the top navigation bar, the theme, and the session security check that appear on every screen.

---

### 5.3 Controllers — The Traffic Directors

**Simple explanation:** A Controller is like a receptionist. When you click a button or submit a form, the Controller receives your request, figures out what you need, asks the right Service to handle it, and then sends the result back to the correct View to display to you.

The Controller does not do the actual work itself — it just directs traffic. This keeps everything organised and easy to maintain.

**Examples of Controllers in this system:**

| Controller | What it handles |
|-----------|----------------|
| **Account** | Login, logout, forgot password, reset password, password expiry |
| **Admin** | Creating users and engagements, assigning team members, approving and archiving engagements, viewing the audit log |
| **Dashboard** | Loading your portfolio of engagements and their current status |
| **Messages** | Sending messages, replying, editing, deleting, and checking for new messages |
| **Profile** | Editing your personal details and uploading your profile picture |
| **Directives** | Serving the DEETYAPAC help page and fetching content from heda.co.za |
| **Saqa** | Serving the SAQA search page |
| **Rule 10 through Rule 67** | One Controller per audit rule — handles connecting to the database, running the validation, saving results, and managing sign-offs and exports |

When you click "Run Validation" on a rule page, the Rule Controller receives that request, calls the Rule Service to execute the check, and then sends the results back to the View to display in the results table.

---

### 5.4 Services — Where the Work Gets Done

**Simple explanation:** A Service is where the actual work happens. It contains the logic — the instructions that check the data, save results, generate exports, send messages, and enforce rules. The Controller tells the Service what to do; the Service does it.

Think of a Service as a specialist. The Controller says "run Rule 34 for this engagement." The Rule 34 Service knows exactly how to connect to the database, build the correct query, run it, count the passes and failures, and format the result for storage.

**Key Services in this system:**

| Service | What it does |
|---------|-------------|
| **Rule Services** (one per rule) | Contains all the logic for a specific audit rule — builds the validation query, runs it against the client's HEMIS database, counts passes and failures, identifies exceptions, and prepares results for saving and export |
| **System Database Service** | Handles all reading and writing to the system's own database — saving engagement records, storing validation results, retrieving messages, recording sign-offs, and supplying dashboard data |
| **Audit Log Service** | Every time a significant action happens (a login, a validation run, a sign-off, a download), this service writes a record of it to the audit log — who did it, when, and from which IP address |
| **Export Service** | Generates the Excel (.xlsx) and CSV files when an auditor clicks Download — formats the results into properly structured spreadsheets ready to be filed as audit evidence |
| **Password Policy Service** | Checks that passwords meet the required complexity rules, tracks password history to prevent reuse, monitors expiry dates, and forces renewal when a password has expired |
| **Validation Operation Service** | Manages long-running validations — when a check takes more than a few seconds on a large dataset, this service runs it in the background and lets the browser check on progress without timing out |

---

### 5.5 Models — The Data Definitions

**Simple explanation:** A Model is the definition of a piece of information that the system stores. It tells the system exactly what fields a record has and what type of information each field holds — like a form template that defines which boxes exist before anyone fills them in.

Every record saved in the system's database follows a Model. Without Models, the database would not know what to store or how to store it.

**The key Models in this system:**

| Model | What it represents | Why it matters |
|-------|-------------------|----------------|
| **User** | A person who has an account in the system | Stores their name, email, role, department, password history, profile picture, and whether their account is active |
| **Client** (Engagement) | A higher education institution being audited in a specific year | Stores the institution name, fiscal year, institution type, approval status, and who created it |
| **ClientUser** | The link between a user and an engagement | Records which users are assigned to which engagements and what role each person has on that engagement |
| **ValidationRun** | The result of running an audit analytic | Stores the server and database that were used, the total records checked, how many passed and failed, the exception rate, and the full results data |
| **AuditLog** | A record of a system action | Stores the timestamp, the user who performed the action, what the action was, and the IP address it came from |

**A practical example:** When you save a validation result after running Rule 34, the system creates a new `ValidationRun` record. It fills in your user ID, the engagement ID, the rule number, the pass count, the fail count, and stores the full exception rows. Later, when you or your manager opens the engagement to review the results, the system reads that `ValidationRun` record and displays it on screen.

---

### 5.6 Helpers — Shared Tools

**Simple explanation:** Helpers are small shared tools that multiple parts of the system use. Rather than writing the same logic in many different places, it is written once in a Helper and reused wherever it is needed.

Think of a Helper like a reference sheet pinned to the wall. Anyone who needs that information can look it up without recalculating it themselves.

**Key Helpers in this system:**

| Helper | What it does | Why it is useful |
|--------|-------------|-----------------|
| **IntegrityRuleCatalog** | A complete list of all 67 audit rules — their numbers, names, and descriptions | Every part of the system that needs to display rule information reads from this one central list, so the rule names and descriptions are consistent everywhere |
| **RuleRouteHelper** | Knows the web address (URL) for every rule's page | Used to generate the "Previous Rule" and "Next Rule" navigation links on every rule workspace |
| **ModuleSequenceNavigationHelper** | Works out which rule comes before and after the current one | Powers the breadcrumb navigation that lets auditors move between rules without going back to the main menu |
| **ValidationRunAccessPolicy** | Checks whether a user is allowed to perform a specific action on a validation run | Ensures that only the correct role can sign off, view, or download a result — checked once here rather than separately in every Controller |
| **AvatarHelper** | Decides what profile picture to show for a user | If the user has uploaded a photo, shows the photo; if not, generates a coloured circle with their initials |

---

### 5.7 The Database — Where Everything Is Stored

The system uses two separate databases, each with a different job:

**Database 1 — The System Database (the system's own records)**

This is the system's internal storage. It holds:
- All user accounts and their details
- All engagements (clients) and their status
- Who is assigned to which engagement and in what role
- All saved validation results (pass/fail counts, exception data, sign-off records)
- All audit log entries
- All messages and attachments

This database belongs entirely to SNG Grant Thornton. It lives on the firm's own infrastructure and is managed by the system.

**Database 2 — The Client's HEMIS Database (read-only)**

When an auditor runs a validation, the system connects to the institution's own SQL Server database — the database that contains the HEMIS student and staff data. The system reads from this database to perform the checks.

**The system never writes to the client's database.** It only reads. The client's data is never changed, deleted, or modified in any way. This is important for audit independence.

---

### 5.8 Static Assets — Appearance and Interactivity

These are the files that control how the system looks and behaves in the browser:

| Asset | What it does |
|-------|-------------|
| **CSS stylesheet** | Controls the colours, fonts, layout, spacing, and visual design of every page — the dark sidebar, the card layouts, the tables, the toolbar buttons |
| **JavaScript files** | Makes the pages interactive — dropdown menus that load database tables automatically, progress bars during a validation run, sign-off confirmation dialogs, and the real-time unread message count badge |
| **Uploaded files** | Profile pictures and message attachments uploaded by users are stored here on the server |

---

### 5.9 Technology Stack — What Was Used to Build It

| What it does | Tool used |
|-------------|----------|
| The application framework — the foundation everything runs on | ASP.NET Core MVC (C# programming language) |
| Reading and writing to the system's own database | Entity Framework Core (translates C# into database queries automatically) |
| The system's own database | SQLite (a lightweight, file-based database — no separate database server needed) |
| The client's HEMIS data | Microsoft SQL Server (the client's existing database server) |
| User login and security | ASP.NET Core Identity (handles passwords, sessions, and roles) |
| The pages users see | HTML, CSS, JavaScript |
| Generating Excel files | EPPlus / ClosedXML |
| Sending password reset emails | SMTP email |
| Fetching DEETYAPAC content from heda.co.za | ASP.NET HTTP Client |
| Running on the server | Windows Server / IIS (standard firm infrastructure) |

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

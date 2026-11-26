# NBC Claims Management System (NBC-CMS)

A web application for the Namibian Broadcasting Corporation that digitises the artist
claims process described in the NBC-CMS SRS and workflow documents. Artists capture
their work records, submit claims with supporting documents, respond to correction
requests, and download approved claim reports as PDF. The Secretary's office reviews,
corrects, approves or declines claims, and generates the signed claim report used for
payment authorisation.

## Feature overview

**Artist portal**

- Register-free sign-in with Secretary-issued credentials (PRB: only staff create accounts).
- Create and edit draft claims with any number of work records (recording date,
  programme, fee).
- Attach supporting documents (PDF, PNG/JPG, DOCX, XLSX up to 10 MB) to a claim.
- Submit a claim for review; resubmit after applying requested corrections.
- Track claim status: Draft, Submitted, Request Submission Correction, Resubmitted,
  Accepted, Declined.
- Read the Secretary's correction comments and decline reason inline.
- View banking details on record and download the approved claim report.

**Secretary / Admin portal**

- Dashboard with workload, value split, attention lists and recent audit activity.
- Review queue, corrections queue and finalised claims with search and status filters.
- Request a correction (with a required comment), accept, or decline (with a reason).
- Artist management: create accounts, edit details and banking information, activate or
  deactivate access, reset passwords.
- Programme management for the work-record programme list.
- Generate the signed claim report PDF and re-download previous reports.
- Audit log of every significant action with action and user filters.

**Cross-cutting**

- Cookie authentication, role-based authorisation (Artist, Secretary, Admin) and a
  fallback policy that requires authentication everywhere.
- PBKDF2-SHA256 password hashing (120 000 iterations, per-user salt).
- CSRF protection on all unsafe requests via `AutoValidateAntiforgeryToken`.
- Authorisation checks on every claim, document and report download.
- Africa/Windhoek date formatting and Namibian Dollar (N$) currency display.
- Responsive layout, keyboard-friendly forms and progressive-enhancement JavaScript.

## Technology

| Area | Choice |
| --- | --- |
| Runtime | .NET 10 (`net10.0`) |
| Web framework | ASP.NET Core MVC with Razor views |
| Data access | EF Core 10 with SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`) |
| PDF | QuestPDF 2026.9.2 (Community licence) |
| Front end | Hand-written CSS design system + vanilla JavaScript |
| Auth | Cookie authentication, role policies `Staff` and `ArtistOnly` |
| API | ASP.NET Core Web API with JWT bearer tokens (`src/NbcCms.Api`) |

## Getting started

### Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, Express, Developer or full edition)

### Run locally

```bash
dotnet build
cd src/NbcCms.Web
dotnet run
```

The application listens on the URL printed at startup (for example
`http://localhost:5049`). In Development the HTTPS redirect and HSTS are disabled, so
the HTTP URL works directly.

### Run the Web API

A JSON API over the same database lives in `src/NbcCms.Api`:

```bash
cd src/NbcCms.Api
dotnet run --urls http://localhost:5050
```

It authenticates with JWT bearer tokens and exposes claims, users, roles, banking details,
supporting documents, reports and overall amounts under `/api`. The OpenAPI document is at
`http://localhost:5050/openapi/v1.json`. See `src/NbcCms.Api/README.md` for the endpoint
reference, request examples and JWT configuration.

On first start the application:

1. Creates the `NBC_CMS_Dev` database if it does not exist (`EnsureCreated`).
2. Seeds roles, claim statuses and programmes.
3. Seeds demo users, banking details and four sample claims.

### Demo accounts

| Role | Username | Password |
| --- | --- | --- |
| Artist | `artist.demo` | `Artist@123` |
| Artist | `artist.selma` | `Artist@123` |
| Secretary | `secretary` | `Secretary@123` |
| Admin | `admin` | `Admin@123` |

These accounts are only seeded when the `Users` table is empty. Change or remove them
before using the system outside a development environment.

### Configuration

`src/NbcCms.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=NBC_CMS_Dev;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Connect Timeout=30"
  },
  "Cms": {
    "SystemName": "Claims Management System",
    "MaxUploadBytes": 10485760
  }
}
```

`Cms:MaxUploadBytes` caps the size of a single uploaded document.

### Production database provisioning

The schema can also be created from `database/NBC_CMS.sql` on a SQL Server instance:

```bash
sqlcmd -S <server> -i database/NBC_CMS.sql
```

Then point `DefaultConnection` at that database. The application still seeds lookup
data and the demo accounts on first start when they are missing.

## Using the system

1. Sign in as `artist.demo`.
2. Open **New claim**, choose the claim date, add work records and attach any supporting
   documents, then **Save draft** or **Submit for review**.
3. Sign out and sign in as `secretary`.
4. Open the claim from the review queue. Accept it, decline it with a reason, or request
   a correction with a comment.
5. The artist can then open the claim, read the comment, correct the records and
   resubmit it.
6. Once a claim is accepted, use **Generate report** to create the PDF. The report is
   stored with the claim and can be downloaded by staff and by the claim's artist. It
   reproduces the official *Artists' Fees or Royalties* form (NBC logo above the title,
   header fields, 20-row fee table, cash total, certification and authorisation lines).

## Project structure

```
NbcCms.slnx
database/
  NBC_CMS.sql                 SQL Server schema and lookup seed
src/NbcCms.Web/
  Controllers/                Account, Claims, Artists, Programmes, Reports,
                              Documents, Audit, Dashboard, Error
  Data/
    CmsDbContext.cs           EF Core model, table/column mappings
    DbSeeder.cs               Lookup data, demo users and sample claims
    Entities/                 User, Claim, Document, Report, Audit, Programme
  Services/
    PasswordHasher.cs         PBKDF2-SHA256 hashing and verification
    DocumentStorage.cs        Validated file storage outside wwwroot
    PdfReportService.cs       QuestPDF claim report
    ClaimQueries.cs           Shared claim projections
    Formatting.cs             Windhoek dates, currency, relative time
    Icons.cs                  Inline SVG icon set
  ViewModels/                 Screen and form models with validation
  Views/                      Razor views for every screen
  wwwroot/                    CSS, JavaScript, NBC logo
src/NbcCms.Api/
  Controllers/                Auth, Users, UserRoles, ArtistBankingDetails, Claim,
                              SupportingDocuments, Reports, OverallAmounts
  Data/                       EF Core model shared with the MVC portal, lookups seeder
  Models/                     Requests, responses and problem exceptions
  Security/                   JWT issuing/validation, PBKDF2 hasher, exception handler
  Services/                   Claim, user, role, banking, document, report, amounts logic
```

Uploaded documents are stored under `src/NbcCms.Web/App_Data/uploads/{claimId}/` so they
are never served directly; every download passes through an authorised controller
action.

## Workflow

```
Draft ──submit──▶ Submitted ──accept──▶ Accepted ──generate──▶ PDF report
   ▲                  │
   │                  ├──correction──▶ Request Submission Correction
   │                  │                     │
   └──edit────────────┘                     │ artist corrects + resubmits
                                             ▼
                                         Resubmitted ──accept/decline──▶ ...
   Submitted/Resubmitted ──decline──▶ Declined (reason recorded)
```

## Security notes

- Passwords are stored only as PBKDF2-SHA256 hashes with a per-user random salt.
- The authentication cookie is HttpOnly and SameSite=Lax; it is marked Secure outside
  Development.
- Every POST is protected by the anti-forgery token; forms include the token explicitly.
- Downloads resolve the requesting user's role and ownership before serving a file.
- Uploaded file names are generated server-side; the original name is kept only as
  metadata and the extension is checked against an allow-list.
- The audit log records sign-ins, failed sign-ins, claim transitions, document changes,
  account and programme changes, report generation and password changes.

## Known limitations

- The demo environment uses SQL Server LocalDB. For production, provision a SQL Server
  database, rotate the seeded demo accounts and serve the application over HTTPS.
- The system does not send email; accounts and password resets are handled by the
  Secretary's office.
- Reports are stored in the database (`report.fileData`) so the database grows with each
  generated report.

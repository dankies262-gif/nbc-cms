# NBC-CMS Web API

JSON API for the Namibian Broadcasting Corporation Claims Management System. It exposes
the same claim lifecycle, accounts, banking, documents and PDF reports as the MVC portal,
against the same SQL Server LocalDB database.

- **Framework:** ASP.NET Core 10 Web API, Entity Framework Core 10 (SQL Server)
- **Authentication:** JWT bearer tokens (HMAC-SHA256)
- **Authorization:** role policies — `Staff` (Secretary, Admin) and `ArtistOnly` (Artist)
- **Reports:** QuestPDF
- **OpenAPI document:** `GET /openapi/v1.json` (anonymous)

## Run the API

```bash
cd src/NbcCms.Api
dotnet run --urls http://localhost:5050
```

Prerequisites are the same as the MVC portal: .NET 10 SDK and SQL Server LocalDB. On first
start the API creates the database if necessary and seeds the role, status and programme
lookups. The MVC portal seeds demo users and sample claims; if you start the API against a
fresh database first, create the initial administrator with `POST /api/auth/bootstrap-admin`
(only available while the `Users` table is empty).

Demo accounts (already present in a seeded development database):

| Role | Username | Password |
| --- | --- | --- |
| Artist | `artist.demo` | `Artist@123` |
| Artist | `artist.selma` | `Artist@123` |
| Secretary | `secretary` | `Secretary@123` |
| Admin | `admin` | `Admin@123` |

## Configuration (`appsettings.json`)

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | LocalDB database shared with `NbcCms.Web` (`NBC_CMS_Dev`) |
| `Jwt:Issuer`, `Jwt:Audience` | Token validation values; must match between issue and validation |
| `Jwt:Key` | HMAC signing key, **at least 32 bytes** (startup fails otherwise) |
| `Jwt:LifetimeMinutes` | Token lifetime, default 480 minutes |
| `Cms:MaxUploadBytes` | Largest accepted supporting document, default 10 MB |

The development signing key in `appsettings.json` must be replaced with a secret
(for example user secrets, environment variables or a key vault) before deployment.

## Authentication

```bash
curl -s -X POST http://localhost:5050/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"secretary","password":"Secretary@123"}'
```

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "tokenType": "Bearer",
  "expiresAtUtc": "2026-11-21T00:31:12Z",
  "user": { "userId": 1, "username": "secretary", "roleName": "Secretary", "...": "..." }
}
```

Send the token on every other request:

```bash
curl -s http://localhost:5050/api/claims \
  -H "Authorization: Bearer $TOKEN"
```

`GET /api/auth/me` returns the signed-in user's profile. Tokens contain the user id,
username, full name and role, and expire after `Jwt:LifetimeMinutes`.

## Endpoint reference

Every route requires a bearer token unless marked **anonymous**. The role column shows
the policy that gates the endpoint.

### Auth — `/api/auth`

| Method | Route | Role | Description |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | anonymous | Sign in, returns JWT and profile |
| POST | `/api/auth/bootstrap-admin` | anonymous | Create the first Admin; 409 once users exist |
| GET | `/api/auth/me` | any | Profile of the signed-in user |

### Users — `/api/users` (Staff)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/users?search=&role=` | List accounts with claim counts and totals |
| GET | `/api/users/{id}` | Single account |
| POST | `/api/users` | Create an account (password required) |
| PUT | `/api/users/{id}` | Update an account (password optional = reset) |
| PATCH | `/api/users/{id}/status` | `{ "isActive": false }` activate/deactivate |

### User roles — `/api/user-roles` (Staff)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/user-roles` | List roles with user counts |
| POST | `/api/user-roles` | Create a role |
| PUT | `/api/user-roles/{id}` | Rename/describe a role |
| DELETE | `/api/user-roles/{id}` | Delete an unused, non-system role |

### Banking details — `/api/artist-banking-details`

| Method | Route | Role | Description |
| --- | --- | --- | --- |
| GET | `/api/artist-banking-details` | Artist | Own banking details |
| GET | `/api/artist-banking-details/{artistId}` | Staff | Bank details for an artist |
| PUT | `/api/artist-banking-details/{artistId}` | Staff | Create or replace banking details |

### Claims — `/api/claims`

| Method | Route | Role | Description |
| --- | --- | --- | --- |
| GET | `/api/claims?status=&scope=&search=` | any | Artists see their own claims; staff see all |
| GET | `/api/claims/{id}` | any | Claim with work records, documents, corrections |
| POST | `/api/claims` | Artist | Create a draft claim |
| PUT | `/api/claims/{id}` | Artist | Update a draft or a claim awaiting correction |
| DELETE | `/api/claims/{id}` | Artist | Delete a draft claim |
| POST | `/api/claims/{id}/submit` | Artist | Submit a draft or resubmit after correction |
| POST | `/api/claims/{id}/review` | Staff | Accept, decline or request correction |

`scope` accepts `review`, `corrections`, `final` or `draft`. `status` matches the exact
status name. Statuses: `Draft`, `Submitted`, `Request Submission Correction`, `Resubmitted`,
`Accepted`, `Declined`.

Create a claim:

```bash
curl -s -X POST http://localhost:5050/api/claims \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{
        "claimDate": "2026-11-18",
        "programmeTitle": "Morning Show",
        "items": [
          { "recordingDate": "2026-11-10", "programmeTitle": "Morning Show", "rate": 1500 },
          { "recordingDate": "2026-11-12", "programmeTitle": "Morning Show", "rate": 2500.50 }
        ]
      }'
```

Review a submitted claim:

```bash
# accept
curl -X POST http://localhost:5050/api/claims/7/review \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"decision":"accept"}'

# request a correction (comment required)
-d '{"decision":"correction","comment":"Attach the studio booking form."}'

# decline (comment required)
-d '{"decision":"decline","comment":"Rate exceeds the approved scale."}'
```

### Supporting documents — `/api/supporting-documents`

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/supporting-documents?claimId=` | Active documents for a claim |
| POST | `/api/supporting-documents` | Multipart upload (`claimId`, `File`) |
| GET | `/api/supporting-documents/{id}/download` | Download the stored file |
| DELETE | `/api/supporting-documents/{id}` | Soft-delete a document |

```bash
curl -X POST http://localhost:5050/api/supporting-documents \
  -H "Authorization: Bearer $TOKEN" \
  -F "claimId=7" -F "File=@invoice.pdf;type=application/pdf"
```

Allowed extensions: `.pdf`, `.png`, `.jpg`, `.jpeg`, `.docx`, `.xlsx`, up to
`Cms:MaxUploadBytes`. Files are stored under `App_Data/uploads/{claimId}` with a generated
name. Artists may only touch documents on their own claims, and only while the claim is a
draft or awaiting correction.

### Reports — `/api/reports`

| Method | Route | Role | Description |
| --- | --- | --- | --- |
| GET | `/api/reports?take=100` | Staff | Generated reports, newest first |
| GET | `/api/reports/ready` | Staff | Accepted claims without a report |
| POST | `/api/reports` | Staff | `{ "claimId": 7 }` generate a PDF report |
| GET | `/api/reports/{id}/download` | owner or staff | Download the PDF |

Generating a report is idempotent per claim: an existing report is returned instead of
creating a second copy. Send `{ "claimId": 7, "regenerate": true }` to rebuild the stored
PDF for a claim that already has one.

The PDF mirrors the official *ARTISTS' FEES OR ROYALTIES* form: the NBC logo centred above
the title block, `Req. No.`, claimant and banking fields, a 20-row items table (ITEM, ARTIST
No., RECORDING DATE, PROGRAMME TITLE, FEES/AMOUNT, ALLOCATION, CHEQUE NO., DATE), CASH
TOTAL, the certification sentence, and the Supervisor / Manager: IT / HOD / CTO signature
and date lines followed by the *Processed By Human Resources* block. The logo is read from
`wwwroot/img/nbc-logo.png` and is skipped when the file is absent.

### Overall amounts — `/api/overall-amounts` (Staff)

| Method | Route | Description |
| --- | --- | --- |
| GET | `/api/overall-amounts?claimId=` | List calculations (optionally per claim) |
| POST | `/api/overall-amounts` | Create a calculation (amount = rate × quantity) |
| PUT | `/api/overall-amounts/{id}` | Update a calculation |
| DELETE | `/api/overall-amounts/{id}` | Delete a calculation |

## Errors

Failures use RFC 7807 `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Only draft claims can be deleted.",
  "instance": "/api/claims/7"
}
```

| Status | Meaning |
| --- | --- |
| 400 | Validation or business-rule failure (for example a missing decline reason) |
| 401 | Missing, expired or invalid bearer token |
| 403 | Authenticated but the role cannot perform the action |
| 404 | Unknown id, or a resource the caller may not see |
| 409 | Workflow conflict (wrong status, duplicate username/role) |
| 500 | Unexpected server error (details are logged, not returned) |

## Project layout

```
src/NbcCms.Api
├── Program.cs                     Startup, JWT, policies, CORS, exception mapping
├── Controllers/                   One controller per resource in the API surface
├── Data/
│   ├── ApplicationDbContext.cs    Entity mappings (same tables as the MVC portal)
│   ├── DataSeeder.cs              Roles, statuses, programmes
│   └── Entities/                  User, Claim, documents, amounts
├── Models/
│   ├── Requests/                  Validated request payloads
│   ├── Response/                  Response DTOs with FromEntity mappers
│   └── ApiProblemException.cs     Business errors mapped to ProblemDetails
├── Security/                      JWT options/service, PBKDF2 hasher, exception handler
└── Services/                      Business logic + Services/Interfaces contracts
```

## Workflow notes

- Claim references are `CLM-000007` (id padded to six digits).
- Totals are the sum of work-record fees (`rate` per item).
- Work-record dates may not be in the future and a claim needs at least one record.
- Submitting a claim awaiting correction records the corrections as resolved and moves the
  claim to `Resubmitted`.
- A PDF report can only be generated for an `Accepted` claim.
- Passwords are stored as PBKDF2-SHA256 hashes (120 000 iterations, per-user salt) and are
  compatible with the MVC portal, so the same accounts work in both applications.
- The API listens on HTTP by default; terminate TLS in production and rotate the JWT key
  and demo accounts before going live.

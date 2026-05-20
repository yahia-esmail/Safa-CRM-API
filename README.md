# Safa CRM Api

> **Project:** A specialized CRM system for B2B sales management for a software company that sells technology solutions to companies in the tourism, travel, Hajj, and Umrah sectors.

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────┐
│  Client (Postman / Frontend)                    │
└────────────────────┬────────────────────────────┘
                     │ HTTPS / JWT / Rate Limiting
┌────────────────────▼────────────────────────────┐
│  API Layer  (ASP.NET Core 10)                   │
│  Controllers · Middleware · Security Headers    │
└────────────────────┬────────────────────────────┘
                     │ MediatR
┌────────────────────▼────────────────────────────┐
│  Application Layer                              │
│  Commands · Queries · Handlers · Validators     │
└────────────────────┬────────────────────────────┘
                     │ Interfaces
┌────────────────────▼────────────────────────────┐
│  Domain Layer                                   │
│  Entities · Enums · Interfaces (SAAS Multi-Tenant)
└────────────────────┬────────────────────────────┘
                     │ EF Core + Interceptors
┌────────────────────▼────────────────────────────┐
│  Infrastructure Layer                           │
│  AppDbContext (Audit & Sanitization)            │
│  Repositories · Services (Files, JWT, Email)    │
└─────────────────────────────────────────────────┘
```

**Pattern:** Clean Architecture (4 layers) + CQRS via MediatR
**Security:** Rate Limiting + Refresh Token Rotation + Input Sanitization + Audit Log
**Database:** SQL Server via Entity Framework Core 10
**API:** REST — ASP.NET Core 10
**Auth:** JWT Bearer + Refresh Tokens
**Architecture:** SAAS Multi-Tenant with Global Query Filters

---

## 2. Domain Entities

### SystemUser
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Name | string | |
| Email | string | Unique |
| PasswordHash | string | BCrypt |
| Role | Enum | SuperAdmin / Admin / Sales |
| IsActive | bool | |
| TenantId | Guid? | Null for SuperAdmin, Required for Admin/Sales |
| PasswordResetToken | string? | BCrypt-hashed reset token |
| PasswordResetTokenExpiry | DateTime? | 15-min expiry |

### Tenant
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Name | string | Company Name |
| Industry | string | |
| SubscriptionPlanId | Guid | FK |
| SubscriptionStart | DateTime | |
| SubscriptionEnd | DateTime | |
| IsActive | bool | |

### SubscriptionPlan
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Name | string | Basic, Pro, Enterprise |
| MaxAdmins | int | |
| MaxSales | int | |
| Price | decimal | |

### UserSmtpSetting
| Field | Type | Notes |
|---|---|---|
| UserId | Guid | PK & FK |
| Host | string | |
| Port | int | |
| Email | string | |
| Password | string | |
| Encryption | string | SSL / STARTTLS / NONE |

### RefreshToken
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | FK → SystemUser |
| Token | string | Unique, 500 chars |
| ExpiresAt | DateTime | |
| CreatedAt | DateTime | |
| IsRevoked | bool | |
| ReplacedByToken | string? | For rotation chain |
| CreatedByIp | string | |
| RevokedByIp | string? | |

### AuditLog
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| EntityName | string | e.g. "Company" |
| EntityId | string | PK of the record |
| Action | string | Added / Modified / Deleted |
| OldValues | string | JSON snapshot |
| NewValues | string | JSON snapshot |
| UserId | Guid? | Who did it |
| UserName | string? | |
| IpAddress | string? | |
| CreatedAt | DateTime | |

### Company
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| ArabicName | string | |
| EnglishName | string | |
| Country | string | |
| Phone | string | E.164 format |
| Email | string | |
| Website | string? | |
| SafaKey | int? | **Unique** — Must be > 0 to verify |
| AccountType | string | New / Existing |
| ContractAttachment | string? | URL/path to PDF or image |
| ApplicationForm | string? | URL/path to PDF or image |
| Stage | Enum | LeadOpportunity / Proposal / … |
| LeadSource | string? | Facebook / LinkedIn / … |
| LeadStatus | string | Reached / UnReached |
| ExpectedRevenue | decimal? | |
| IsActive | bool | Soft delete |
| AssignedToUserId | Guid? | FK → SystemUser |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | Auto-updated on modification |

**Uniqueness Rules:**
- `SafaKey` — unique if provided and `> 0`
- `Email` — unique if provided
- `Phone` — unique after E.164 format conversion

### SalesOrder
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| InvoiceNumber | string | Format: `INV/{year}/{seq:D5}` |
| CompanyId | Guid | FK |
| CreatedByUserId | Guid | FK |
| OrderReference | string? | e.g. S02119 |
| SaleOrderType | string | New / Renewal / Upgrade |
| Status | Enum | Draft / Confirmed / Cancelled |
| PaymentMethod | string? | Cash / Online / Transfer |
| OriginalCurrency | Enum | Currency applies to ALL items in this order |
| OriginalAmount | decimal | **Auto-calculated** = Σ item prices (read-only) |
| UsdRateAtTime | decimal | Exchange rate snapshot at creation time |
| UsdAmount | decimal | Calculated = OriginalAmount ÷ rate |
| Attachment | string? | |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | Auto-updated on modification |
| CancellationReason | string? | Why the order was cancelled |
| ConfirmedAt | DateTime? | When the order was confirmed |
| CancelledAt | DateTime? | When the order was cancelled |

> **Important:** `originalAmount` is not sent by the client. It is calculated automatically as the sum of the prices of all items (including negative values).

### SalesOrderItem
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| SalesOrderId | Guid | FK |
| SolutionId | Guid | FK |
| Price | decimal | **Can be negative** (discount line) |
| StartDate | DateOnly? | Subscription start — date only, no time |
| EndDate | DateOnly? | Subscription end — date only, no time |
| Note | string? | |

> Currency is inherited from the parent Order, not per-item.

### Invoice
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| SalesOrderId | Guid | FK (One-to-One with SalesOrder) |
| InvoiceNumber | string | Same as the Order's InvoiceNumber |
| IssueDate | DateTime | |
| Notes | string? | |
| PdfUrl | string? | |

### ImportLog
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| FileName | string | Uploaded file name |
| Type | string | `Companies` |
| Status | string | `Success` / `PartialSuccess` / `Failed` |
| TotalRows | int | Total rows in file |
| SuccessRows | int | Rows saved to DB |
| ErrorDetails | string? | JSON with row errors |
| UploadedByUserId | Guid | FK → SystemUser |
| CreatedAt | DateTime | |

### Notification
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| UserId | Guid | FK → SystemUser (Recipient) |
| Title | string | Brief alert title |
| Body | string | Detailed message content |
| Type | Enum | NotificationType |
| EntityType | string? | Related entity name (e.g. "Company") |
| EntityId | string? | ID of the related entity |
| IsRead | bool | Read status |
| CreatedAt | DateTime | |
| ReadAt | DateTime? | When marked read |

### CompanyNote
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CompanyId | Guid | FK → Company |
| CreatedByUserId | Guid | FK → SystemUser |
| Content | string | Note content |
| CreatedAt | DateTime | |
| UpdatedAt | DateTime? | |
| TenantId | Guid | FK → Tenant |

### CompanyTag
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Name | string | Tag name (e.g., "VIP", "Hot Lead") |
| Color | string | Hex color code |
| CreatedByUserId | Guid | FK → SystemUser |
| CreatedAt | DateTime | |

### CompanyTagAssignment
| Field | Type | Notes |
|---|---|---|
| CompanyId | Guid | PK & FK → Company |
| TagId | Guid | PK & FK → CompanyTag |

### StageHistory
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CompanyId | Guid | FK → Company |
| FromStage | string | Previous stage |
| ToStage | string | New stage |
| ChangedByUserId | Guid | FK → SystemUser |
| Reason | string? | Reason for stage change |
| ChangedAt | DateTime | |
| TenantId | Guid | FK → Tenant |

### TechSolution
Products/solutions that can be added to an order.

### CompanyContact
Contacts (people) inside a client company.

### Activity
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| CompanyId | Guid | FK |
| CreatedByUserId | Guid | FK → SystemUser |
| Type | Enum | ColdCall / Meeting / WhatsAppFollowUp / Email / Task |
| Note | string | |
| DueDate | DateTime? | Task deadline |
| IsCompleted | bool | Completion status |
| CompletedAt | DateTime? | When task was completed |
| CreatedAt | DateTime | |

### ExchangeRate
Daily snapshot of 160+ currency exchange rates (USD as base currency).

---

## 3. API Endpoint Reference

### Auth — `/api/auth`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| POST | `/login` | No | Login, get access + refresh tokens |
| POST | `/refresh` | No | Renew access token using refresh token |
| POST | `/logout` | Yes | Invalidate current refresh token |
| POST | `/register` | Yes (Admin) | Create a new system user |
| POST | `/forgot-password` | No | Send password reset email |
| POST | `/reset-password` | No | Reset password with token |

### Super Admin — `/api/superadmin`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/dashboard` | Yes (SuperAdmin) | Get super admin statistics |
| GET | `/plans` | Yes (SuperAdmin) | List subscription plans |
| POST | `/plans` | Yes (SuperAdmin) | Create new subscription plan |
| PUT | `/plans/{id}` | Yes (SuperAdmin) | Update plan |
| DELETE | `/plans/{id}` | Yes (SuperAdmin) | Delete plan |
| GET | `/tenants` | Yes (SuperAdmin) | List tenants |
| GET | `/tenants/{id}` | Yes (SuperAdmin) | Get tenant details |
| POST | `/tenants` | Yes (SuperAdmin) | Create new tenant |
| PUT | `/tenants/{id}` | Yes (SuperAdmin) | Update tenant |
| PATCH | `/tenants/{id}/toggle` | Yes (SuperAdmin) | Activate/Deactivate tenant |
| GET | `/audit-logs` | Yes (SuperAdmin) | View system audit logs |

### Users (Tenant-level) — `/api/users`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes (Admin) | List users for the current tenant |
| GET | `/{id}` | Yes (Admin) | Get specific user |
| POST | `/` | Yes (Admin) | Create new user (Sales) |
| PUT | `/{id}` | Yes (Admin) | Update user details |
| PATCH | `/{id}/toggle` | Yes (Admin) | Activate/Deactivate user |
| DELETE| `/{id}` | Yes (Admin) | Soft delete user |

### Profile — `/api/profile`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Get current user's profile |
| PUT | `/` | Yes | Update current user's profile |
| POST | `/change-password`| Yes | Change current user's password |

### Companies — `/api/companies`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Search and paginate companies |
| GET | `/export` | Yes | Export filtered companies to Excel |
| GET | `/{id}` | Yes | Get company by ID |
| POST | `/` | Yes | Create company |
| PUT | `/{id}` | Yes | Update company |
| DELETE | `/{id}` | Yes | Soft delete company |
| POST | `/{id}/assign` | Yes (Admin) | Assign company to a sales rep |
| GET | `/{id}/contacts` | Yes | List contacts |
| POST | `/{id}/contacts` | Yes | Add contact |
| PUT | `/{companyId}/contacts/{contactId}` | Yes | Update contact |
| DELETE| `/{companyId}/contacts/{contactId}` | Yes | Delete contact |
| GET | `/{id}/activities`| Yes | List activities |
| POST | `/{id}/activities`| Yes | Add activity |
| PUT | `/{companyId}/activities/{activityId}` | Yes | Update activity |
| PATCH | `/{companyId}/activities/{activityId}/toggle-complete` | Yes | Toggle activity completion |
| DELETE| `/{companyId}/activities/{activityId}` | Yes | Delete activity |
| GET | `/{id}/stage-history`| Yes | View pipeline stage timeline history |
| POST | `/{id}/tags/{tagId}`| Yes | Assign tag to company |
| DELETE| `/{id}/tags/{tagId}`| Yes | Remove tag from company |
| GET | `/{id}/notes` | Yes | List company free-form notes |
| POST | `/{id}/notes` | Yes | Create note |
| PUT | `/{companyId}/notes/{noteId}` | Yes | Update note content |
| DELETE| `/{companyId}/notes/{noteId}` | Yes | Delete note |
| POST | `/bulk-assign` | Yes (Admin) | Assign multiple companies to a rep |
| POST | `/bulk-stage` | Yes | Move multiple companies to a new stage |
| POST | `/bulk-delete` | Yes | Soft delete multiple companies |

### Orders — `/api/orders`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | List orders (Admin=all, Sales=own) |
| GET | `/export` | Yes | Export filtered sales orders to Excel |
| GET | `/{id}` | Yes | Get single order |
| POST | `/` | Yes | Create order |
| PUT | `/{id}` | Yes | Update order |
| DELETE | `/{id}` | Yes | Delete draft order |

### Invoices — `/api/invoices`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/{orderId}/pdf` | Yes | Download invoice as PDF |

### Activities & Tasks — `/api/activities`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/my-tasks` | Yes | List tasks assigned to me or created by me, with completion filter |

### Pipeline — `/api/pipeline`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Get company counts split by sales stage |

### Email — `/api/email`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| POST | `/send` | Yes | Send a single email (using user SMTP) |
| POST | `/bulk` | Yes | Send bulk emails |

### SMTP Settings — `/api/smtp`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Get logged-in user's SMTP settings (excluding password) |
| POST | `/` | Yes | Register own SMTP settings |
| PUT | `/` | Yes | Update own SMTP settings |
| DELETE | `/` | Yes | Delete own SMTP settings |
| POST | `/test` | Yes | Send test email to self to verify SMTP credentials |

### Notifications — `/api/notifications`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | List notifications (paginated, with type & read filters) |
| GET | `/unread-count`| Yes | Get total number of unread notifications |
| PUT | `/{id}/read` | Yes | Mark a single notification as read |
| PUT | `/read-all` | Yes | Mark all own notifications as read |
| DELETE | `/{id}` | Yes | Delete a specific notification |

### Tags — `/api/tags`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | List all custom labels / tags |
| POST | `/` | Yes | Create a new custom tag |
| DELETE | `/{id}` | Yes (Admin) | Delete a custom tag (cascades assignments) |

### Dashboard — `/api/dashboard`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Get Admin or Sales dashboard stats |
| GET | `/export/pdf` | Yes | Download beautiful PDF dashboard summary (QuestPDF) |

### Audit logs — `/api/audit`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes (Admin) | Query DB audit logs (entity filters, userId, changes diff) |

### Exchange Rates — `/api/rates`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/today` | Yes | Get latest exchange rates |
| GET | `/history`| Yes | Get historical rates |
| POST | `/refresh`| Yes (Admin) | Manually trigger a rates refresh |

### Solutions — `/api/solutions`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | List tech solutions |
| POST | `/` | Yes (Admin) | Add new solution |
| PUT | `/{id}` | Yes (Admin) | Update solution |

### Files — `/api/files`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| POST | `/upload` | Yes | Secure file upload (Max 10MB) |

### Company Import — `/api/companyimport`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/template` | Yes | Download Excel import template |
| POST | `/preview` | Yes | Upload Excel & validate rows |
| POST | `/confirm` | Yes | Save validated rows from session |
| GET | `/logs` | Yes (Admin) | View import history |

### AI — `/api/ai`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| POST | `/compose-email`| Yes | Generate AI sales email |
| GET | `/lead-score/{id}`| Yes | AI lead score |
| GET | `/next-action/{id}`| Yes | Recommended next CRM activity |
| GET | `/insights` | Yes (Admin) | AI-written sales insights |

---

## 4. Request / Response Examples

### POST `/api/orders`
```json
{
  "companyId": "a8d23989-9fea-4f20-b18f-ca0389388179",
  "orderReference": "S02119",
  "saleOrderType": "Renewal",
  "paymentMethod": "Transfer",
  "originalCurrency": "EGP",
  "attachment": null,
  "items": [
    {
      "solutionId": "677c3fe0-354f-4d7d-a55e-eb3e1295c000",
      "price": 25000,
      "startDate": "2026-01-01",
      "endDate": "2026-12-31",
      "note": "Website Builder — 1 year subscription"
    },
    {
      "solutionId": "677c3fe0-354f-4d7d-a55e-eb3e1295c001",
      "price": -2000,
      "note": "Early payment discount"
    }
  ]
}
```
> `originalAmount` auto-calculated: 25000 + (-2000) = **23,000 EGP**

### POST `/api/companies`
```json
{
  "arabicName": "شركة الاختبار",
  "englishName": "Test Company",
  "country": "EG",
  "phone": "+201001234567",
  "email": "test@company.com",
  "safaKey": 1234,
  "accountType": "New",
  "stage": "LeadOpportunity",
  "leadStatus": "UnReached",
  "contractAttachment": null,
  "applicationForm": null
}
```

---

## 5. Business Rules

| Rule | Details |
|---|---|---|---|
| Rate Limiting | **Login/Reset**: 5 req/min. **General**: 100 req/min. |
| Token Rotation | Refresh token is replaced on every use; old ones revoked. |
| Compromise detection | Using a revoked refresh token revokes ALL user tokens. |
| Input Sanitization | All text input cleaned via `HtmlSanitizer` before DB save. |
| Audit Trail | Every Add/Update/Delete is logged with old/new values. |
| SafaKey uniqueness | Checked only if provided and value > 0 |
| Email uniqueness | Checked if not empty (company level) |
| Phone uniqueness | Checked after E.164 phone format conversion |
| OriginalAmount | Read-only, auto = Σ all item prices |
| Negative item price | Allowed — treated as discount line |
| SMTP Password Storage | AES-256 encrypted using key from environment variables. |
| Multi-Tenancy Isolation | Automatically enforced at the EF Core query layer. |
| Audit Query Bounds | Restricted to SuperAdmins and Admins (Sales reps cannot query). |
| Bulk Operations | Admins can assign any leads; Sales reps can only update their own leads. |

---

## 6. Infrastructure Services

| Service | Description |
|---|---|---|---|
| `JwtService` | Access token (60 min) + refresh token generation |
| `EmailService` | HTML email via SMTP (MailKit) |
| `EncryptionService` | Secure AES-256 encryption & decryption for credentials |
| `ExportService` | Generates Excel workbooks (ClosedXML) and PDF dashboards (QuestPDF) |
| `ExchangeRateService` | Fetch & save 160+ currency rates from external API |
| `PdfInvoiceService` | Generate PDF invoices using QuestPDF |
| `FileUploadService` | Secure file management (Validation + GUID naming) |
| Hangfire Background Jobs | - Daily exchange rate refresh (6:00 AM UTC)<br>- Daily subscription renewal alerts (8:00 AM UTC)<br>- Daily activity overdue checks (8:00 AM UTC) |

---

## 7. Database Migrations Log

| Migration | Date | Changes |
|---|---|---|---|
| `InitialCreate` | 2026-03-10 | Full initial schema |
| `AddPasswordResetToken` | 2026-03-11 | PasswordResetToken + Expiry columns on Users |
| `AddInvoicesTable` | 2026-04-06 | New Invoices table (1:1 with SalesOrders) |
| `AddOrderItemDatesRemoveCurrency` | 2026-04-16 | Removed `Currency` from SalesOrderItems; added `StartDate`, `EndDate` |
| `PhaseA_Security` | 2026-04-30 | Added `RefreshTokens` and `AuditLogs` tables; updated `SystemUser` |
| `AddImportLogsAndAiConfig` | 2026-05-02 | Ensured `ImportLogs` table is created with correct schema |
| `AddSaaSArchitecture` | 2026-05-03 | Added `Tenant`, `SubscriptionPlan`, `UserSmtpSetting`. Implemented Multi-Tenancy. |
| `AddNotificationsAndGaps` | 2026-05-19 | Added `CompanyNotes`, `CompanyTags`, `Notifications`, `StageHistories`, and gap columns to existing tables |

---

## 8. Default Seed User

| Field | Value |
|---|---|
| Name | System Administrator |
| Email | `admin@safa-crm.local` |
| Password | `Admin@123` |
| Role | Admin |

> **Warning:** Change the default password immediately after deploying the system to a production environment.

---

## 9. Tech Stack

| Component | Technology / Version |
|---|---|---|---|
| API Framework | ASP.NET Core 10 |
| Architecture | Clean Architecture + CQRS + MediatR |
| Database | SQL Server |
| ORM | Entity Framework Core 10 |
| Authentication | JWT Bearer + BCrypt.Net-Next |
| Rate Limiting | Built-in ASP.NET Core 10 RateLimiter |
| Security | Sanitization (HtmlSanitizer) + Security Headers |
| Background Jobs | Hangfire |
| PDF Generation | QuestPDF (Community license) |
| Excel Read/Write | ClosedXML 0.102+ |
| Phone Formatting | libphonenumber-csharp |
| Email | MailKit / SMTP |
| AI Integration | Google Gemini REST API (gemini-2.0-flash) |
| Preview Caching | IMemoryCache (built-in .NET) |

---

## 10. User Experience (UX) Flow

The system provides a tailored user experience depending on the user's role (`SuperAdmin`, `Admin`, or `Sales`), focusing on clarity, speed, and actionable insights.

### 1. SuperAdmin Experience
- **Focus:** System-wide management and monetization.
- **Flow:**
  - Logs in to a global dashboard showing all registered Tenants and total MRR.
  - Manages **Subscription Plans** (Basic, Pro, Enterprise) and defines limits (Max Admins, Max Sales).
  - Onboards new **Tenants**, setting their active plans and limits.
  - Can view **Audit Logs** globally to monitor system activity or investigate issues.

### 2. Admin (Tenant Manager) Experience
- **Focus:** Tenant configuration, team performance, and reporting.
- **Flow:**
  - Lands on the **Admin Dashboard**, viewing company-wide KPIs (Active Companies, Orders by Status, Revenue).
  - Can **invite Sales Representatives** (up to plan limits) and manage their active status.
  - Uses **AI Insights** to read automated summaries of sales performance.
  - Can use the **Company Import Tool** to upload historical leads via Excel, preview errors, and confirm.
  - Has full visibility into all companies and orders across the tenant, and can **reassign leads** between sales reps.

### 3. Sales Representative Experience
- **Focus:** Lead management, daily activities, and closing deals.
- **Flow:**
  - Lands on the **Sales Dashboard**, which highlights *their* assigned leads, recent orders, and daily tasks.
  - **Company Management:**
    - Adds new prospects manually or receives assigned leads from Admin.
    - Tracks interactions via the **Activity Feed** (Calls, Meetings, WhatsApp).
  - **AI-Assisted Selling:**
    - Views the AI **Lead Score** to prioritize which companies to contact today.
    - Clicks **Next Action** to get AI recommendations on follow-ups.
    - Uses **Compose Email** to let AI draft contextual emails, then sends them directly via their personal **SMTP settings**.
  - **Closing Deals:**
    - Creates **Sales Orders**, adding solutions with potential discounts (negative values).
    - Generates and downloads **PDF Invoices** instantly to send to the client.

### Common UX Elements
- **Responsive Navigation:** Sidebar or top-nav adapted to the user's permissions.
- **Immediate Feedback:** Toast notifications for success/error on CRUD operations.
- **Data Tables:** Searchable, filterable, and paginated lists for Companies and Orders.
- **Slide-overs/Modals:** For quick actions like adding a Contact or Activity without leaving the Company view.

---

## 11. Changelog

### v4.1 — 2026-05-19
- **Reporting & Data Export Engine:**
  - Implemented Excel export for Companies and Sales Orders using ClosedXML.
  - Implemented QuestPDF PDF Dashboard Export generating comprehensive KPI cards, sales analytics, and lead distributions for Admins and Sales Reps.
  - Added database audit logs query endpoint (`GET /api/audit`) to monitor entity insertions/updates (Admin only).
- **In-App Notifications & Background Reminders:**
  - Implemented in-app pull-based notifications (`GET /api/notifications` and `GET /api/notifications/unread-count`).
  - Configured Hangfire background jobs to check daily for overdue activities and order subscription renewals.
- **Activity & Task Management Updates:**
  - Added new fields to Activities (`DueDate`, `IsCompleted`, `CompletedAt`, `CreatedByUserId`).
  - Added endpoints to update, toggle-complete, and delete activities.
  - Added `GET /api/activities/my-tasks` to retrieve tasks assigned to or created by the logged-in user.
- **Tags & Free-Form Notes Systems:**
  - Created customizable `CompanyTags` with hex color badges.
  - Added many-to-many tag assignment to companies with tag-based filtering.
  - Added a free-form `CompanyNotes` system allowing reps to store detailed conversation summaries.
- **Bulk Operations:**
  - Added batch assignments, bulk pipeline stage transitions, and bulk soft-delete endpoints for companies.
- **Security & Infrastructure Refactoring:**
  - Refactored email dispatcher to run under individual user SMTP credentials.
  - Added secure AES-256 encryption service to safeguard SMTP passwords in the database.
  - Applied Global Query Filters for companies to enforce soft-delete constraints uniformly.

### v4.0 — 2026-05-03
- **SAAS Multi-Tenancy:**
  - Added `SuperAdmin`, `Admin`, and `Sales` hierarchy.
  - Added `Tenant` and `SubscriptionPlan` (Packages) management.
  - Implemented `IMustHaveTenant` Global Query Filters in EF Core for total data isolation.
  - `SuperAdmin` can manage plans and tenants. `Admin` can only add `Sales` within plan limits.
  - `UserSmtpSetting` added allowing each `Sales` user to configure personal SMTP for sending emails.

### v3.1 — 2026-05-02
- **Dashboard Enhancements:**
  - **Admin Dashboard**: Added `TotalCompanies`, `TotalActiveCompanies`, `TotalOrders`, `NewCompaniesThisMonth`, `NewOrdersThisMonth`, `OrdersByStatus` (Draft/Confirmed/Cancelled), and a `RecentOrders` feed (last 10).
  - **Sales Dashboard**: Added `NewCompaniesThisMonth`, a `RecentOrders` feed (last 10 of my orders), and a `RecentActivities` feed (last 10 activities on my companies).
  - Frontend docs updated to reflect the new API response and suggested UI widgets.

### v3.0 — 2026-05-02
- **Excel Bulk Company Import:**
  - `GET /api/companyimport/template` — Download official .xlsx template
  - `POST /api/companyimport/preview` — Upload & validate (no DB write); returns ImportId + preview summary
  - `POST /api/companyimport/confirm` — Submit ImportId to save valid rows (15-min session cache)
  - `GET /api/companyimport/logs` — Admin view of all import history
  - Full row-level validation: required fields, E.164 phone, email format, valid enum values
  - Deduplication: within file AND against existing DB records
  - Template stored at `wwwroot/templates/Safa_CRM_Company_Import_Template.xlsx`
- **AI Integration — Google Gemini (gemini-2.0-flash):**
  - `POST /api/ai/compose-email` — Generates professional AR/EN email from company context
  - `GET /api/ai/lead-score/{id}` — AI score 0–100 with grade, factors, recommendation
  - `GET /api/ai/next-action/{id}` — Best next CRM action with urgency + message template
  - `GET /api/ai/insights` — Admin sales insights: bilingual summary, highlights, alerts
  - API key stored in `appsettings.json` under `AI:GeminiApiKey`
- **Infrastructure:** ClosedXML added for Excel parsing; `IMemoryCache` for import sessions
- **Migration:** `AddImportLogsAndAiConfig` applied

- **Rate Limiting**: Applied to Auth (5 req/min) and API (100 req/min)
- **Refresh Token Rotation**: Moved tokens to dedicated table; added "Theft detection" (revokes all if leaked token used)
- **Audit Logs**: Automatic EF Core Interceptor captures every Added/Modified/Deleted record with JSON diff
- **Input Sanitization**: Global Interceptor cleans HTML/XSS from all text fields using `HtmlSanitizer`
- **Security Headers**: X-Frame-Options (DENY), CSP (self), HSTS, X-Content-Type-Options
- **File Upload Security**: New `FileUploadService` with size/type validation and GUID renaming
- **API**: Added `POST /api/files/upload`

### v1.4 — 2026-04-16
- **Global Exception Middleware** (`API/Middleware/GlobalExceptionMiddleware.cs`):
  - All unhandled exceptions now return the correct HTTP status globally
  - `ArgumentException` / `InvalidOperationException` → `400 Bad Request`
  - `KeyNotFoundException` → `404 Not Found`
  - `UnauthorizedAccessException` → `401 Unauthorized`
  - Any other exception → `500 Internal Server Error` with generic message
  - Eliminates `500` errors when empty/invalid data is submitted to any endpoint
- **User Validation** (`UserFeatures.cs`):
  - Name, Email, Password, Role are all validated with clear messages (Arabic + English)
  - Password minimum 6 characters
  - Valid Role values: `Admin`, `Sales`
  - Email uniqueness check on create & update (excludes self on update)
  - Email normalized to lowercase before save

### v1.3 — 2026-04-16
- **Orders redesign:**
  - Currency moved to Order level (removed from per-item)
  - `originalAmount` removed from request — auto-calculated as Σ item prices
  - Added `startDate` / `endDate` per item (date only, for subscription tracking)
  - Negative item prices supported (discount lines shown in red on PDF)
  - PDF invoice: added Period column, red discount lines
- **Company fixes:**
  - SafaKey uniqueness check now only runs if value > 0
  - Controller now properly returns `400 Bad Request` instead of `500` on validation errors

### v1.2 — 2026-04-06
- Fixed `POST /api/Orders` — invoice number generation rewritten in-memory to bypass EF query translation
- Fixed `GET /api/Dashboard` — all GroupBy queries refactored for EF Core 10 compatibility
- Added `Invoice` entity and table (1:1 with SalesOrder), auto-created on Order save
- Added `GET /api/invoices/{orderId}/pdf` endpoint using QuestPDF
- Company uniqueness validation: SafaKey, Email, Phone (bilingual error messages)
- Company: added `ContractAttachment` and `ApplicationForm` optional fields

### v1.1 — 2026-03-11
- Auth: Register (Admin only), Forgot Password, Reset Password
- Password reset email with 15-min token
- Database seeder: initial Admin user created on startup

### v1.0 — 2026-03-10
- Initial system: Clean Architecture + CQRS setup
- Auth: Login, Refresh, Logout with JWT
- Companies CRUD with search, pagination, soft delete
- Contacts and Activities management
- TechSolutions (products) CRUD
- SalesOrders with multi-currency exchange rate conversion
- Dashboard: Admin and Sales views (different KPIs)
- ExchangeRate service with 160+ world currencies

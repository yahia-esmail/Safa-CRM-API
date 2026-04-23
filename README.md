# Safa-CRM-API


> **Project:** A specialized CRM system for B2B sales management for a software company that sells technology solutions to companies in the tourism, travel, Hajj, and Umrah sectors.

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────┐
│  Client (Postman / Frontend)                    │
└────────────────────┬────────────────────────────┘
                     │ HTTPS / JWT
┌────────────────────▼────────────────────────────┐
│  API Layer  (ASP.NET Core 10)                   │
│  Controllers · BaseController · JWT Middleware  │
└────────────────────┬────────────────────────────┘
                     │ MediatR
┌────────────────────▼────────────────────────────┐
│  Application Layer                              │
│  Commands · Queries · Handlers · Validators     │
└────────────────────┬────────────────────────────┘
                     │ Interfaces
┌────────────────────▼────────────────────────────┐
│  Domain Layer                                   │
│  Entities · Enums · Interfaces                  │
└────────────────────┬────────────────────────────┘
                     │ EF Core
┌────────────────────▼────────────────────────────┐
│  Infrastructure Layer                           │
│  AppDbContext · Repositories · Services         │
│  JWT · Email · ExchangeRate · PDF · Hangfire    │
└─────────────────────────────────────────────────┘
```

**Pattern:** Clean Architecture (4 layers) + CQRS via MediatR
**Database:** SQL Server via Entity Framework Core 10
**API:** REST — ASP.NET Core 10
**Auth:** JWT Bearer + Refresh Tokens

---

## 2. Domain Entities

### SystemUser
| Field | Type | Notes |
|---|---|---|
| Id | Guid | PK |
| Name | string | |
| Email | string | Unique |
| PasswordHash | string | BCrypt |
| Role | Enum | Admin / Sales |
| IsActive | bool | |
| RefreshToken | string? | |
| RefreshTokenExpiry | DateTime? | |
| PasswordResetToken | string? | BCrypt-hashed reset token |
| PasswordResetTokenExpiry | DateTime? | 15-min expiry |

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

### TechSolution
Products/solutions that can be added to an order.

### CompanyContact
Contacts (people) inside a client company.

### Activity
CRM activities: ColdCall / Meeting / WhatsAppFollowUp / Email / Task.

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
| POST | `/forgot-password` | No | Send password reset email (always 204) |
| POST | `/reset-password` | No | Reset password with token (15-min expiry) |

### Companies — `/api/companies`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Search and paginate companies |
| GET | `/{id}` | Yes | Get company by ID |
| POST | `/` | Yes | Create company (uniqueness validated) |
| PUT | `/{id}` | Yes | Update company |
| DELETE | `/{id}` | Yes | Soft delete (sets IsActive=false) |
| POST | `/{id}/assign` | Yes (Admin) | Assign company to a sales rep |
| GET | `/{id}/contacts` | Yes | List contacts |
| POST | `/{id}/contacts` | Yes | Add contact |
| PUT | `/{id}/contacts/{cid}` | Yes | Update contact |
| DELETE | `/{id}/contacts/{cid}` | Yes | Delete contact |
| GET | `/{id}/activities` | Yes | List activities |
| POST | `/{id}/activities` | Yes | Add activity |

### Orders — `/api/orders`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | List orders (Admin=all, Sales=own only) |
| GET | `/{id}` | Yes | Get single order with items |
| POST | `/` | Yes | Create order — OriginalAmount auto-calculated |
| PUT | `/{id}` | Yes | Update order metadata/status |
| DELETE | `/{id}` | Yes | Delete if status is Draft only |

### Invoices — `/api/invoices`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/{orderId}/pdf` | Yes | Download invoice as PDF file |

### Dashboard — `/api/dashboard`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Auto-returns Admin or Sales dashboard by role |

### Exchange Rates — `/api/exchangerates`
| Method | Endpoint | Auth Required | Description |
|---|---|---|---|
| GET | `/` | Yes | Get latest exchange rates |
| POST | `/refresh` | Yes (Admin) | Manually trigger a rates refresh |

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
|---|---|
| SafaKey uniqueness | Checked only if provided and value > 0 |
| Email uniqueness | Checked if not empty (company level) |
| Phone uniqueness | Checked after E.164 phone format conversion |
| OriginalAmount | Read-only, auto = Σ all item prices |
| Negative item price | Allowed — treated as discount line |
| StartDate / EndDate | Date only (no time), both optional, EndDate ≥ StartDate |
| Single currency | One currency per order applies to all items |
| Invoice auto-create | Invoice created automatically when Order is saved |
| Delete restriction | Confirmed orders cannot be deleted |
| Soft delete | Companies: `IsActive = false` |
| Password reset | Token BCrypt-hashed in DB, expires in 15 minutes |
| Exchange rate | Fetched daily by Hangfire background job |
| User enumeration | `forgot-password` always returns 204 regardless of email existence |

---

## 6. Infrastructure Services

| Service | Description |
|---|---|
| `JwtService` | Access token (60 min) + refresh token generation |
| `EmailService` | HTML email via SMTP (MailKit) |
| `ExchangeRateService` | Fetch & save 160+ currency rates from external API |
| `PdfInvoiceService` | Generate PDF invoices using QuestPDF (Community license) |
| Hangfire Background Jobs | Daily exchange rate refresh |

---

## 7. Database Migrations Log

| Migration | Date | Changes |
|---|---|---|
| `InitialCreate` | 2026-03-10 | Full initial schema |
| `AddPasswordResetToken` | 2026-03-11 | PasswordResetToken + Expiry columns on Users |
| `AddInvoicesTable` | 2026-04-06 | New Invoices table (1:1 with SalesOrders) |
| `AddOrderItemDatesRemoveCurrency` | 2026-04-16 | Removed `Currency` from SalesOrderItems; added `StartDate`, `EndDate` |

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
|---|---|
| API Framework | ASP.NET Core 10 |
| Architecture | Clean Architecture + CQRS + MediatR |
| Database | SQL Server |
| ORM | Entity Framework Core 10 |
| Authentication | JWT Bearer + BCrypt.Net-Next |
| Background Jobs | Hangfire |
| PDF Generation | QuestPDF (Community license) |
| Phone Formatting | libphonenumber-csharp |
| Email | MailKit / SMTP |

---

## 10. Changelog

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
